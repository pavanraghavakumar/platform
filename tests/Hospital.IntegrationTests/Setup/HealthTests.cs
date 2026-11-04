using System.Net;
using System.Text.Json;
using Hospital.IntegrationTests.Fixtures;

namespace Hospital.IntegrationTests.Setup;

public sealed class HealthTests : IClassFixture<HospitalWebApplicationFactory>
{
    private readonly HospitalWebApplicationFactory factory;

    public HealthTests(HospitalWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthReturnsHealthyWithoutSensitiveDetails()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain("Data Source", body, StringComparison.OrdinalIgnoreCase);
    }
}
