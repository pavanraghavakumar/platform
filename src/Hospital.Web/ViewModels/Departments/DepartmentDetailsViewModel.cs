namespace Hospital.Web.ViewModels.Departments;

public sealed record DepartmentDoctorViewModel(
    string Name,
    string Slug,
    string PhotoPath,
    string Qualifications,
    string Specialization);

public sealed record DepartmentDetailsViewModel(
    string Name,
    string Slug,
    string ImagePath,
    string Description,
    string Details,
    IReadOnlyList<DepartmentDoctorViewModel> Doctors);
