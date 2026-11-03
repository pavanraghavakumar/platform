namespace Hms.Web.Models.Entities;

public class MedicineBatch
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public Medicine? Medicine { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime ExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
