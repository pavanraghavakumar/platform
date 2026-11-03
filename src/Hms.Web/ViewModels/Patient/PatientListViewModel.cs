using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Patient;

/// <summary>
/// The patient list page: one page of patients plus the toolbar state (search, sort, status filter)
/// and the paging figures the pagination partial needs.
/// </summary>
public class PatientListViewModel
{
    public const int PageSize = 10;

    public IReadOnlyList<Models.Entities.Patient> Patients { get; set; } = Array.Empty<Models.Entities.Patient>();

    public string? Search { get; set; }

    public string Sort { get; set; } = PatientSort.Newest;

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int TotalCount { get; set; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}

/// <summary>The sort options the patient list offers, as the sort control names them.</summary>
public static class PatientSort
{
    public const string Newest = "newest";
    public const string Oldest = "oldest";
    public const string NameAsc = "name";
    public const string NameDesc = "name-desc";

    public static readonly (string Value, string Label)[] Options =
    {
        (Newest, "Newest first"),
        (Oldest, "Oldest first"),
        (NameAsc, "Name A–Z"),
        (NameDesc, "Name Z–A")
    };

    public static string Normalise(string? sort) =>
        Options.Any(o => o.Value == sort) ? sort! : Newest;
}
