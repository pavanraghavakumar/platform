using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Billing;

/// <summary>The invoice page: the invoice, its lines and payments, and the totals to show.</summary>
public class InvoiceDetailsViewModel
{
    public Invoice Invoice { get; set; } = new();

    public IReadOnlyList<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    public IReadOnlyList<Payment> Payments { get; set; } = new List<Payment>();

    public decimal LinesTotal => Items.Sum(i => i.Amount);

    public decimal Taxable => LinesTotal - Invoice.DiscountAmount;

    public decimal Tax => Math.Round(Taxable * Invoice.TaxRate / 100m, 2, MidpointRounding.AwayFromZero);

    public decimal Outstanding => Math.Round(Invoice.Total - Invoice.PaidAmount, 2, MidpointRounding.AwayFromZero);

    public bool CanRecordPayment { get; set; }
}
