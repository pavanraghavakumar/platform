using Hospital.Application.Features.PatientAuthentication;

namespace Hospital.UnitTests.PatientAuthentication;

public sealed class PatientAuthenticationResultTests
{
    [Fact]
    public void PatientAuthenticationSuccessCarriesOnlyTheAuthenticatedAccountId()
    {
        var result = PatientAuthenticationResult.Success("account-123");

        Assert.True(result.IsSuccess);
        Assert.Equal(PatientAuthenticationStatus.Success, result.Status);
        Assert.Equal("account-123", result.AccountId);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void PatientAuthenticationFailureDoesNotCreateASuccessfulResult()
    {
        var result = new PatientAuthenticationResult(PatientAuthenticationStatus.InvalidCredentials);

        Assert.False(result.IsSuccess);
        Assert.Null(result.AccountId);
    }
}
