using System.Security.Claims;
using System.Globalization;
using Hospital.Application.Features.PatientAuthentication;
using Hospital.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Identity.PatientAuthentication;

public sealed class IdentityPatientAuthenticationService : IPatientAuthenticationService
{
    private static readonly Action<ILogger, Exception?> LogRegistrationFailure =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(3001, nameof(RegisterAsync)),
            "Patient registration dependency failed.");

    private static readonly Action<ILogger, Exception?> LogLoginFailure =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(3002, nameof(LoginAsync)),
            "Patient sign-in dependency failed.");

    public const string ExistingAccountMessage = "An account already exists for those details. Please sign in instead.";
    public const string InvalidCredentialsMessage = "Invalid email address, patient identifier, or password.";
    public const string UnavailableMessage = "Patient sign-in is temporarily unavailable. Please try again later.";

    private readonly UserManager<ApplicationUser> users;
    private readonly RoleManager<IdentityRole> roles;
    private readonly HospitalDbContext dbContext;
    private readonly ILogger<IdentityPatientAuthenticationService> logger;

    public IdentityPatientAuthenticationService(
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        HospitalDbContext dbContext,
        ILogger<IdentityPatientAuthenticationService> logger)
    {
        this.users = users;
        this.roles = roles;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<PatientAuthenticationResult> RegisterAsync(
        PatientRegistrationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = command.Email.Trim();
            var normalizedIdentifier = command.PatientIdentifier.Trim();
            if (await users.FindByEmailAsync(normalizedEmail) is not null
                || await users.FindByNameAsync(normalizedIdentifier) is not null)
            {
                return new(PatientAuthenticationStatus.ExistingAccount);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            if (!await roles.RoleExistsAsync("Patient"))
            {
                var roleResult = await roles.CreateAsync(new IdentityRole("Patient"));
                if (!roleResult.Succeeded)
                {
                    return Unavailable();
                }
            }

            var user = new ApplicationUser
            {
                UserName = normalizedIdentifier,
                Email = normalizedEmail,
                PhoneNumber = command.Phone.Trim(),
                DisplayName = command.FullName.Trim()
            };

            var created = await users.CreateAsync(user, command.Password);
            if (!created.Succeeded)
            {
                if (created.Errors.Any(error =>
                    error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                {
                    return new(PatientAuthenticationStatus.ExistingAccount);
                }

                return new(
                    PatientAuthenticationStatus.Validation,
                    Errors:
                    new Dictionary<string, string[]>
                    {
                        ["Password"] = created.Errors.Select(error => error.Description).ToArray()
                    });
            }

            var roleAdded = await users.AddToRoleAsync(user, "Patient");
            if (!roleAdded.Succeeded)
            {
                await users.DeleteAsync(user);
                return Unavailable();
            }

            if (command.DateOfBirth is not null)
            {
                var claimResult = await users.AddClaimAsync(
                    user,
                    new Claim(
                        "date_of_birth",
                        command.DateOfBirth.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                if (!claimResult.Succeeded)
                {
                    await users.DeleteAsync(user);
                    return Unavailable();
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return PatientAuthenticationResult.Success(user.Id);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRegistrationFailure(logger, exception);
            return Unavailable();
        }
    }

    public async Task<PatientAuthenticationResult> LoginAsync(
        PatientLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var identifier = command.Identifier.Trim();
            var user = identifier.Contains('@', StringComparison.Ordinal)
                ? await users.FindByEmailAsync(identifier)
                : await users.FindByNameAsync(identifier);

            if (user is null || !await users.IsInRoleAsync(user, "Patient"))
            {
                return new(PatientAuthenticationStatus.InvalidCredentials);
            }

            if (await users.IsLockedOutAsync(user) || !await users.CheckPasswordAsync(user, command.Password))
            {
                await users.AccessFailedAsync(user);
                return new(PatientAuthenticationStatus.InvalidCredentials);
            }

            await users.ResetAccessFailedCountAsync(user);
            return PatientAuthenticationResult.Success(user.Id);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogLoginFailure(logger, exception);
            return Unavailable();
        }
    }

    private static PatientAuthenticationResult Unavailable() =>
        new(PatientAuthenticationStatus.Unavailable);
}
