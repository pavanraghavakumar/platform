using Hospital.Application.Features.Departments;

namespace Hospital.Infrastructure.PublicContent.Departments;

public sealed class DepartmentDoctorRecord
{
    public long Id { get; set; }
    public long DepartmentId { get; set; }
    public DepartmentRecord Department { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PhotoPath { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public DepartmentPublicationState PublicationState { get; set; }
}
