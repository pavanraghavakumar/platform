using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Lab;

/// <summary>
/// The result form of an In Progress lab request (Requirement 8.5) and the result page
/// of a completed one (Requirement 8.6).
/// </summary>
public class LabResultViewModel
{
    public LabRequest Request { get; set; } = new();

    public string? ResultValue { get; set; }

    public string? Remarks { get; set; }
}
