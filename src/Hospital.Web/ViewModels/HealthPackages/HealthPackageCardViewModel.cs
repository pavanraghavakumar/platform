namespace Hospital.Web.ViewModels.HealthPackages;

public sealed record HealthPackageCardViewModel(
    string Slug,
    string Name,
    string Description,
    IReadOnlyList<string> Details);
