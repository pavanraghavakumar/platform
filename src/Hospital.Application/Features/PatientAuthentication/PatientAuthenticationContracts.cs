namespace Hospital.Application.Features.PatientAuthentication;

public enum PatientAuthenticationStatus
{
    Success,
    Validation,
    ExistingAccount,
    InvalidCredentials,
    Unavailable
}

public sealed record PatientRegistrationCommand(
    string Email,
    string PatientIdentifier,
    string Password,
    string FullName,
    DateOnly? DateOfBirth,
    string Phone);

public sealed record PatientLoginCommand(string Identifier, string Password, bool RememberMe);

public sealed record PatientAuthenticationResult(
    PatientAuthenticationStatus Status,
    string? AccountId = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public bool IsSuccess => Status == PatientAuthenticationStatus.Success;

    public static PatientAuthenticationResult Success(string accountId) =>
        new(PatientAuthenticationStatus.Success, accountId);
}

public interface IPatientAuthenticationService
{
    Task<PatientAuthenticationResult> RegisterAsync(
        PatientRegistrationCommand command,
        CancellationToken cancellationToken = default);

    Task<PatientAuthenticationResult> LoginAsync(
        PatientLoginCommand command,
        CancellationToken cancellationToken = default);
}

public interface IPatientContinuationService
{
    string Create(string localDestination);
    string? Consume(string token);
}
