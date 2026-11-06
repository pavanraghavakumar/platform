using System.Net;
using System.Text.RegularExpressions;
using Hospital.Application.Features.PatientAuthentication;
using Hospital.IntegrationTests.Fixtures;
using Hospital.Infrastructure.Data;
using Hospital.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hospital.IntegrationTests.PatientAuthentication;

public sealed partial class PatientAuthenticationTests : IClassFixture<HospitalWebApplicationFactory>
{
    private readonly HospitalWebApplicationFactory factory;

    public PatientAuthenticationTests(HospitalWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PatientAuthenticationPagesShowRequiredFieldsAndJourneyLinks()
    {
        using var client = CreateClient();

        var login = await client.GetStringAsync("/patient/login");
        var registration = await client.GetStringAsync("/patient/register");

        Assert.Contains("Patient Login", login, StringComparison.Ordinal);
        Assert.Contains("name=\"Identifier\"", login, StringComparison.Ordinal);
        Assert.Contains("name=\"Password\"", login, StringComparison.Ordinal);
        Assert.Contains("name=\"RememberMe\"", login, StringComparison.Ordinal);
        Assert.Contains("data-password-toggle", login, StringComparison.Ordinal);
        Assert.Contains("/patient/register", login, StringComparison.Ordinal);

        foreach (var field in new[] { "Email", "PatientIdentifier", "Password", "ConfirmPassword", "FullName", "DateOfBirth", "Phone" })
        {
            Assert.Contains($"name=\"{field}\"", registration, StringComparison.Ordinal);
        }
        Assert.Contains("novalidate", registration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-validation-summary=\"true\"", registration, StringComparison.Ordinal);
        Assert.Contains("hidden=\"hidden\"", registration, StringComparison.Ordinal);
        Assert.Contains("required", InputFor(registration, "Email"), StringComparison.Ordinal);
        Assert.Contains("pattern=\"[A-Za-z0-9\\-]{4,32}\"", registration, StringComparison.Ordinal);
        Assert.Contains("minlength=\"12\"", InputFor(registration, "Password"), StringComparison.Ordinal);
        Assert.Contains("required", InputFor(registration, "ConfirmPassword"), StringComparison.Ordinal);
        Assert.Contains("required", InputFor(registration, "FullName"), StringComparison.Ordinal);
        Assert.Contains("required", InputFor(registration, "Phone"), StringComparison.Ordinal);
        Assert.Contains("/patient/login", registration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PatientLandingRequiresAuthentication()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/patient");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/patient/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task InvalidCredentialsUseOneGenericMessageAndDoNotCreateSession()
    {
        using var client = CreateClient();
        var form = await GetFormAsync(client, "/patient/login");

        using var response = await client.PostAsync("/patient/login", Form(
            form.Token,
            ("Identifier", "missing@example.test"),
            ("Password", "Wrong-Password-2026!")));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid email address, patient identifier, or password.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("missing@example.test does not exist", body, StringComparison.OrdinalIgnoreCase);
        using var landing = await client.GetAsync("/patient");
        Assert.Equal(HttpStatusCode.Redirect, landing.StatusCode);
    }

    [Fact]
    public async Task RegistrationCreatesPatientAndRedisplayNeverIncludesPassword()
    {
        using var client = CreateClient();
        var unique = Guid.NewGuid().ToString("N");
        var email = $"patient-{unique}@example.test";
        var identifier = $"P-{unique[..12]}";
        const string password = "Patient-New-2026!";
        var form = await GetFormAsync(client, "/patient/register");

        using var response = await client.PostAsync("/patient/register", Form(
            form.Token,
            ("Email", email),
            ("PatientIdentifier", identifier),
            ("Password", password),
            ("ConfirmPassword", password),
            ("FullName", "New Patient"),
            ("DateOfBirth", "1990-04-12"),
            ("Phone", "+910000000001")));

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal("/patient", response.Headers.Location?.OriginalString);
        using var landing = await client.GetAsync("/patient");
        Assert.Equal(HttpStatusCode.OK, landing.StatusCode);

        using var secondClient = CreateClient();
        var duplicateForm = await GetFormAsync(secondClient, "/patient/register");
        using var duplicate = await secondClient.PostAsync("/patient/register", Form(
            duplicateForm.Token,
            ("Email", email),
            ("PatientIdentifier", $"OTHER-{unique[..8]}"),
            ("Password", password),
            ("ConfirmPassword", password),
            ("FullName", "New Patient"),
            ("Phone", "+910000000001")));
        var duplicateBody = await duplicate.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Contains("An account already exists for those details. Please sign in instead.", duplicateBody, StringComparison.Ordinal);
        Assert.Contains("/patient/login", duplicateBody, StringComparison.Ordinal);
        Assert.Contains(email, duplicateBody, StringComparison.Ordinal);
        Assert.DoesNotContain(password, duplicateBody, StringComparison.Ordinal);

        using var loginClient = CreateClient();
        var loginForm = await GetFormAsync(loginClient, "/patient/login");
        using var login = await loginClient.PostAsync("/patient/login", Form(
            loginForm.Token,
            ("Identifier", identifier),
            ("Password", password)));
        Assert.Equal(HttpStatusCode.SeeOther, login.StatusCode);
        Assert.Equal("/patient", login.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RegistrationDependencyFailureRollsBackCreatedAccount()
    {
        var unique = Guid.NewGuid().ToString("N");
        var email = $"rollback-{unique}@example.test";
        var identifier = $"R-{unique[..12]}";

        await using (var setupScope = factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER FailPatientDateOfBirthClaim
                BEFORE INSERT ON AspNetUserClaims
                BEGIN
                    SELECT RAISE(ABORT, 'simulated claim dependency failure');
                END;
                """);
        }

        try
        {
            using var client = CreateClient();
            var form = await GetFormAsync(client, "/patient/register");
            using var response = await client.PostAsync("/patient/register", Form(
                form.Token,
                ("Email", email),
                ("PatientIdentifier", identifier),
                ("Password", "Patient-New-2026!"),
                ("ConfirmPassword", "Patient-New-2026!"),
                ("FullName", "Rollback Patient"),
                ("DateOfBirth", "1990-04-12"),
                ("Phone", "+910000000001")));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            await using var verificationScope = factory.Services.CreateAsyncScope();
            var users = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.Null(await users.FindByEmailAsync(email));
            Assert.Null(await users.FindByNameAsync(identifier));
        }
        finally
        {
            await using var cleanupScope = factory.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS FailPatientDateOfBirthClaim;");
        }
    }

    [Fact]
    public async Task SuccessfulLoginConsumesOnlyOpaqueLocalContinuation()
    {
        string token;
        using (var scope = factory.Services.CreateScope())
        {
            token = scope.ServiceProvider.GetRequiredService<IPatientContinuationService>()
                .Create("/appointments/book?step=personal-information");
        }

        using var client = CreateClient();
        var form = await GetFormAsync(client, $"/patient/login?returnToken={token}");
        using var response = await client.PostAsync("/patient/login", Form(
            form.Token,
            ("Identifier", "patient@example.test"),
            ("Password", "Greenfield-Patient-2026!"),
            ("ReturnToken", token)));

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal("/appointments/book?step=personal-information", response.Headers.Location?.OriginalString);

        using var secondClient = CreateClient();
        var secondForm = await GetFormAsync(secondClient, "/patient/login");
        using var replay = await secondClient.PostAsync("/patient/login", Form(
            secondForm.Token,
            ("Identifier", "patient@example.test"),
            ("Password", "Greenfield-Patient-2026!"),
            ("ReturnToken", token)));
        Assert.Equal("/patient", replay.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AuthenticationServiceFailureReturnsRetryLaterWithoutSession()
    {
        using var unavailableFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPatientAuthenticationService>();
                services.AddScoped<IPatientAuthenticationService, UnavailableAuthenticationService>();
            }));
        using var client = unavailableFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = await GetFormAsync(client, "/patient/login");

        using var response = await client.PostAsync("/patient/login", Form(
            form.Token,
            ("Identifier", "patient@example.test"),
            ("Password", "Greenfield-Patient-2026!")));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Patient sign-in is temporarily unavailable. Please try again later.", body, StringComparison.Ordinal);
        using var landing = await client.GetAsync("/patient");
        Assert.Equal(HttpStatusCode.Redirect, landing.StatusCode);
    }

    [Fact]
    public async Task SessionIssuanceFailureAfterValidLoginReturnsRetryLaterWithoutSession()
    {
        using var unavailableFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPatientAuthenticationService>();
                services.AddScoped<IPatientAuthenticationService, SessionUnavailableAuthenticationService>();
            }));
        using var client = unavailableFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = await GetFormAsync(client, "/patient/login");

        using var response = await client.PostAsync("/patient/login", Form(
            form.Token,
            ("Identifier", "patient@example.test"),
            ("Password", "Greenfield-Patient-2026!")));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Patient sign-in is temporarily unavailable. Please try again later.", body, StringComparison.Ordinal);
        using var landing = await client.GetAsync("/patient");
        Assert.Equal(HttpStatusCode.Redirect, landing.StatusCode);
    }

    [Fact]
    public async Task SessionIssuanceFailureAfterPersistedRegistrationRemovesAccountBeforeRetryLaterResponse()
    {
        var unique = Guid.NewGuid().ToString("N");
        var email = $"session-failure-{unique}@example.test";
        var identifier = $"S-{unique[..12]}";
        using var unavailableFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ThrowingSignInManager>();
                services.AddScoped<SignInManager<ApplicationUser>>(provider =>
                    provider.GetRequiredService<ThrowingSignInManager>());
            }));
        using var client = unavailableFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = await GetFormAsync(client, "/patient/register");

        using var response = await client.PostAsync("/patient/register", Form(
            form.Token,
            ("Email", email),
            ("PatientIdentifier", identifier),
            ("Password", "Patient-New-2026!"),
            ("ConfirmPassword", "Patient-New-2026!"),
            ("FullName", "New Patient"),
            ("Phone", "+910000000001")));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Patient sign-in is temporarily unavailable. Please try again later.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Patient-New-2026!", body, StringComparison.Ordinal);
        using var landing = await client.GetAsync("/patient");
        Assert.Equal(HttpStatusCode.Redirect, landing.StatusCode);

        await using var verificationScope = unavailableFactory.Services.CreateAsyncScope();
        var users = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync(email));
        Assert.Null(await users.FindByNameAsync(identifier));
    }

    [Theory]
    [InlineData("/patient/login", "Identifier", "Enter your email address or patient identifier.")]
    [InlineData("/patient/register", "Email", "Enter your email address.")]
    public async Task InvalidAuthenticationFieldsAreLinkedFromValidationSummary(
        string path,
        string field,
        string message)
    {
        using var client = CreateClient();
        var form = await GetFormAsync(client, path);

        using var response = await client.PostAsync(path, Form(form.Token));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains($"href=\"#{field}\"", body, StringComparison.Ordinal);
        Assert.Contains(message, body, StringComparison.Ordinal);
        Assert.Contains($"id=\"{field}\"", body, StringComparison.Ordinal);
    }

    private HttpClient CreateClient() => factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<(string Token, string Html)> GetFormAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var input = AntiforgeryInputPattern().Match(html);
        var value = input.Success ? ValueAttributePattern().Match(input.Value) : Match.Empty;
        Assert.True(value.Success, $"No antiforgery token was rendered by {path}.");
        return (WebUtility.HtmlDecode(value.Groups[1].Value), html);
    }

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
    {
        var values = fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)).ToList();
        values.Add(new KeyValuePair<string, string>("__RequestVerificationToken", token));
        return new FormUrlEncodedContent(values);
    }

    private static string InputFor(string html, string fieldName)
    {
        var input = Regex.Match(html, $"<input[^>]*name=\"{Regex.Escape(fieldName)}\"[^>]*>");
        Assert.True(input.Success, $"No input was rendered for {fieldName}.");
        return input.Value;
    }

    [GeneratedRegex("<input[^>]*name=\"__RequestVerificationToken\"[^>]*>")]
    private static partial Regex AntiforgeryInputPattern();

    [GeneratedRegex("value=\"([^\"]+)\"")]
    private static partial Regex ValueAttributePattern();

    private sealed class UnavailableAuthenticationService : IPatientAuthenticationService
    {
        public Task<PatientAuthenticationResult> RegisterAsync(
            PatientRegistrationCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PatientAuthenticationResult(PatientAuthenticationStatus.Unavailable));

        public Task<PatientAuthenticationResult> LoginAsync(
            PatientLoginCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PatientAuthenticationResult(PatientAuthenticationStatus.Unavailable));

    }

    private sealed class SessionUnavailableAuthenticationService : IPatientAuthenticationService
    {
        public Task<PatientAuthenticationResult> RegisterAsync(
            PatientRegistrationCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PatientAuthenticationResult.Success("registered-patient"));

        public Task<PatientAuthenticationResult> LoginAsync(
            PatientLoginCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PatientAuthenticationResult.Success("authenticated-patient"));

    }

    private sealed class ThrowingSignInManager : SignInManager<ApplicationUser>
    {
        public ThrowingSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override Task SignInAsync(
            ApplicationUser user,
            bool isPersistent,
            string? authenticationMethod = null) =>
            throw new InvalidOperationException("Simulated session issuance failure.");
    }
}
