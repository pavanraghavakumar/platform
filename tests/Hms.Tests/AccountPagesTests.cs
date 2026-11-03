using System.Net;
using System.Text.RegularExpressions;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hms.Tests;

/// <summary>
/// Sign-in, forgot and reset password pages: boots the real app against a throwaway SQLite file
/// in a temporary folder and checks the rendered pages, the role redirects and the reset flow.
/// </summary>
public class AccountPagesTests : IClassFixture<AccountPagesTests.HmsAppFactory>
{
    private readonly HmsAppFactory _factory;

    public AccountPagesTests(HmsAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Root_without_signing_in_shows_the_sign_in_page()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign in", html);
        Assert.Contains("name=\"UserName\"", html);
        Assert.Contains("name=\"Password\"", html);
        Assert.Contains("Forgot password", html);
        Assert.Contains(SystemSetting.DefaultHospitalName, html);
    }

    [Fact]
    public async Task Login_page_is_served_at_account_login()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("hms-auth-card", html);
        Assert.Contains("hms-auth-logo", html);
    }

    [Theory]
    [InlineData(DemoDataSeeder.AdminEmail, DemoDataSeeder.AdminPassword, "/Dashboard/Admin")]
    [InlineData(DemoDataSeeder.DoctorEmail, DemoDataSeeder.DoctorPassword, "/Dashboard/Doctor")]
    [InlineData(DemoDataSeeder.ReceptionistEmail, DemoDataSeeder.ReceptionistPassword, "/Dashboard/Receptionist")]
    [InlineData(DemoDataSeeder.PatientEmail, DemoDataSeeder.PatientPassword, "/Dashboard/Patient")]
    public async Task Signing_in_opens_the_dashboard_of_the_persons_role(string email, string password, string expectedPath)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await PostFormAsync(client, "/Account/Login", token, new()
        {
            ["UserName"] = email,
            ["Password"] = password
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedPath, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Wrong_password_keeps_the_sign_in_page_and_shows_the_message()
    {
        using var client = _factory.CreateClient();

        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await PostFormAsync(client, "/Account/Login", token, new()
        {
            ["UserName"] = DemoDataSeeder.AdminEmail,
            ["Password"] = "WrongPassword1"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid user name or password.", html);
        Assert.Contains("name=\"Password\"", html);
    }

    [Fact]
    public async Task Unknown_user_name_shows_the_invalid_message()
    {
        using var client = _factory.CreateClient();

        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await PostFormAsync(client, "/Account/Login", token, new()
        {
            ["UserName"] = "nobody@hms.local",
            ["Password"] = "Whatever@123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid user name or password.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Deactivated_account_is_blocked_with_its_message()
    {
        var email = $"deactivated-{Guid.NewGuid():N}@hms.local";
        const string password = "Deactivated@123";

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Deactivated Person",
                IsActive = false
            };
            var created = await users.CreateAsync(user, password);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, Roles.Receptionist);
        }

        using var client = _factory.CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await PostFormAsync(client, "/Account/Login", token, new()
        {
            ["UserName"] = email,
            ["Password"] = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Your account is deactivated.", html);
        Assert.DoesNotContain("Invalid user name or password.", html);
    }

    [Fact]
    public async Task Forgot_password_shows_the_reset_link_on_the_page()
    {
        using var client = _factory.CreateClient();

        var token = await GetAntiforgeryTokenAsync(client, "/Account/ForgotPassword");
        var response = await PostFormAsync(client, "/Account/ForgotPassword", token, new()
        {
            ["Email"] = DemoDataSeeder.AdminEmail
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("id=\"hmsResetLink\"", html);
        Assert.Contains("/Account/ResetPassword", html);
        Assert.Contains("token=", html);
    }

    [Fact]
    public async Task The_reset_link_opens_a_page_that_sets_a_new_password()
    {
        var email = $"reset-{Guid.NewGuid():N}@hms.local";
        const string oldPassword = "Original@123";
        const string newPassword = "Changed@456";

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Reset Person"
            };
            var created = await users.CreateAsync(user, oldPassword);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, Roles.Patient);
        }

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 1. Ask for the reset link on the forgot-password page.
        var forgotToken = await GetAntiforgeryTokenAsync(client, "/Account/ForgotPassword");
        var forgotResponse = await PostFormAsync(client, "/Account/ForgotPassword", forgotToken, new()
        {
            ["Email"] = email
        });
        var forgotHtml = await forgotResponse.Content.ReadAsStringAsync();

        Assert.True(
            forgotResponse.IsSuccessStatusCode,
            $"The forgot-password POST should succeed, got {(int)forgotResponse.StatusCode}.");

        var linkMatch = Regex.Match(forgotHtml, "<a[^>]*id=\"hmsResetLinkAnchor\"[^>]*>");
        Assert.True(linkMatch.Success, "The forgot-password page should show a reset link.");

        var hrefMatch = Regex.Match(linkMatch.Value, "href=\"([^\"]+)\"");
        Assert.True(hrefMatch.Success, "The reset link should carry an href.");

        // 2. Open the link: it renders the reset-password page.
        // The link is absolute (http://localhost/...); the test server only answers relative paths.
        var link = WebUtility.HtmlDecode(hrefMatch.Groups[1].Value);
        var linkPath = new Uri(link, UriKind.RelativeOrAbsolute).PathAndQuery;
        var resetPage = await client.GetAsync(linkPath);
        Assert.Equal(HttpStatusCode.OK, resetPage.StatusCode);
        var resetHtml = await resetPage.Content.ReadAsStringAsync();
        Assert.Contains("name=\"Password\"", resetHtml);
        Assert.Contains("name=\"ConfirmPassword\"", resetHtml);

        // 3. Submit the new password from that page.
        var resetToken = await GetAntiforgeryTokenAsync(client, linkPath);
        var resetResponse = await PostFormAsync(client, linkPath, resetToken, new()
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["ConfirmPassword"] = newPassword
        });

        Assert.Equal(HttpStatusCode.Redirect, resetResponse.StatusCode);
        Assert.Equal("/Account/Login", resetResponse.Headers.Location?.OriginalString);

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True(await users.CheckPasswordAsync(user!, newPassword), "The new password should work.");
            Assert.False(await users.CheckPasswordAsync(user!, oldPassword), "The old password should no longer work.");
        }
    }

    [Fact]
    public async Task Signing_out_ends_the_session_and_shows_the_sign_in_page()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginToken = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var loginResponse = await PostFormAsync(client, "/Account/Login", loginToken, new()
        {
            ["UserName"] = DemoDataSeeder.AdminEmail,
            ["Password"] = DemoDataSeeder.AdminPassword
        });
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        // The signed-in dashboard page carries the sign-out form and its token.
        var dashboard = await client.GetAsync("/Dashboard/Admin");
        var dashboardHtml = await dashboard.Content.ReadAsStringAsync();
        var logoutToken = ExtractAntiforgeryToken(dashboardHtml);

        var logoutResponse = await PostFormAsync(client, "/Account/Logout", logoutToken, new());
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Equal("/", logoutResponse.Headers.Location?.OriginalString);

        // Following the redirect lands on the sign-in page.
        var afterLogout = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, afterLogout.StatusCode);
        Assert.Equal("/Account/Login", afterLogout.Headers.Location?.OriginalString);

        var signInPage = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, signInPage.StatusCode);
        Assert.Contains("name=\"Password\"", await signInPage.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Access_denied_page_shows_its_message()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/AccessDenied");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("You do not have access to this page.", await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return ExtractAntiforgeryToken(await response.Content.ReadAsStringAsync());
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The page should carry an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string path,
        string antiforgeryToken,
        Dictionary<string, string> fields)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", antiforgeryToken)
        };
        form.AddRange(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));

        using var content = new FormUrlEncodedContent(form);
        return await client.PostAsync(path, content);
    }

    /// <summary>
    /// Boots the app with a SQLite file in a temporary folder, migrated and seeded once for the class.
    /// </summary>
    public sealed class HmsAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath =
            Path.Combine(Path.GetTempPath(), $"hms-account-pages-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
        }
    }
}
