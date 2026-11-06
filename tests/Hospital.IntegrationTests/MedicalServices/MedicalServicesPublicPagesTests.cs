using System.Data.Common;
using System.Net;
using Hospital.Infrastructure.Data;
using Hospital.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;

namespace Hospital.IntegrationTests.MedicalServices;

public sealed class MedicalServicesPublicPagesTests : IDisposable
{
    private readonly HospitalWebApplicationFactory factory = new();

    [Fact]
    public async Task MedicalServicesIndexShowsPublishedCardsAndHealthcareImageryOnly()
    {
        using var client = factory.CreateClient();
        await client.GetAsync("/medical-services");
        await ExecuteAsync("""
            INSERT INTO MedicalServices
                (Slug, Name, Description, ImagePath, Details, CareInformation, PublicationState, IsAvailable, PublishedAt)
            VALUES
                ('published-service', 'Published Service', 'Visible public description', '/media/published.webp', 'Visible details', 'Visible care', 'Published', 1, CURRENT_TIMESTAMP),
                ('draft-service', 'Draft Service', 'Hidden draft description', '/media/draft.webp', 'Hidden details', 'Hidden care', 'Draft', 1, NULL),
                ('archived-service', 'Archived Service', 'Hidden archived description', '/media/archive.webp', 'Hidden details', 'Hidden care', 'Published', 1, CURRENT_TIMESTAMP);
            UPDATE MedicalServices SET ArchivedAt = CURRENT_TIMESTAMP WHERE Slug = 'archived-service';
            """);

        var response = await client.GetAsync("/medical-services");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Published Service", html);
        Assert.Contains("Visible public description", html);
        Assert.Contains("/media/published.webp", html);
        Assert.Contains("srcset=\"/media/responsive/480.webp?source=%2Fmedia%2Fpublished.webp 480w, /media/responsive/768.webp?source=%2Fmedia%2Fpublished.webp 768w, /media/responsive/1200.webp?source=%2Fmedia%2Fpublished.webp 1200w\"", html);
        Assert.Contains("sizes=\"(max-width: 767px)", html);
        Assert.Contains("width=\"960\"", html);
        Assert.Contains("height=\"640\"", html);
        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("href=\"/medical-services/published-service\"", html);
        AssertStyleSheetIsInHead(html);
        Assert.DoesNotContain("Draft Service", html);
        Assert.DoesNotContain("Archived Service", html);
    }

    [Fact]
    public async Task MedicalServiceDetailsShowsEditorialCareAndAppointmentRoute()
    {
        using var client = factory.CreateClient();
        await client.GetAsync("/medical-services");
        await ExecuteAsync("""
            INSERT INTO MedicalServices
                (Slug, Name, Description, ImagePath, Details, CareInformation, PublicationState, IsAvailable, PublishedAt)
            VALUES
                ('precision-care', 'Precision Care', 'Individual care description', '/media/precision-care.webp', 'Editorial service presentation', 'Bring your referral and current medicines.', 'Published', 1, CURRENT_TIMESTAMP);
            """);

        var response = await client.GetAsync("/medical-services/precision-care");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Precision Care", html);
        Assert.Contains("Editorial service presentation", html);
        Assert.Contains("Bring your referral and current medicines.", html);
        Assert.Contains("Book an Appointment", html);
        Assert.Contains("/appointments/book?service=precision-care", html);
        Assert.Contains("Healthcare professionals supporting Precision Care", html);
        Assert.Contains("srcset=\"/media/responsive/480.webp?source=%2Fmedia%2Fprecision-care.webp 480w, /media/responsive/768.webp?source=%2Fmedia%2Fprecision-care.webp 768w, /media/responsive/1200.webp?source=%2Fmedia%2Fprecision-care.webp 1200w\"", html);
        Assert.Contains("sizes=\"(max-width: 767px)", html);
        Assert.Contains("loading=\"eager\"", html);
        Assert.Contains("fetchpriority=\"high\"", html);
        AssertStyleSheetIsInHead(html);
    }

    [Fact]
    public async Task ResponsiveImageCandidatesReturnDistinctResizedResources()
    {
        var mediaDirectory = Path.Combine(factory.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath, "media");
        var sourcePath = Path.Combine(mediaDirectory, $"responsive-source-{Guid.NewGuid():N}.png");
        Directory.CreateDirectory(mediaDirectory);

        try
        {
            using (var sourceImage = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(1400, 700))
            {
                await sourceImage.SaveAsPngAsync(sourcePath);
            }

            using var client = factory.CreateClient();
            var source = $"/media/{Path.GetFileName(sourcePath)}";
            var phoneResponse = await client.GetAsync($"/media/responsive/480.webp?source={Uri.EscapeDataString(source)}");
            var desktopResponse = await client.GetAsync($"/media/responsive/1200.webp?source={Uri.EscapeDataString(source)}");

            Assert.Equal(HttpStatusCode.OK, phoneResponse.StatusCode);
            Assert.Equal("image/webp", phoneResponse.Content.Headers.ContentType?.MediaType);
            Assert.Equal(HttpStatusCode.OK, desktopResponse.StatusCode);

            using var phoneImage = await Image.LoadAsync(await phoneResponse.Content.ReadAsStreamAsync());
            using var desktopImage = await Image.LoadAsync(await desktopResponse.Content.ReadAsStreamAsync());
            Assert.Equal((480, 240), (phoneImage.Width, phoneImage.Height));
            Assert.Equal((1200, 600), (desktopImage.Width, desktopImage.Height));
            Assert.NotEqual(
                await phoneResponse.Content.ReadAsByteArrayAsync(),
                await desktopResponse.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            DeleteIfPresent(sourcePath);
        }
    }

    [Theory]
    [InlineData("Draft", 1, false)]
    [InlineData("Published", 0, false)]
    [InlineData("Published", 1, true)]
    public async Task UnavailableMedicalServiceUsesBrandedNotFound(string state, int isAvailable, bool archived)
    {
        using var client = factory.CreateClient();
        await client.GetAsync("/medical-services");
        var slug = $"unavailable-{state.ToLowerInvariant()}-{isAvailable}-{archived.ToString().ToLowerInvariant()}";
        await ExecuteAsync("""
            INSERT INTO MedicalServices
                (Slug, Name, Description, Details, PublicationState, IsAvailable, PublishedAt, ArchivedAt)
            VALUES (@slug, 'Private Service', 'Must not be public', 'Private details', @state, @available, CURRENT_TIMESTAMP, @archivedAt);
            """,
            ("@slug", slug), ("@state", state), ("@available", isAvailable),
            ("@archivedAt", archived ? DateTimeOffset.UtcNow.ToString("O") : DBNull.Value));

        var response = await client.GetAsync($"/medical-services/{slug}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("This service is not available", html);
        Assert.Contains("Back to Medical Services", html);
        Assert.Contains("href=\"/medical-services\"", html);
        Assert.DoesNotContain("Private details", html);
        AssertStyleSheetIsInHead(html);
    }

    [Fact]
    public async Task PublishedUpdatesArchivesAndRestoresFlowThroughToPublicPages()
    {
        using var client = factory.CreateClient();
        await client.GetAsync("/medical-services");
        await ExecuteAsync("""
            INSERT INTO MedicalServices
                (Slug, Name, Description, Details, CareInformation, PublicationState, IsAvailable, PublishedAt)
            VALUES ('changing-service', 'Original Name', 'Original description', 'Original details', 'Original care', 'Published', 1, CURRENT_TIMESTAMP);
            """);

        Assert.Contains("Original Name", await client.GetStringAsync("/medical-services/changing-service"));

        await ExecuteAsync("""
            UPDATE MedicalServices
            SET Name = 'Updated Name', Description = 'Updated description', Details = 'Updated public details'
            WHERE Slug = 'changing-service';
            """);
        var updatedHtml = await client.GetStringAsync("/medical-services/changing-service");
        Assert.Contains("Updated Name", updatedHtml);
        Assert.Contains("Updated public details", updatedHtml);

        await ExecuteAsync("UPDATE MedicalServices SET ArchivedAt = CURRENT_TIMESTAMP WHERE Slug = 'changing-service';");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/medical-services/changing-service")).StatusCode);

        await ExecuteAsync("UPDATE MedicalServices SET ArchivedAt = NULL WHERE Slug = 'changing-service';");
        var restored = await client.GetAsync("/medical-services/changing-service");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Contains("Updated Name", await restored.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExistingDatabaseWithManagedSeedSlugStartsWithoutMutationOrDuplicateFailure()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"hospital-medical-services-existing-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<HospitalDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using (var existingDatabase = new HospitalDbContext(options))
            {
                await existingDatabase.Database.EnsureCreatedAsync();
                await existingDatabase.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE MedicalServices (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Slug TEXT NOT NULL UNIQUE,
                        Name TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        ImagePath TEXT NULL,
                        Details TEXT NOT NULL,
                        CareInformation TEXT NULL,
                        PublicationState TEXT NOT NULL,
                        IsAvailable INTEGER NOT NULL DEFAULT 1,
                        PublishedAt TEXT NULL,
                        ArchivedAt TEXT NULL
                    );
                    INSERT INTO MedicalServices
                        (Slug, Name, Description, Details, CareInformation, PublicationState, IsAvailable, PublishedAt)
                    VALUES
                        ('advanced-diagnostics', 'Administrator Managed Diagnostics', 'Existing description',
                         'Existing managed details', 'Existing care information', 'Published', 1, CURRENT_TIMESTAMP);
                    """);
            }

            using var existingFactory = new ExistingMedicalServicesDatabaseFactory(databasePath);
            using var client = existingFactory.CreateClient();

            var response = await client.GetAsync("/medical-services/advanced-diagnostics");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Administrator Managed Diagnostics", html);
            Assert.Contains("Existing managed details", html);

            await using var verification = new SqliteConnection($"Data Source={databasePath}");
            await verification.OpenAsync();
            await using var count = verification.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM MedicalServices;";
            Assert.Equal(1L, Assert.IsType<long>(await count.ExecuteScalarAsync()));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfPresent(databasePath);
            DeleteIfPresent(databasePath + "-shm");
            DeleteIfPresent(databasePath + "-wal");
        }
    }

    public void Dispose() => factory.Dispose();

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var connection = database.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var item in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = item.Name;
            parameter.Value = item.Value;
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void AssertStyleSheetIsInHead(string html)
    {
        var stylePosition = html.IndexOf("/css/pages/medical-services.css", StringComparison.Ordinal);
        var headEndPosition = html.IndexOf("</head>", StringComparison.Ordinal);

        Assert.True(stylePosition >= 0, "The medical-services stylesheet should be rendered.");
        Assert.True(stylePosition < headEndPosition, "The medical-services stylesheet should be rendered inside the document head.");
    }

    private sealed class ExistingMedicalServicesDatabaseFactory : WebApplicationFactory<Program>
    {
        private readonly string databasePath;

        public ExistingMedicalServicesDatabaseFactory(string databasePath)
        {
            this.databasePath = databasePath;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:HospitalDatabase", $"Data Source={databasePath}");
        }
    }
}
