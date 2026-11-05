namespace Hospital.Application.Features.Departments;

public sealed record DepartmentCard(
    long Id,
    string Name,
    string Slug,
    string ImagePath,
    string Description);

public sealed record DepartmentDoctor(
    long Id,
    string Name,
    string Slug,
    string PhotoPath,
    string Qualifications,
    string Specialization);

public sealed record DepartmentDetails(
    long Id,
    string Name,
    string Slug,
    string ImagePath,
    string Description,
    string Details,
    IReadOnlyList<DepartmentDoctor> Doctors);
