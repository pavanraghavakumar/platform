namespace Hospital.Application.Features.HealthPackages;

public sealed record HealthPackageContent(
    string Slug,
    string Name,
    string Description,
    IReadOnlyList<string> Details,
    HealthPackagePublicationState PublicationState);
