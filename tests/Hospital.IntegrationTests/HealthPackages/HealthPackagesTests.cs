using System.Net;
using System.Text.RegularExpressions;
using Hospital.Application.Features.HealthPackages;
using Hospital.Infrastructure.PublicContent.HealthPackages;
using Hospital.IntegrationTests.Fixtures;
using Hospital.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hospital.IntegrationTests.HealthPackages;

public sealed class HealthPackagesTests : IClassFixture<HospitalWebApplicationFactory>
{
    private static readonly string[] ForbiddenOperations =
    {
        "book", "checkout", "purchase", "payment", "card", "charge", "tax", "refund", "invoice"
    };

    private readonly HospitalWebApplicationFactory factory;
    private readonly HttpClient client;

    public HealthPackagesTests(HospitalWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task SimultaneousInitialIndexRequestsBothSucceed()
    {
        using var freshFactory = new HospitalWebApplicationFactory();
        using var freshClient = freshFactory.CreateClient(new() { AllowAutoRedirect = false });
        var responses = await Task.WhenAll(
            freshClient.GetAsync("/health-packages"),
            freshClient.GetAsync("/health-packages"));
        var bodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.All(bodies, html => Assert.Contains("Essential Health Check", html));
    }

    [Fact]
    public async Task IndexReturnsPublishedPackageCardsAndInformationOnlyControls()
    {
        var response = await client.GetAsync("/health-packages");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1 id=\"health-packages-title\">Health Packages</h1>", html);
        Assert.Contains("Essential Health Check", html);
        Assert.Contains("Package details", html);
        Assert.Contains("General physician review of your results", html);
        Assert.Contains("information only", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-label=\"View package details for Essential Health Check\"", html);
        AssertInformationOnlyControls(html);
    }

    [Fact]
    public async Task PageStylesUseAccessibleForegroundColors()
    {
        var response = await client.GetAsync("/css/pages/health-packages.css");
        var css = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches(
            "\\.health-package-card__link\\s*\\{[^}]*color:\\s*var\\(--color-navy\\)",
            css);
        Assert.Matches(
            "\\.health-package-not-found__mark\\s*\\{[^}]*color:\\s*var\\(--color-primary\\)",
            css);
    }

    [Fact]
    public async Task DetailsReturnsCompletePublishedPackageInformation()
    {
        var response = await client.GetAsync("/health-packages/essential-health-check");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Essential Health Check", html);
        Assert.Contains("Complete blood count and blood sugar screening", html);
        Assert.Contains("Kidney and liver function profile", html);
        Assert.Contains("Cholesterol and thyroid screening", html);
        Assert.Contains("General physician review of your results", html);
        AssertInformationOnlyControls(html);
    }

    [Fact]
    public async Task MissingPackageReturnsBrandedNotFoundWithRouteBack()
    {
        var response = await client.GetAsync("/health-packages/not-a-current-package");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Meridian Hospital", html);
        Assert.Contains("We couldn't find that health package", html);
        Assert.Contains("href=\"/health-packages\"", html);
    }

    [Fact]
    public async Task PublicationAndDetailChangesAppearWithoutChangingPublicContract()
    {
        var slug = $"integration-package-{Guid.NewGuid():N}";
        await SavePackageAsync(slug, "Hidden Integration Package", "Hidden description", "Hidden detail", HealthPackagePublicationState.Draft);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/health-packages/{slug}")).StatusCode);

        await SavePackageAsync(slug, "Published Integration Package", "Current public description", "Current public detail", HealthPackagePublicationState.Published);

        var response = await client.GetAsync($"/health-packages/{slug}");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Published Integration Package", html);
        Assert.Contains("Current public description", html);
        Assert.Contains("Current public detail", html);
        AssertInformationOnlyControls(html);

        await SavePackageAsync(slug, "Published Integration Package", "Current public description", "Current public detail", HealthPackagePublicationState.Withdrawn);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/health-packages/{slug}")).StatusCode);
    }

    private async Task SavePackageAsync(
        string slug,
        string name,
        string description,
        string detail,
        HealthPackagePublicationState state)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var existing = await database.HealthPackages.SingleOrDefaultAsync(package => package.Slug == slug);
        if (existing is null)
        {
            database.HealthPackages.Add(new HealthPackageRecord
            {
                Slug = slug,
                Name = name,
                Description = description,
                Details = JsonSerializer.Serialize(new[] { detail }),
                PublicationState = state
            });
        }
        else
        {
            existing.Name = name;
            existing.Description = description;
            existing.Details = JsonSerializer.Serialize(new[] { detail });
            existing.PublicationState = state;
        }

        await database.SaveChangesAsync();
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task PackageRouteExposesNoUnsafePublicOperation(string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/health-packages/essential-health-check");

        var response = await client.SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
        Assert.NotEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);

        if (response.Headers.Location is not null)
        {
            Assert.DoesNotContain(ForbiddenOperations, operation =>
                response.Headers.Location.ToString().Contains(operation, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void AssertInformationOnlyControls(string html)
    {
        Assert.DoesNotContain("<form", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<button", html, StringComparison.OrdinalIgnoreCase);

        var destinations = Regex.Matches(html, "(?:href|action)\\s*=\\s*[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value)
            .ToArray();

        foreach (var destination in destinations)
        {
            Assert.DoesNotContain(ForbiddenOperations, operation =>
                destination.Contains(operation, StringComparison.OrdinalIgnoreCase));
        }
    }
}
