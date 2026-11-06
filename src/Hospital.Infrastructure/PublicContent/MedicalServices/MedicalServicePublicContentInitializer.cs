using System.Data;
using System.Data.Common;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.PublicContent.MedicalServices;

public static class MedicalServicePublicContentInitializer
{
    public static async Task InitializeAsync(
        HospitalDbContext database,
        bool seedDevelopmentContent,
        CancellationToken cancellationToken = default)
    {
        var connection = database.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = SchemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);

        if (!seedDevelopmentContent || await HasMedicalServicesAsync(connection, cancellationToken))
        {
            return;
        }

        command.CommandText = DevelopmentSeedSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> HasMedicalServicesAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM MedicalServices LIMIT 1);";
        return await command.ExecuteScalarAsync(cancellationToken) is long value && value == 1;
    }

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS MedicalServices (
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
        CREATE INDEX IF NOT EXISTS IX_MedicalServices_PublicationState_Slug
            ON MedicalServices (PublicationState, Slug);
        """;

    private const string DevelopmentSeedSql = """
        INSERT INTO MedicalServices
            (Slug, Name, Description, ImagePath, Details, CareInformation, PublicationState, IsAvailable, PublishedAt)
        VALUES
            ('advanced-diagnostics', 'Advanced Diagnostics',
             'Clear answers through modern imaging and laboratory expertise.', NULL,
             'Our diagnostic teams bring imaging, pathology, and specialist interpretation together to help patients and clinicians make informed care decisions.',
             'Your care team will explain preparation, what to expect during the test, and how results are shared.',
             'Published', 1, CURRENT_TIMESTAMP),
            ('preventive-care', 'Preventive Care',
             'Personalized screening and wellness support for every stage of life.', NULL,
             'Preventive care combines health assessment, evidence-based screening, and practical guidance tailored to your needs and health goals.',
             'Bring a current medicines list and any recent reports so your team can recommend the most relevant next steps.',
             'Published', 1, CURRENT_TIMESTAMP),
            ('rehabilitation', 'Rehabilitation',
             'Coordinated therapy that helps you regain strength, movement, and confidence.', NULL,
             'Our rehabilitation program brings together physiotherapy and individualized recovery planning after illness, injury, or surgery.',
             'Wear comfortable clothing and bring any mobility aid you currently use to your first assessment.',
             'Published', 1, CURRENT_TIMESTAMP);
        """;
}
