namespace Hospital.Application.Features.HealthPackages;

public interface IPublicHealthPackagesQuery
{
    Task<IReadOnlyList<PublicHealthPackage>> GetPublishedAsync(CancellationToken cancellationToken = default);

    Task<PublicHealthPackage?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
