namespace Hospital.Web.ViewModels.HealthPackages;

public sealed record HealthPackageDetailsViewModel(
    string Slug,
    string Name,
    string Description,
    IReadOnlyList<string> Details);
