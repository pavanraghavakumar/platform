namespace Hospital.Application.Features.HealthPackages;

public sealed class PublicHealthPackagesQuery : IPublicHealthPackagesQuery
{
    private readonly IHealthPackageContentSource contentSource;

    public PublicHealthPackagesQuery(IHealthPackageContentSource contentSource)
    {
        this.contentSource = contentSource;
    }

    public async Task<IReadOnlyList<PublicHealthPackage>> GetPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var packages = await contentSource.GetAllAsync(cancellationToken);

        return packages
            .Where(IsPublished)
            .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToPublicPackage)
            .ToArray();
    }

    public async Task<PublicHealthPackage?> GetPublishedBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var packages = await contentSource.GetAllAsync(cancellationToken);
        var package = packages.FirstOrDefault(item =>
            IsPublished(item) && string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));

        return package is null ? null : ToPublicPackage(package);
    }

    private static bool IsPublished(HealthPackageContent package) =>
        package.PublicationState == HealthPackagePublicationState.Published;

    private static PublicHealthPackage ToPublicPackage(HealthPackageContent package) =>
        new(package.Slug, package.Name, package.Description, package.Details.ToArray());
}
