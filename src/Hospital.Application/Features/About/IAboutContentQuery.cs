namespace Hospital.Application.Features.About;

public interface IAboutContentQuery
{
    Task<AboutContent> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record AboutContent(
    string Eyebrow,
    string Title,
    string Introduction,
    AboutImage? HeroImage,
    IReadOnlyList<AboutStatistic> Statistics,
    string QualityTitle,
    IReadOnlyList<string> QualityParagraphs,
    AboutImage? QualityImage,
    IReadOnlyList<AboutHighlight> QualityHighlights);

public sealed record AboutStatistic(string Value, string Label);

public sealed record AboutHighlight(string Title, string Description);

public sealed record AboutImage(
    string Source,
    string SourceSet,
    string AlternativeText,
    int Width,
    int Height);

public static class AboutContentSanitizer
{
    public static AboutContent Create(
        string? eyebrow,
        string? title,
        string? introduction,
        AboutImage? heroImage,
        IEnumerable<AboutStatistic?>? statistics,
        string? qualityTitle,
        IEnumerable<string?>? qualityParagraphs,
        AboutImage? qualityImage,
        IEnumerable<AboutHighlight?>? qualityHighlights)
    {
        return new AboutContent(
            Clean(eyebrow),
            Clean(title),
            Clean(introduction),
            IsUsable(heroImage) ? heroImage : null,
            statistics?.Where(item => item is not null && HasText(item.Value) && HasText(item.Label)).Cast<AboutStatistic>().ToArray()
                ?? Array.Empty<AboutStatistic>(),
            Clean(qualityTitle),
            qualityParagraphs?.Where(HasText).Select(Clean).ToArray() ?? Array.Empty<string>(),
            IsUsable(qualityImage) ? qualityImage : null,
            qualityHighlights?.Where(item => item is not null && HasText(item.Title) && HasText(item.Description)).Cast<AboutHighlight>().ToArray()
                ?? Array.Empty<AboutHighlight>());
    }

    private static bool IsUsable(AboutImage? image) =>
        image is not null && HasText(image.Source) && HasText(image.AlternativeText) && image.Width > 0 && image.Height > 0;

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
}
