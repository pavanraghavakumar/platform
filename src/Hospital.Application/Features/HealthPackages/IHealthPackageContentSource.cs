namespace Hospital.Application.Features.HealthPackages;

public interface IHealthPackageContentSource
{
    Task<IReadOnlyList<HealthPackageContent>> GetAllAsync(CancellationToken cancellationToken = default);
}
