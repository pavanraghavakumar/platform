namespace Hospital.Application.Features.MedicalServices;

public sealed record MedicalServiceSummary(
    string Slug,
    string Name,
    string Description,
    string? ImagePath);

public sealed record MedicalServiceDetails(
    string Slug,
    string Name,
    string Description,
    string? ImagePath,
    string Details,
    string? CareInformation);

