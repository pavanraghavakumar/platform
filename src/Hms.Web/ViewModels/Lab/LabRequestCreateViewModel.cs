using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Lab;

/// <summary>
/// The tests a Doctor requests for a patient during a consultation (Requirement 8.2).
/// One lab request is created per selected test.
/// </summary>
public class LabRequestCreateViewModel
{
    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int? AppointmentId { get; set; }

    public string? PatientName { get; set; }

    public IReadOnlyList<LaboratoryTest> AvailableTests { get; set; } = Array.Empty<LaboratoryTest>();

    public IReadOnlyList<int> SelectedTestIds { get; set; } = Array.Empty<int>();
}
