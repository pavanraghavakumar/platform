using Hospital.Application.Features.HealthPackages;

namespace Hospital.Infrastructure.PublicContent.HealthPackages;

public sealed class InMemoryHealthPackageContentSource : IHealthPackageContentSource
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, HealthPackageContent> packages;

    public InMemoryHealthPackageContentSource()
    {
        packages = SeedPackages().ToDictionary(package => package.Slug, StringComparer.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<HealthPackageContent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<HealthPackageContent>>(packages.Values
                .Select(Copy)
                .ToArray());
        }
    }

    public void Upsert(HealthPackageContent package)
    {
        ArgumentNullException.ThrowIfNull(package);

        lock (syncRoot)
        {
            packages[package.Slug] = Copy(package);
        }
    }

    private static HealthPackageContent Copy(HealthPackageContent package) =>
        package with { Details = package.Details.ToArray() };

    private static IEnumerable<HealthPackageContent> SeedPackages()
    {
        yield return new(
            "essential-health-check",
            "Essential Health Check",
            "A practical preventive screening package designed to establish a clear picture of your everyday health.",
            new[]
            {
                "Complete blood count and blood sugar screening",
                "Kidney and liver function profile",
                "Cholesterol and thyroid screening",
                "General physician review of your results"
            },
            HealthPackagePublicationState.Published);

        yield return new(
            "heart-wellness-profile",
            "Heart Wellness Profile",
            "Focused cardiovascular screening that helps you understand common heart-health risk indicators.",
            new[]
            {
                "Blood pressure and cardiovascular risk assessment",
                "Lipid profile and blood sugar screening",
                "Resting electrocardiogram",
                "Physician consultation with preventive guidance"
            },
            HealthPackagePublicationState.Published);

        yield return new(
            "womens-wellness-check",
            "Women's Wellness Check",
            "A considerate health review covering important routine screening needs for adult women.",
            new[]
            {
                "Complete blood count and metabolic screening",
                "Thyroid and vitamin profile",
                "Women's health consultation",
                "Personalized preventive-care guidance"
            },
            HealthPackagePublicationState.Published);
    }
}
