using Hospital.Application.Features.HealthPackages;

namespace Hospital.UnitTests.HealthPackages;

public sealed class PublicHealthPackagesQueryTests
{
    [Fact]
    public async Task GetPublishedAsyncReturnsOnlyPublishedPackages()
    {
        var source = new StubContentSource(
            Package("published", HealthPackagePublicationState.Published),
            Package("draft", HealthPackagePublicationState.Draft),
            Package("withdrawn", HealthPackagePublicationState.Withdrawn),
            Package("archived", HealthPackagePublicationState.Archived),
            Package("unavailable", HealthPackagePublicationState.Unavailable));
        var query = new PublicHealthPackagesQuery(source);

        var result = await query.GetPublishedAsync();

        var package = Assert.Single(result);
        Assert.Equal("published", package.Slug);
    }

    [Theory]
    [InlineData(HealthPackagePublicationState.Draft)]
    [InlineData(HealthPackagePublicationState.Withdrawn)]
    [InlineData(HealthPackagePublicationState.Archived)]
    [InlineData(HealthPackagePublicationState.Unavailable)]
    public async Task GetPublishedBySlugAsyncReturnsNullForEveryUnavailableState(
        HealthPackagePublicationState state)
    {
        var query = new PublicHealthPackagesQuery(new StubContentSource(Package("private-package", state)));

        var result = await query.GetPublishedBySlugAsync("private-package");

        Assert.Null(result);
    }

    [Fact]
    public async Task QueriesReflectPublicationStateAndDetailChangesFromSource()
    {
        var source = new StubContentSource(Package("changing", HealthPackagePublicationState.Draft));
        var query = new PublicHealthPackagesQuery(source);
        Assert.Null(await query.GetPublishedBySlugAsync("changing"));

        source.Items[0] = new HealthPackageContent(
            "changing",
            "Updated Package",
            "Updated description",
            new[] { "Updated public detail" },
            HealthPackagePublicationState.Published);

        var result = await query.GetPublishedBySlugAsync("CHANGING");

        Assert.NotNull(result);
        Assert.Equal("Updated Package", result.Name);
        Assert.Equal("Updated public detail", Assert.Single(result.Details));
    }

    private static HealthPackageContent Package(string slug, HealthPackagePublicationState state) =>
        new(slug, slug, $"{slug} description", new[] { $"{slug} detail" }, state);

    private sealed class StubContentSource : IHealthPackageContentSource
    {
        public StubContentSource(params HealthPackageContent[] items) => Items = items.ToList();

        public List<HealthPackageContent> Items { get; }

        public Task<IReadOnlyList<HealthPackageContent>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HealthPackageContent>>(Items);
    }
}
