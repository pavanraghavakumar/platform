using Hospital.Application.Features.HealthPackages;

namespace Hospital.Infrastructure.PublicContent.HealthPackages;

public sealed class HealthPackageRecord
{
    public long Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Details { get; set; } = "[]";

    public HealthPackagePublicationState PublicationState { get; set; }
}
