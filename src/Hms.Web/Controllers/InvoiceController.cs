using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Controllers;

[Authorize]
public class InvoiceController : Controller
{
    private readonly HmsDbContext _db;
    private readonly IBillingService _billing;
    private readonly UserManager<ApplicationUser> _userManager;

    public InvoiceController(HmsDbContext db, IBillingService billing, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _billing = billing;
        _userManager = userManager;
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin)]
    public async Task<IActionResult> Index(string? status)
    {
        var query = _db.Invoices.Include(i => i.Patient).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        ViewData["Status"] = status;
        return View(await query.OrderByDescending(i => i.CreatedAt).ToListAsync());
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpGet]
    public async Task<IActionResult> Generate(int appointmentId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null)
        {
            return NotFound();
        }

        var lines = await _billing.BuildLinesAsync(appointmentId);
        var model = new InvoiceGenerateViewModel
        {
            AppointmentId = appointment.Id,
            PatientName = appointment.Patient?.FullName ?? string.Empty,
            PatientCode = appointment.Patient?.PatientCode ?? string.Empty,
            DoctorName = appointment.Doctor?.FullName ?? string.Empty,
            AppointmentDate = appointment.AppointmentDate,
            Lines = lines.Select(l => new InvoiceLinePreview { Description = l.Description, Amount = l.Amount }).ToList()
        };

        return View(model);
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        int appointmentId,
        decimal discountAmount,
        decimal taxRate,
        string? additionalServiceDescription,
        decimal additionalServiceAmount)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null)
        {
            return NotFound();
        }

        var lines = await _billing.BuildLinesAsync(appointmentId);
        additionalServiceAmount = Math.Max(0m, additionalServiceAmount);
        var linesTotal = lines.Sum(line => line.Amount) + additionalServiceAmount;
        discountAmount = Math.Clamp(discountAmount, 0m, linesTotal);
        taxRate = Math.Clamp(taxRate, 0m, 100m);

        var invoice = await _billing.GenerateFromAppointmentAsync(
            appointmentId,
            discountAmount,
            taxRate,
            additionalServiceDescription,
            additionalServiceAmount);

        TempData["Message"] = "Invoice generated.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin + "," + Roles.Patient)]
    public async Task<IActionResult> Details(int id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice is null)
        {
            return NotFound();
        }

        if (!await CanAccessAsync(invoice))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(BuildDetails(invoice));
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin + "," + Roles.Patient)]
    public async Task<IActionResult> Print(int id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice is null)
        {
            return NotFound();
        }

        if (!await CanAccessAsync(invoice))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(BuildDetails(invoice));
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal? amount, string method, DateTime paidAt)
    {
        if (!amount.HasValue || amount.Value <= 0m)
        {
            TempData["Error"] = "Payment amount must be greater than zero.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.GetUserAsync(User);
        try
        {
            await _billing.RecordPaymentAsync(id, amount.Value, method, paidAt, user?.UserName ?? string.Empty);
            TempData["Message"] = "Payment recorded.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int paymentId)
    {
        var user = await _userManager.GetUserAsync(User);
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
        var invoiceId = payment?.InvoiceId;

        var refunded = await _billing.RefundAsync(paymentId, user?.UserName ?? string.Empty);
        TempData[refunded ? "Message" : "Error"] = refunded
            ? "Payment refunded."
            : "This payment cannot be refunded.";

        if (invoiceId is not null)
        {
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<Invoice?> LoadInvoiceAsync(int id)
    {
        return await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    private async Task<bool> CanAccessAsync(Invoice invoice)
    {
        if (User.IsInRole(Roles.Receptionist) || User.IsInRole(Roles.Admin))
        {
            return true;
        }

        if (!User.IsInRole(Roles.Patient))
        {
            return false;
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return false;
        }

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        return patient is not null && patient.Id == invoice.PatientId;
    }

    private InvoiceDetailsViewModel BuildDetails(Invoice invoice)
    {
        return new InvoiceDetailsViewModel
        {
            Invoice = invoice,
            Items = invoice.Items.OrderBy(i => i.Id).ToList(),
            Payments = invoice.Payments.OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id).ToList(),
            CanRecordPayment = User.IsInRole(Roles.Receptionist)
                               && invoice.Status != EntityStatus.InvoicePaid
                               && invoice.Status != EntityStatus.InvoiceCancelled
        };
    }
}
