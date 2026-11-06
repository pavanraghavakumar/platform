namespace Hospital.Web.ViewModels.HealthPackages;

public sealed record HealthPackagesIndexViewModel(IReadOnlyList<HealthPackageCardViewModel> Packages);
