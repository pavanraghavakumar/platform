using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IBillingService
{
    /// <summary>
    /// Builds the lines an invoice for the appointment will carry: the Doctor's consultation fee,
    /// each completed lab request of the appointment and each medicine line of the appointment's
    /// prescription. Nothing is saved.
    /// </summary>
    Task<IReadOnlyList<InvoiceItem>> BuildLinesAsync(int appointmentId);

    /// <summary>
    /// Generates and saves the invoice for the appointment with the given additional service
    /// charge, discount and tax rate, and returns it.
    /// </summary>
    Task<Invoice> GenerateFromAppointmentAsync(
        int appointmentId,
        decimal discountAmount,
        decimal taxRate,
        string? additionalServiceDescription,
        decimal additionalServiceAmount);

    Task<Payment> RecordPaymentAsync(int invoiceId, decimal amount, string method, DateTime paidAt, string userName);

    Task<bool> RefundAsync(int paymentId, string userName);

    static decimal ComputeTotal(decimal linesTotal, decimal discount, decimal taxRate)
    {
        linesTotal = Math.Max(0m, linesTotal);
        discount = Math.Clamp(discount, 0m, linesTotal);
        taxRate = Math.Clamp(taxRate, 0m, 100m);
        var taxable = linesTotal - discount;
        var tax = taxable * taxRate / 100m;
        return Math.Round(taxable + tax, 2, MidpointRounding.AwayFromZero);
    }

    static string StatusFor(decimal total, decimal paidAmount)
    {
        if (paidAmount >= total)
        {
            return EntityStatus.InvoicePaid;
        }

        return paidAmount > 0 ? EntityStatus.InvoicePartiallyPaid : EntityStatus.InvoiceUnpaid;
    }
}

public class BillingService : IBillingService
{
    private readonly HmsDbContext _db;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;

    public BillingService(HmsDbContext db, IAuditService audit, INotificationService notifications)
    {
        _db = db;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<InvoiceItem>> BuildLinesAsync(int appointmentId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        var lines = new List<InvoiceItem>();

        if (appointment.Doctor is not null)
        {
            lines.Add(new InvoiceItem
            {
                Description = $"Consultation fee - Dr. {appointment.Doctor.FullName}",
                Amount = appointment.Doctor.ConsultationFee
            });
        }

        var labRequests = await _db.LabRequests
            .Include(r => r.LaboratoryTest)
            .Where(r => r.AppointmentId == appointmentId && r.Status == EntityStatus.LabCompleted)
            .ToListAsync();

        foreach (var request in labRequests)
        {
            lines.Add(new InvoiceItem
            {
                Description = $"Lab test - {request.LaboratoryTest?.Name ?? "Laboratory test"}",
                Amount = request.LaboratoryTest?.Price ?? 0m
            });
        }

        var prescriptionItems = await _db.PrescriptionItems
            .Include(i => i.Medicine)
            .Include(i => i.Prescription)
            .Where(i => i.Prescription != null
                        && i.Prescription.MedicalRecord != null
                        && i.Prescription.MedicalRecord.AppointmentId == appointmentId)
            .ToListAsync();

        foreach (var item in prescriptionItems)
        {
            lines.Add(new InvoiceItem
            {
                Description = $"Medicine - {item.Medicine?.Name ?? "Medicine"}",
                Amount = item.Medicine?.UnitPrice ?? 0m
            });
        }

        return lines;
    }

    public async Task<Invoice> GenerateFromAppointmentAsync(
        int appointmentId,
        decimal discountAmount,
        decimal taxRate,
        string? additionalServiceDescription,
        decimal additionalServiceAmount)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        var existing = await _db.Invoices.FirstOrDefaultAsync(i => i.AppointmentId == appointmentId);
        if (existing is not null)
        {
            return existing;
        }

        var lines = (await BuildLinesAsync(appointmentId)).ToList();

        additionalServiceAmount = Math.Max(0m, additionalServiceAmount);
        if (additionalServiceAmount > 0)
        {
            lines.Add(new InvoiceItem
            {
                Description = string.IsNullOrWhiteSpace(additionalServiceDescription)
                    ? "Additional service charge"
                    : additionalServiceDescription.Trim(),
                Amount = additionalServiceAmount
            });
        }

        var linesTotal = lines.Sum(l => l.Amount);
        discountAmount = Math.Clamp(discountAmount, 0m, linesTotal);
        taxRate = Math.Clamp(taxRate, 0m, 100m);
        var invoice = new Invoice
        {
            InvoiceNumber = await NextInvoiceNumberAsync(),
            PatientId = appointment.PatientId,
            AppointmentId = appointment.Id,
            DiscountAmount = discountAmount,
            TaxRate = taxRate,
            Total = IBillingService.ComputeTotal(linesTotal, discountAmount, taxRate),
            PaidAmount = 0m,
            Status = EntityStatus.InvoiceUnpaid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var line in lines)
        {
            invoice.Items.Add(line);
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task<Payment> RecordPaymentAsync(int invoiceId, decimal amount, string method, DateTime paidAt, string userName)
    {
        if (amount <= 0m)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        var invoice = await _db.Invoices.FindAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found.");

        var outstanding = Math.Max(0m, invoice.Total - invoice.PaidAmount);
        if (amount > outstanding)
        {
            throw new InvalidOperationException("Payment cannot be more than the outstanding balance.");
        }

        var oldStatus = invoice.Status;
        var payment = new Payment
        {
            InvoiceId = invoiceId,
            Amount = amount,
            Method = method,
            PaidAt = paidAt,
            Status = EntityStatus.PaymentRecorded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        invoice.PaidAmount += amount;
        invoice.Status = IBillingService.StatusFor(invoice.Total, invoice.PaidAmount);
        var changedAt = DateTime.UtcNow;
        invoice.UpdatedAt = changedAt;

        // A payment is itself an auditable financial event. Record it even when another
        // partial payment leaves the invoice's textual status unchanged; AuditService's
        // general change-only path intentionally suppresses equal old/new values.
        _db.AuditEntries.Add(new AuditEntry
        {
            RecordType = "Invoice",
            RecordId = invoice.Id,
            FieldName = "Status",
            OldValue = oldStatus,
            NewValue = invoice.Status,
            UserName = userName,
            ChangedAt = changedAt,
            CreatedAt = changedAt,
            UpdatedAt = changedAt
        });

        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> RefundAsync(int paymentId, string userName)
    {
        var payment = await _db.Payments.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment?.Invoice is null || payment.Status == EntityStatus.PaymentRefunded)
        {
            return false;
        }

        var invoice = payment.Invoice;
        var oldStatus = invoice.Status;

        payment.Status = EntityStatus.PaymentRefunded;
        payment.UpdatedAt = DateTime.UtcNow;
        invoice.PaidAmount -= payment.Amount;
        if (invoice.PaidAmount < 0)
        {
            invoice.PaidAmount = 0m;
        }

        invoice.Status = IBillingService.StatusFor(invoice.Total, invoice.PaidAmount);
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Invoice", invoice.Id, "Status", oldStatus, invoice.Status, userName);

        var patient = await _db.Patients.FindAsync(invoice.PatientId);
        if (patient?.UserId is not null)
        {
            await _notifications.AddAsync(patient.UserId, "A payment on your invoice has been refunded.");
        }

        return true;
    }

    private async Task<string> NextInvoiceNumberAsync()
    {
        var existing = await _db.Invoices.Select(i => i.InvoiceNumber).ToListAsync();
        var highest = 0;
        foreach (var number in existing)
        {
            if (number.StartsWith("INV-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(number.AsSpan(4), out var value)
                && value > highest)
            {
                highest = value;
            }
        }

        return $"INV-{(highest + 1):D5}";
    }
}
