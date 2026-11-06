namespace Hospital.Application.Features.MedicalServices;

public interface IMedicalServicePublicStore
{
    Task<IReadOnlyList<MedicalServiceSummary>> ListPublishedAsync(CancellationToken cancellationToken = default);

    Task<MedicalServiceDetails?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

