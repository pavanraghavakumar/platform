using Hospital.Application.Features.About;

namespace Hospital.UnitTests.About;

public sealed class AboutContentSanitizerTests
{
    [Fact]
    public void CreateOmitsMissingOptionalItemsAndTrimsPublishedText()
    {
        var content = AboutContentSanitizer.Create(
            " About us ",
            " Trusted care ",
            " Our introduction. ",
            new AboutImage("", "", "Missing source", 100, 100),
            new AboutStatistic?[] { new("30+", "Years"), null, new("", "Missing") },
            " Quality ",
            new string?[] { " Safe care. ", null, "   " },
            null,
            new AboutHighlight?[] { new("People first", "Shared decisions"), new("Incomplete", " "), null });

        Assert.Equal("About us", content.Eyebrow);
        Assert.Null(content.HeroImage);
        Assert.Collection(content.Statistics, statistic => Assert.Equal("Years", statistic.Label));
        Assert.Equal(new[] { "Safe care." }, content.QualityParagraphs);
        Assert.Collection(content.QualityHighlights, highlight => Assert.Equal("People first", highlight.Title));
    }

    [Fact]
    public void CreateKeepsUsableResponsiveImageMetadata()
    {
        var image = new AboutImage("/images/hospital.webp", "/images/hospital-720.webp 720w", "Hospital entrance", 1200, 800);

        var content = AboutContentSanitizer.Create("About", "Title", "Intro", image, null, "Quality", null, image, null);

        Assert.Same(image, content.HeroImage);
        Assert.Same(image, content.QualityImage);
    }
}
