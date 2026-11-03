namespace Hms.Web.Models.Entities;

public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = EntityStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();

    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
