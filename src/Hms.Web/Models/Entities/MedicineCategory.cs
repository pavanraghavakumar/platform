namespace Hms.Web.Models.Entities;

public class MedicineCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
