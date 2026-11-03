using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Audit;

public class AuditListViewModel
{
    public IReadOnlyList<AuditEntry> Entries { get; init; } = Array.Empty<AuditEntry>();

    public string? RecordType { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalCount { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
