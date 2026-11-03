using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IBillingService
{
    Task<Invoice> GenerateFromAppointmentAsync(int appointmentId);

    Task<Payment> RecordPaymentAsync(int invoiceId, decimal amount, string method, DateTime paidAt, string userName);

    Task<bool> RefundAsync(int paymentId, string userName);

    static decimal ComputeTotal(decimal linesTotal, decimal discount, decimal taxRate)
    {
        var taxable = linesTotal - discount;
        var tax = taxable * taxRate / 100m;
        return Math.Round(taxable + tax, 2, MidpointRounding.AwayFromZero);
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

    public async Task<Invoice> GenerateFromAppointmentAsync(int appointmentId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PatientId = appointment.PatientId,
            AppointmentId = appointment.Id,
            Status = EntityStatus.InvoiceUnpaid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (appointment.Doctor is not null)
        {
            invoice.Items.Add(new InvoiceItem
            {
                Description = $"Consultation fee - Dr. {appointment.Doctor.FullName}",
                Amount = appointment.Doctor.ConsultationFee
            });
        }

        invoice.Total = IBillingService.ComputeTotal(invoice.Items.Sum(i => i.Amount), 0m, 0m);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task<Payment> RecordPaymentAsync(int invoiceId, decimal amount, string method, DateTime paidAt, string userName)
    {
        var invoice = await _db.Invoices.FindAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found.");

        var outstanding = invoice.Total - invoice.PaidAmount;
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
        invoice.Status = invoice.PaidAmount >= invoice.Total
            ? EntityStatus.InvoicePaid
            : invoice.PaidAmount > 0
                ? EntityStatus.InvoicePartiallyPaid
                : EntityStatus.InvoiceUnpaid;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Invoice", invoice.Id, "Status", oldStatus, invoice.Status, userName);
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
        invoice.Status = invoice.PaidAmount >= invoice.Total
            ? EntityStatus.InvoicePaid
            : invoice.PaidAmount > 0
                ? EntityStatus.InvoicePartiallyPaid
                : EntityStatus.InvoiceUnpaid;
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
}
