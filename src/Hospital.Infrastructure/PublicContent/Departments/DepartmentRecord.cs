using Hospital.Application.Features.Departments;

namespace Hospital.Infrastructure.PublicContent.Departments;

public sealed class DepartmentRecord
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DepartmentPublicationState PublicationState { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public ICollection<DepartmentDoctorRecord> Doctors { get; set; } = new List<DepartmentDoctorRecord>();
}
