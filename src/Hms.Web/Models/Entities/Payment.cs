namespace Hms.Web.Models.Entities;

public class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = "Cash";

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = EntityStatus.PaymentRecorded;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
