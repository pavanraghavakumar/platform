using System.Net;
using System.Text.RegularExpressions;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hms.Tests.Account;

public class ProfileTests : IClassFixture<ProfileTests.HmsAppFactory>
{
    private readonly HmsAppFactory _factory;

    public ProfileTests(HmsAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Profile_shows_user_name_email_role_and_phone_and_saves_a_new_phone()
    {
        var account = await CreateAccountAsync("profile", "Patient", "Profile@123", "+91 90000 11111");
        using var client = await SignInAsync(account.Email, account.Password);

        var page = await client.GetAsync("/Account/Profile");
        var html = WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains(account.Email, html);
        Assert.Contains("Patient", html);
        Assert.Contains("+91 90000 11111", html);

        var response = await PostFormAsync(client, "/Account/Profile", ExtractAntiforgeryToken(html), new()
        {
            ["PhoneNumber"] = "+91 98888 22222"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var saved = await users.FindByEmailAsync(account.Email);
        Assert.Equal("+91 98888 22222", saved?.PhoneNumber);
    }

    [Fact]
    public async Task Short_password_keeps_old_password_and_shows_error_beside_new_password_with_values()
    {
        var account = await CreateAccountAsync("short-password", "Receptionist", "Original@123");
        using var client = await SignInAsync(account.Email, account.Password);
        var pageHtml = await (await client.GetAsync("/Account/ChangePassword")).Content.ReadAsStringAsync();

        var response = await PostFormAsync(client, "/Account/ChangePassword", ExtractAntiforgeryToken(pageHtml), new()
        {
            ["CurrentPassword"] = account.Password,
            ["NewPassword"] = "short"
        });
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Password must be at least 8 characters.", html);
        Assert.Matches("<span[^>]*data-valmsg-for=\"NewPassword\"[^>]*>[^<]*Password must be at least 8 characters.", html);
        Assert.Contains($"value=\"{account.Password}\"", html);
        Assert.Contains("value=\"short\"", html);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(account.Email);
        Assert.True(await users.CheckPasswordAsync(user!, account.Password));
    }

    [Fact]
    public async Task Valid_password_change_saves_password_and_shows_success_message()
    {
        var account = await CreateAccountAsync("change-password", "Doctor", "Original@123");
        const string newPassword = "Changed@456";
        using var client = await SignInAsync(account.Email, account.Password);
        var pageHtml = await (await client.GetAsync("/Account/ChangePassword")).Content.ReadAsStringAsync();

        var response = await PostFormAsync(client, "/Account/ChangePassword", ExtractAntiforgeryToken(pageHtml), new()
        {
            ["CurrentPassword"] = account.Password,
            ["NewPassword"] = newPassword
        });
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Your password has been changed.", html);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(account.Email);
        Assert.True(await users.CheckPasswordAsync(user!, newPassword));
        Assert.False(await users.CheckPasswordAsync(user!, account.Password));
    }

    private async Task<(string Email, string Password)> CreateAccountAsync(
        string prefix,
        string role,
        string password,
        string? phone = null)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@hms.local";
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Profile Test Person",
            PhoneNumber = phone
        };
        var created = await users.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        var roleResult = await users.AddToRoleAsync(user, role);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        return (email, password);
    }

    private async Task<HttpClient> SignInAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var page = await client.GetAsync("/Account/Login");
        var token = ExtractAntiforgeryToken(await page.Content.ReadAsStringAsync());
        var response = await PostFormAsync(client, "/Account/Login", token, new()
        {
            ["UserName"] = email,
            ["Password"] = password
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var input = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>");
        var value = Regex.Match(input.Value, "value=\"([^\"]+)\"");
        Assert.True(value.Success, "The page should carry an antiforgery token.");
        return WebUtility.HtmlDecode(value.Groups[1].Value);
    }

    private static Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string path,
        string token,
        Dictionary<string, string> fields)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token)
        };
        form.AddRange(fields);
        return client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    public sealed class HmsAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath =
            Path.Combine(Path.GetTempPath(), $"hms-profile-tests-{Guid.NewGuid():N}.db");

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
