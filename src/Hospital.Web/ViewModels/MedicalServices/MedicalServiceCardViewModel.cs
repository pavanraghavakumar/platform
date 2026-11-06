namespace Hospital.Web.ViewModels.MedicalServices;

public sealed record MedicalServiceCardViewModel(
    string Slug,
    string Name,
    string Description,
    string ImageSource,
    string? ImageSourceSet,
    string ImageAlt);

