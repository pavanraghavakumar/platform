namespace Hospital.Application.Features.MedicalServices;

public sealed class MedicalServicesQuery : IMedicalServicesQuery
{
    private readonly IMedicalServicePublicStore store;

    public MedicalServicesQuery(IMedicalServicePublicStore store)
    {
        this.store = store;
    }

    public Task<IReadOnlyList<MedicalServiceSummary>> ListAsync(CancellationToken cancellationToken = default) =>
        store.ListPublishedAsync(cancellationToken);

    public Task<MedicalServiceDetails?> FindAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Task.FromResult<MedicalServiceDetails?>(null);
        }

        return store.FindPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken);
    }
}

