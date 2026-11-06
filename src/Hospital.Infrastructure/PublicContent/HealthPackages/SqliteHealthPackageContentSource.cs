using System.Text.Json;
using Hospital.Application.Features.HealthPackages;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.PublicContent.HealthPackages;

public sealed class SqliteHealthPackageContentSource : IHealthPackageContentSource
{
    private readonly HospitalDbContext database;

    public SqliteHealthPackageContentSource(HospitalDbContext database)
    {
        this.database = database;
    }

    public async Task<IReadOnlyList<HealthPackageContent>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await database.HealthPackages
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        return records.Select(ToContent).ToArray();
    }

    private static HealthPackageContent ToContent(HealthPackageRecord record) =>
        new(
            record.Slug,
            record.Name,
            record.Description,
            JsonSerializer.Deserialize<string[]>(record.Details) ?? Array.Empty<string>(),
            record.PublicationState);

}
