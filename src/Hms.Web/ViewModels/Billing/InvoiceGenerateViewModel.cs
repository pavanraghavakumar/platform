namespace Hms.Web.ViewModels.Billing;

/// <summary>
/// The generate-invoice form: the appointment's chargeable lines, an optional additional
/// service charge, and the discount and tax rate the Receptionist sets.
/// </summary>
public class InvoiceGenerateViewModel
{
    public int AppointmentId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string PatientCode { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }

    public IReadOnlyList<InvoiceLinePreview> Lines { get; set; } = new List<InvoiceLinePreview>();

    public string? AdditionalServiceDescription { get; set; }

    public decimal AdditionalServiceAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal LinesTotal => Lines.Sum(l => l.Amount) + AdditionalServiceAmount;

    public decimal Taxable => LinesTotal - DiscountAmount;

    public decimal Tax => Math.Round(Taxable * TaxRate / 100m, 2, MidpointRounding.AwayFromZero);

    public decimal Total => Math.Round(Taxable + Tax, 2, MidpointRounding.AwayFromZero);
}

/// <summary>One line the invoice will carry, shown before the invoice is saved.</summary>
public class InvoiceLinePreview
{
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
