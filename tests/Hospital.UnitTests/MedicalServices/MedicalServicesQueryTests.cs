using Hospital.Application.Features.MedicalServices;

namespace Hospital.UnitTests.MedicalServices;

public sealed class MedicalServicesQueryTests
{
    [Fact]
    public async Task MedicalServicesListReturnsStoreProjectionWithoutStaleCopy()
    {
        var store = new MutableMedicalServiceStore();
        var query = new MedicalServicesQuery(store);
        store.Items.Add(new MedicalServiceSummary("heart-care", "Heart Care", "Original description", "/heart.webp"));

        var first = await query.ListAsync();
        store.Items[0] = store.Items[0] with { Description = "Updated public description" };
        var second = await query.ListAsync();

        Assert.Equal("Original description", first.Single().Description);
        Assert.Equal("Updated public description", second.Single().Description);
    }

    [Fact]
    public async Task MedicalServicesFindNormalizesSlugAndReturnsPublishedProjection()
    {
        var expected = new MedicalServiceDetails(
            "heart-care", "Heart Care", "Description", "/heart.webp", "Details", "Care information");
        var store = new MutableMedicalServiceStore { Details = expected };
        var query = new MedicalServicesQuery(store);

        var result = await query.FindAsync("  HEART-CARE  ");

        Assert.Same(expected, result);
        Assert.Equal("heart-care", store.RequestedSlug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MedicalServicesFindRejectsBlankSlugs(string slug)
    {
        var store = new MutableMedicalServiceStore();
        var query = new MedicalServicesQuery(store);

        Assert.Null(await query.FindAsync(slug));
        Assert.Null(store.RequestedSlug);
    }

    private sealed class MutableMedicalServiceStore : IMedicalServicePublicStore
    {
        public List<MedicalServiceSummary> Items { get; } = new();
        public MedicalServiceDetails? Details { get; init; }
        public string? RequestedSlug { get; private set; }

        public Task<IReadOnlyList<MedicalServiceSummary>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MedicalServiceSummary>>(Items.ToArray());

        public Task<MedicalServiceDetails?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            RequestedSlug = slug;
            return Task.FromResult(Details);
        }
    }
}

