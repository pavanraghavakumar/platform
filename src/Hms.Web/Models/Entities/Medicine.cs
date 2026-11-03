namespace Hms.Web.Models.Entities;

public class Medicine
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public MedicineCategory? Category { get; set; }

    public int? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public decimal UnitPrice { get; set; }

    public int ReorderLevel { get; set; }

    public int StockQuantity { get; set; }

    public string Status { get; set; } = EntityStatus.MedicineInStock;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();

    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
