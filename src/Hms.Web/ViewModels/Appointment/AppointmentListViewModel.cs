using Hms.Web.Models.Entities;

namespace Hms.Web.ViewModels.Appointment;

using AppointmentEntity = Hms.Web.Models.Entities.Appointment;

/// <summary>The appointment list with its status filter and the actions each row may offer.</summary>
public class AppointmentListViewModel
{
    public static readonly string[] Statuses =
    {
        EntityStatus.AppointmentPending,
        EntityStatus.AppointmentConfirmed,
        EntityStatus.AppointmentCheckedIn,
        EntityStatus.AppointmentInProgress,
        EntityStatus.AppointmentCompleted,
        EntityStatus.AppointmentCancelled,
        EntityStatus.AppointmentNoShow
    };

    public string? Status { get; set; }

    public bool IsReceptionist { get; set; }

    public bool IsDoctor { get; set; }

    public bool IsPatient { get; set; }

    public IReadOnlyList<AppointmentEntity> Appointments { get; set; } = Array.Empty<AppointmentEntity>();
}
