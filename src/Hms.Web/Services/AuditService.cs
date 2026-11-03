using Hms.Web.Data;
using Hms.Web.Models.Entities;

namespace Hms.Web.Services;

public interface IAuditService
{
    Task RecordAsync(string recordType, int recordId, string field, string? oldValue, string? newValue, string userName);
}

/// <summary>
/// Writes audit entries for changes to medical and financial records.
/// Opening a record without changing it writes nothing.
/// </summary>
public class AuditService : IAuditService
{
    private readonly HmsDbContext _db;

    public AuditService(HmsDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(string recordType, int recordId, string field, string? oldValue, string? newValue, string userName)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return;
        }

        _db.AuditEntries.Add(new AuditEntry
        {
            RecordType = recordType,
            RecordId = recordId,
            FieldName = field,
            OldValue = oldValue,
            NewValue = newValue,
            UserName = userName,
            ChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
