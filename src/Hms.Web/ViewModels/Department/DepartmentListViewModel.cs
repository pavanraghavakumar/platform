using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Department;

/// <summary>
/// The department list page: the rows of the current page plus the toolbar state
/// (search text, sort order, status filter and page number) so the view can keep them.
/// </summary>
public class DepartmentListViewModel
{
    public IReadOnlyList<DepartmentRow> Rows { get; set; } = new List<DepartmentRow>();

    public string? Search { get; set; }

    public string Sort { get; set; } = "name";

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>One department row with the number of doctors and staff it holds.</summary>
public class DepartmentRow
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = EntityStatus.Active;

    public int DoctorCount { get; set; }

    public int StaffCount { get; set; }
}
