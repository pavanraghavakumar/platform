using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hms.Tests.Notification;

/// <summary>
/// Requirement 15: the notification bell and panel. The service tests run on a throwaway
/// SQLite file in the temp folder; the integration tests boot the real app and sign in.
/// </summary>
public class NotificationTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public NotificationTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-notification-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<HmsDbContext>(options => options.UseSqlite(_connection));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<HmsDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<INotificationService, NotificationService>();

        _services = services.BuildServiceProvider();
    }

    // ---------- Service ----------

    [Fact]
    public async Task AddAsync_saves_an_unread_notification_for_that_user_only()
    {
        var (db, service, userId, otherUserId) = await CreateServiceAsync();

        await service.AddAsync(userId, "Your appointment is confirmed.");
        await service.AddAsync(otherUserId, "Someone else's notification.");

        var mine = await db.Notifications.Where(n => n.UserId == userId).ToListAsync();
        var single = Assert.Single(mine);
        Assert.Equal("Your appointment is confirmed.", single.Message);
        Assert.False(single.IsRead);

        Assert.Equal(1, await service.GetUnreadCountAsync(userId));
        Assert.Equal(1, await service.GetUnreadCountAsync(otherUserId));
    }

    [Fact]
    public async Task GetRecentAsync_returns_the_ten_newest_first()
    {
        var (_, service, userId, _) = await CreateServiceAsync();

        for (var i = 1; i <= 12; i++)
        {
            await service.AddAsync(userId, $"Notification {i}");
        }

        var recent = await service.GetRecentAsync(userId, NotificationService.RecentTake);

        Assert.Equal(10, recent.Count);
        Assert.Equal("Notification 12", recent[0].Message);
        Assert.Equal("Notification 3", recent[9].Message);
        Assert.DoesNotContain(recent, n => n.Message == "Notification 1" || n.Message == "Notification 2");
    }

    [Fact]
    public async Task MarkAllReadAsync_sets_the_unread_count_to_zero()
    {
        var (db, service, userId, _) = await CreateServiceAsync();

        await service.AddAsync(userId, "One");
        await service.AddAsync(userId, "Two");
        Assert.Equal(2, await service.GetUnreadCountAsync(userId));

        await service.MarkAllReadAsync(userId);

        Assert.Equal(0, await service.GetUnreadCountAsync(userId));
        var rows = await db.Notifications.Where(n => n.UserId == userId).ToListAsync();
        Assert.All(rows, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task GetRecentAsync_returns_nothing_for_a_user_with_no_notifications()
    {
        var (_, service, userId, _) = await CreateServiceAsync();

        var recent = await service.GetRecentAsync(userId);

        Assert.Empty(recent);
        Assert.Equal(0, await service.GetUnreadCountAsync(userId));
    }

    // ---------- Bell endpoints ----------

    [Fact]
    public async Task UnreadCount_answers_the_json_count_for_the_signed_in_user()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(client, DemoDataSeeder.PatientEmail, DemoDataSeeder.PatientPassword);

        var userId = await GetUserIdAsync(factory, DemoDataSeeder.PatientEmail);
        await AddNotificationsAsync(factory, userId, 3);

        var response = await client.GetAsync("/Notification/UnreadCount");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, json.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Panel_lists_the_notifications_newest_first_and_marks_them_read()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(client, DemoDataSeeder.PatientEmail, DemoDataSeeder.PatientPassword);

        var userId = await GetUserIdAsync(factory, DemoDataSeeder.PatientEmail);
        await AddNotificationsAsync(factory, userId, 2);

        var panel = await client.GetAsync("/Notification/Panel");
        Assert.Equal(HttpStatusCode.OK, panel.StatusCode);
        var html = await panel.Content.ReadAsStringAsync();
        Assert.Contains("Notification 2", html);
        Assert.Contains("Notification 1", html);
        Assert.True(html.IndexOf("Notification 2", StringComparison.Ordinal) <
                    html.IndexOf("Notification 1", StringComparison.Ordinal),
            "The newest notification should be listed first.");
        Assert.DoesNotContain("You have no notifications.", html);

        var count = await client.GetAsync("/Notification/UnreadCount");
        using var json = JsonDocument.Parse(await count.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Panel_shows_the_empty_message_when_the_user_has_no_notifications()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(client, DemoDataSeeder.ReceptionistEmail, DemoDataSeeder.ReceptionistPassword);

        var panel = await client.GetAsync("/Notification/Panel");

        Assert.Equal(HttpStatusCode.OK, panel.StatusCode);
        Assert.Contains("You have no notifications.", await panel.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Bell_endpoints_require_a_signed_in_user()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var count = await client.GetAsync("/Notification/UnreadCount");
        var panel = await client.GetAsync("/Notification/Panel");

        Assert.Equal(HttpStatusCode.Redirect, count.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, panel.StatusCode);
        Assert.Contains("/Account/Login", count.Headers.Location?.OriginalString ?? string.Empty);
    }

    // ---------- Helpers ----------

    private async Task<(HmsDbContext Db, INotificationService Service, string UserId, string OtherUserId)> CreateServiceAsync()
    {
        var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.EnsureRolesAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userId = await CreateUserAsync(users, "notify.owner@hms.test");
        var otherUserId = await CreateUserAsync(users, "notify.other@hms.test");

        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        return (db, service, userId, otherUserId);
    }

    private static async Task<string> CreateUserAsync(UserManager<ApplicationUser> users, string email)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Notification Test User",
            IsActive = true
        };
        var result = await users.CreateAsync(user, "Test@123");
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, Roles.Patient);
        return user.Id;
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
            builder.UseEnvironment(Environments.Development);
        });
    }

    private static async Task SignInAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var token = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Account/Login", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetUserIdAsync(WebApplicationFactory<Program> factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        return user!.Id;
    }

    private static async Task AddNotificationsAsync(WebApplicationFactory<Program> factory, string userId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        for (var i = 1; i <= count; i++)
        {
            await service.AddAsync(userId, $"Notification {i}");
        }
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>");
        if (!match.Success)
        {
            match = Regex.Match(html, "<input[^>]*__RequestVerificationToken[^>]*>");
        }
        Assert.True(match.Success, "The login page should render an anti-forgery token.");
        var value = Regex.Match(match.Value, "value=\"([^\"]+)\"");
        Assert.True(value.Success, "The anti-forgery input should carry a value.");
        return value.Groups[1].Value;
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Hms.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
