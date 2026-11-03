using System.Net;
using System.Text.RegularExpressions;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hms.Tests.Layout;

/// <summary>
/// Requirement 17: the shared layout, the collapsible sidebar, the phone drawer and the
/// "No records found." empty state. Each test boots the real app on its own throwaway SQLite
/// file in the temp folder and checks only the rows it writes.
/// </summary>
public class LayoutTests : IAsyncLifetime
{
    private readonly string _databasePath;
    private LayoutFactory _factory = null!;
    private HttpClient _client = null!;

    public LayoutTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-layout-{Guid.NewGuid():N}.db");
    }

    public async Task InitializeAsync()
    {
        _factory = new LayoutFactory(_databasePath);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // The app migrates and seeds on startup; add the admin account this test signs in with.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Admin, Roles.Doctor, Roles.Receptionist, Roles.Patient })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"layout-admin-{Guid.NewGuid():N}@hms.local";
        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            FullName = "Layout Admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var created = await users.CreateAsync(admin, "Layout@123");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(admin, Roles.Admin);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [Fact]
    public async Task Signed_in_page_renders_the_shared_layout_with_sidebar_topbar_breadcrumb_and_footer()
    {
        var html = await SignInAndGetAsync("/Dashboard/Admin");

        // 17.1: one layout with a collapsible sidebar, a top bar, a breadcrumb and a footer.
        Assert.Contains("id=\"hmsShell\"", html);
        Assert.Contains("id=\"hmsSidebar\"", html);
        Assert.Contains("id=\"hmsSidebarToggle\"", html);
        Assert.Contains("aria-label=\"Toggle sidebar\"", html);
        Assert.Contains("class=\"hms-topbar\"", html);
        Assert.Contains("class=\"hms-search\"", html);
        Assert.Contains("id=\"hmsBell\"", html);
        Assert.Contains("id=\"hmsProfileBtn\"", html);
        Assert.Contains("class=\"hms-breadcrumb\"", html);
        Assert.Contains("class=\"hms-footer\"", html);
        Assert.Contains("Greenfield General Hospital", html);
    }

    [Fact]
    public async Task Sidebar_toggle_collapses_to_icons_and_expands_again()
    {
        var html = await SignInAndGetAsync("/Dashboard/Admin");

        // 17.3: the toggle flips the hms-collapsed class, which hides the labels and narrows the
        // sidebar to the icon rail; the next click removes it again.
        Assert.Contains("shell.classList.toggle('hms-collapsed')", await ReadAssetAsync("/js/site.js"));
        Assert.Contains("hms-collapsed", await ReadAssetAsync("/css/site.css"));
        Assert.Contains("hms-sidebar-label", html);
    }

    [Fact]
    public async Task Phone_width_hides_the_sidebar_behind_a_menu_button()
    {
        var html = await SignInAndGetAsync("/Dashboard/Admin");
        var css = await ReadAssetAsync("/css/site.css");
        var js = await ReadAssetAsync("/js/site.js");

        // 17.8: at 375px the sidebar is a drawer behind the menu button, and the page never
        // scrolls sideways.
        Assert.Contains("id=\"hmsBackdrop\"", html);
        Assert.Contains("id=\"hmsSidebarClose\"", html);
        Assert.Contains("name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"", html);

        Assert.Contains("@media (max-width: 900px)", css);
        Assert.Contains("transform: translateX(-100%)", css);
        Assert.Contains(".hms-shell.hms-sidebar-open .hms-sidebar", css);
        Assert.Contains(".hms-sidebar[aria-hidden=\"true\"]", css);
        Assert.Contains("visibility: hidden", css);
        Assert.Contains("overflow-x: hidden", css);

        Assert.Contains("hms-sidebar-open", js);
        Assert.Contains("matchMedia('(max-width: 900px)')", js);
        Assert.Contains("sidebar.setAttribute('aria-hidden'", js);
        Assert.Contains("closeBtn.addEventListener('click'", js);
    }

    [Fact]
    public async Task Phone_width_turns_list_tables_into_labelled_cards()
    {
        var css = await ReadAssetAsync("/css/site.css");
        var js = await ReadAssetAsync("/js/site.js");

        // 17.8 and the design's phone list layout: rows become cards and each value receives
        // its desktop heading as a label instead of squeezing columns into letter-wide cells.
        Assert.Contains(".hms-table tbody tr", css);
        Assert.Contains("grid-template-columns: minmax(88px, 36%) minmax(0, 1fr)", css);
        Assert.Contains("content: attr(data-label)", css);
        Assert.Contains("word-break: normal", css);
        Assert.Contains("table.querySelectorAll('thead th')", js);
        Assert.Contains("cell.setAttribute('data-label'", js);
    }

    [Fact]
    public async Task Desktop_width_shows_the_sidebar_expanded_beside_the_content()
    {
        var css = await ReadAssetAsync("/css/site.css");

        // 17.9: the sidebar is 248px wide and sits beside the content; only the phone media
        // query takes it out of the flow.
        Assert.Contains("--hms-sidebar-width: 248px", css);
        Assert.Contains(".hms-shell {", css);
        Assert.Contains("display: flex;", css);
        Assert.Contains(".hms-main {", css);
        Assert.Contains("flex: 1 1 auto;", css);
    }

    [Fact]
    public async Task Empty_state_partial_renders_no_records_found()
    {
        // 17.7: the shared empty state partial renders "No records found." when a list has no rows.
        // It is rendered through the app's own Razor view engine, so no seeded list is needed.
        using var scope = _factory.Services.CreateScope();
        var viewEngine = scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();
        var tempData = scope.ServiceProvider.GetRequiredService<ITempDataProvider>();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        httpContext.Request.Path = "/Department/Index";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewResult = viewEngine.FindView(actionContext, "_EmptyState", isMainPage: false);
        Assert.True(viewResult.Success, "The _EmptyState partial should exist in Views/Shared.");

        await using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View!,
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            new TempDataDictionary(httpContext, tempData),
            writer,
            new HtmlHelperOptions());

        await viewResult.View!.RenderAsync(viewContext);

        var html = writer.ToString();
        Assert.Contains("No records found.", html);
        Assert.Contains("hms-empty-state", html);
    }

    [Theory]
    [InlineData("Views/Report/Departments.cshtml", "Model.Departments.Count == 0")]
    [InlineData("Views/Report/Doctors.cshtml", "Model.Doctors.Count == 0")]
    [InlineData("Views/Report/Pharmacy.cshtml", "Model.Medicines.Count == 0")]
    [InlineData("Views/Report/Laboratory.cshtml", "Model.Statuses.Count == 0")]
    [InlineData("Views/Report/Index.cshtml", "Model.Pharmacy.Medicines.Count == 0")]
    [InlineData("Views/Report/Index.cshtml", "Model.Laboratory.Statuses.Count == 0")]
    [InlineData("Views/Invoice/Details.cshtml", "Model.Payments.Count == 0")]
    [InlineData("Views/Invoice/Generate.cshtml", "Model.Lines.Count == 0")]
    [InlineData("Views/Invoice/Print.cshtml", "Model.Items.Count == 0")]
    public async Task Reports_render_the_shared_empty_state_instead_of_an_empty_table(string relativePath, string emptyCheck)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Hms.Web"));
        var view = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath));

        Assert.Contains(emptyCheck, view);
        Assert.Contains("<partial name=\"_EmptyState\"", view);
    }

    private async Task<string> SignInAndGetAsync(string path)
    {
        var loginPage = await _client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginHtml);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userName"] = await GetAdminEmailAsync(),
            ["password"] = "Layout@123",
            ["__RequestVerificationToken"] = token
        });

        var login = await _client.PostAsync("/Account/Login", form);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var page = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        return await page.Content.ReadAsStringAsync();
    }

    private async Task<string> GetAdminEmailAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = users.Users.First(u => u.FullName == "Layout Admin");
        return await Task.FromResult(admin.Email!);
    }

    private async Task<string> ReadAssetAsync(string path)
    {
        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(html, "<input[^>]*__RequestVerificationToken[^>]*>");
        Assert.True(match.Success, "The sign-in page should render an anti-forgery token.");
        var value = Regex.Match(match.Value, "value=\"([^\"]+)\"");
        Assert.True(value.Success, "The anti-forgery token input should carry a value.");
        return value.Groups[1].Value;
    }

    private sealed class LayoutFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath;

        public LayoutFactory(string databasePath)
        {
            _databasePath = databasePath;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        }
    }
}
