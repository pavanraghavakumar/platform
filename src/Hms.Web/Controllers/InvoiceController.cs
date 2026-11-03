using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
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

        return View(appointment);
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int appointmentId, decimal discountAmount, decimal taxRate)
    {
        var invoice = await _billing.GenerateFromAppointmentAsync(appointmentId);
        invoice.DiscountAmount = discountAmount;
        invoice.TaxRate = taxRate;
        invoice.Total = IBillingService.ComputeTotal(invoice.Items.Sum(i => i.Amount), discountAmount, taxRate);
        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin + "," + Roles.Patient)]
    public async Task<IActionResult> Details(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    [Authorize(Roles = Roles.Receptionist + "," + Roles.Admin + "," + Roles.Patient)]
    public async Task<IActionResult> Print(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    [Authorize(Roles = Roles.Receptionist)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal amount, string method, DateTime paidAt)
    {
        var user = await _userManager.GetUserAsync(User);
        try
        {
            await _billing.RecordPaymentAsync(id, amount, method, paidAt, user?.UserName ?? string.Empty);
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
        await _billing.RefundAsync(paymentId, user?.UserName ?? string.Empty);
        return RedirectToAction(nameof(Index));
    }
}
