namespace Hospital.Application.Features.HealthPackages;

public sealed record PublicHealthPackage(
    string Slug,
    string Name,
    string Description,
    IReadOnlyList<string> Details);
