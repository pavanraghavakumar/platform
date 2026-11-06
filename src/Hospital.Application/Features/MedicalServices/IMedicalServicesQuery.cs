namespace Hospital.Application.Features.MedicalServices;

public interface IMedicalServicesQuery
{
    Task<IReadOnlyList<MedicalServiceSummary>> ListAsync(CancellationToken cancellationToken = default);

    Task<MedicalServiceDetails?> FindAsync(string slug, CancellationToken cancellationToken = default);
}

