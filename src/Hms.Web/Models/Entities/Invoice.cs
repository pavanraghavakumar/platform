namespace Hms.Web.Models.Entities;

public class Invoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public Patient? Patient { get; set; }

    public int? AppointmentId { get; set; }

    public Appointment? Appointment { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal Total { get; set; }

    public decimal PaidAmount { get; set; }

    public string Status { get; set; } = EntityStatus.InvoiceUnpaid;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
