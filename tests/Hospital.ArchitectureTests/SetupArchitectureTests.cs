using Hospital.Application.Common;
using Hospital.Domain.Models;
using Hospital.Infrastructure.Data;
using System.Text.Json;

namespace Hospital.ArchitectureTests;

public sealed class SetupArchitectureTests
{
    [Fact]
    public void SetupLayersReferenceOnlyInward()
    {
        var domainReferences = typeof(Entity).Assembly.GetReferencedAssemblies().Select(reference => reference.Name);
        var applicationReferences = typeof(Result).Assembly.GetReferencedAssemblies().Select(reference => reference.Name);

        Assert.DoesNotContain("Hospital.Application", domainReferences);
        Assert.DoesNotContain("Hospital.Infrastructure", domainReferences);
        Assert.DoesNotContain("Hospital.Web", domainReferences);
        Assert.DoesNotContain("Hospital.Infrastructure", applicationReferences);
        Assert.DoesNotContain("Hospital.Web", applicationReferences);
        Assert.NotNull(typeof(HospitalDbContext));
    }

    [Fact]
    public void SetupBareDotnetRunUsesDevelopmentEnvironment()
    {
        var launchSettingsPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../src/Hospital.Web/Properties/launchSettings.json"));

        using var launchSettings = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        var profile = launchSettings.RootElement
            .GetProperty("profiles")
            .GetProperty("Hospital.Web");

        Assert.Equal("Project", profile.GetProperty("commandName").GetString());
        Assert.Equal(
            "Development",
            profile.GetProperty("environmentVariables").GetProperty("ASPNETCORE_ENVIRONMENT").GetString());
    }
}
