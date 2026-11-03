namespace Hms.Web.Models.Entities;

public class LaboratoryTest
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public decimal Price { get; set; }

    public string? NormalRange { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LabRequest> Requests { get; set; } = new List<LabRequest>();
}
