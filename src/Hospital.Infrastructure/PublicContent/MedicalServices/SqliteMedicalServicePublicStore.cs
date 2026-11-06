using System.Data;
using System.Data.Common;
using Hospital.Application.Features.MedicalServices;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.PublicContent.MedicalServices;

public sealed class SqliteMedicalServicePublicStore : IMedicalServicePublicStore
{
    private readonly HospitalDbContext database;

    public SqliteMedicalServicePublicStore(HospitalDbContext database)
    {
        this.database = database;
    }

    public async Task<IReadOnlyList<MedicalServiceSummary>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = PublishedSelect + " ORDER BY Name COLLATE NOCASE;";
        var items = new List<MedicalServiceSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MedicalServiceSummary(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), ReadNullableString(reader, 3)));
        }

        return items;
    }

    public async Task<MedicalServiceDetails?> FindPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = PublishedSelect + " AND Slug = @slug LIMIT 1;";
        AddParameter(command, "@slug", slug);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MedicalServiceDetails(
            reader.GetString(0), reader.GetString(1), reader.GetString(2), ReadNullableString(reader, 3),
            reader.GetString(4), ReadNullableString(reader, 5));
    }

    private const string PublishedSelect = """
        SELECT Slug, Name, Description, ImagePath, Details, CareInformation
        FROM MedicalServices
        WHERE PublicationState = 'Published' AND IsAvailable = 1 AND ArchivedAt IS NULL
        """;

    private async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = database.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private static string? ReadNullableString(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

