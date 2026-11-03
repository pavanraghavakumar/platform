using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Lab;

/// <summary>The lab request list with its status filter (Requirement 8.3).</summary>
public class LabRequestListViewModel
{
    public IReadOnlyList<LabRequest> Requests { get; set; } = Array.Empty<LabRequest>();

    /// <summary>The status the list is filtered by, or <c>null</c> for every status.</summary>
    public string? Status { get; set; }

    public static readonly string[] Statuses =
    {
        EntityStatus.LabRequested,
        EntityStatus.LabInProgress,
        EntityStatus.LabCompleted,
        EntityStatus.LabCancelled
    };
}
