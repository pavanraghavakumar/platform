namespace Hospital.Web.ViewModels.Departments;

public sealed record DepartmentCardViewModel(
    string Name,
    string Slug,
    string ImagePath,
    string Description);

public sealed record DepartmentIndexViewModel(IReadOnlyList<DepartmentCardViewModel> Departments);
