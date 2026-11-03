using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Staff;

/// <summary>
/// The staff list page: the rows of the current page plus the toolbar state
/// (search text, sort order, status filter and page number) so the view can keep them.
/// </summary>
public class StaffListViewModel
{
    public IReadOnlyList<StaffRow> Rows { get; set; } = new List<StaffRow>();

    public string? Search { get; set; }

    public string Sort { get; set; } = "name";

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>One staff row as the list shows it.</summary>
public class StaffRow
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Designation { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }

    public string Status { get; set; } = EntityStatus.Active;
}
