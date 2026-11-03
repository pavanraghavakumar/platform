namespace Hms.Web.Models.Entities;

public class SystemSetting
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public const string HospitalNameKey = "HospitalName";

    public const string DefaultHospitalName = "Greenfield General Hospital";
}
