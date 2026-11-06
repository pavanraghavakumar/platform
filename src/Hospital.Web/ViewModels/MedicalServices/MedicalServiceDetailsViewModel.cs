namespace Hospital.Web.ViewModels.MedicalServices;

public sealed record MedicalServiceDetailsViewModel(
    string Slug,
    string Name,
    string Description,
    string ImageSource,
    string? ImageSourceSet,
    string ImageAlt,
    string Details,
    string? CareInformation);

