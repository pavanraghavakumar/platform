using Hospital.Application.Features.About;

namespace Hospital.Web.ViewModels.About;

public sealed record AboutViewModel(
    string Eyebrow,
    string Title,
    string Introduction,
    AboutImage? HeroImage,
    IReadOnlyList<AboutStatistic> Statistics,
    string QualityTitle,
    IReadOnlyList<string> QualityParagraphs,
    AboutImage? QualityImage,
    IReadOnlyList<AboutHighlight> QualityHighlights)
{
    public static AboutViewModel From(AboutContent content) => new(
        content.Eyebrow,
        content.Title,
        content.Introduction,
        content.HeroImage,
        content.Statistics,
        content.QualityTitle,
        content.QualityParagraphs,
        content.QualityImage,
        content.QualityHighlights);
}
