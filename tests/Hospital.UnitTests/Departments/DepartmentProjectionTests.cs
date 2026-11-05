using Hospital.Application.Features.Departments;

namespace Hospital.UnitTests.Departments;

public sealed class DepartmentProjectionTests
{
    [Fact]
    public void PublicationStatesKeepPublishedDistinctFromUnavailableStates()
    {
        Assert.NotEqual(DepartmentPublicationState.Published, DepartmentPublicationState.Draft);
        Assert.NotEqual(DepartmentPublicationState.Published, DepartmentPublicationState.Archived);
    }

    [Fact]
    public void DetailsProjectionCarriesOnlyPublicDisplayDataAndDoctors()
    {
        var doctor = new DepartmentDoctor(7, "Dr Asha Rao", "asha-rao", "/media/asha.webp", "MD", "Cardiology");
        var department = new DepartmentDetails(
            3,
            "Cardiology",
            "cardiology",
            "/media/cardiology.webp",
            "Heart care",
            "Complete cardiac care.",
            new[] { doctor });

        Assert.Equal("cardiology", department.Slug);
        Assert.Equal("Dr Asha Rao", Assert.Single(department.Doctors).Name);
        Assert.DoesNotContain(typeof(DepartmentDetails).GetProperties(), property =>
            property.Name.Contains("Patient", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Account", StringComparison.OrdinalIgnoreCase));
    }
}
