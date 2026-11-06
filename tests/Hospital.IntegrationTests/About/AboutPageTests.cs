using System.Net;
using Hospital.Application.Features.About;
using Hospital.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hospital.IntegrationTests.About;

public sealed class AboutPageTests : IClassFixture<HospitalWebApplicationFactory>
{
    private readonly HospitalWebApplicationFactory factory;

    public AboutPageTests(HospitalWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AboutReturnsEditorialContentSemanticsImagesAndCareRoutes()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/about");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("https://images.unsplash.com", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("<main id=\"main-content\"", html);
        Assert.Contains("<article class=\"about-page\">", html);
        Assert.Contains("<h1 id=\"about-title\">Advanced medicine, delivered with humanity</h1>", html);
        Assert.Contains("Our experience and achievements", html);
        Assert.Contains("Quality of care", html);
        Assert.Contains("srcset=\"https://images.unsplash.com", html);
        Assert.Contains("width=\"1200\"", html);
        Assert.Contains("height=\"800\"", html);
        Assert.Contains("fetchpriority=\"high\"", html);
        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("href=\"/departments\"", html);
        Assert.Contains("href=\"/doctors\"", html);
        Assert.Contains("href=\"/medical-services\"", html);
        Assert.Contains("href=\"/appointments/book\"", html);
        Assert.Contains("href=\"/css/pages/about.css", html);
    }

    [Fact]
    public async Task AboutWhenOptionalItemsAreMissingOmitsTheirMarkupWithoutAnErrorPage()
    {
        using var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAboutContentQuery>();
                services.AddSingleton<IAboutContentQuery>(new MissingOptionalAboutContentQuery());
            });
        });
        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/about");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Essential hospital introduction", html);
        Assert.DoesNotContain("about-media", html);
        Assert.DoesNotContain("about-stats", html);
        Assert.DoesNotContain("about-highlights", html);
        Assert.DoesNotContain("Error", html);
    }

    [Fact]
    public async Task AboutCssDefinesPhoneTabletDesktopAndReducedMotionBehavior()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/css/pages/about.css");
        var css = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("@media (max-width: 47.99rem)", css);
        Assert.Contains("@media (min-width: 48rem) and (max-width: 74.99rem)", css);
        Assert.Contains("grid-template-columns: repeat(4", css);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", css);
        Assert.DoesNotContain("overflow-x: auto", css);
    }

    private sealed class MissingOptionalAboutContentQuery : IAboutContentQuery
    {
        public Task<AboutContent> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AboutContentSanitizer.Create(
                null,
                "Essential hospital introduction",
                "Core content remains available.",
                null,
                null,
                "Quality remains central",
                null,
                null,
                null));
    }
}
