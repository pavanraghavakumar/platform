namespace Hms.Web.Models.Entities;

public class DoctorSchedule
{
    public int Id { get; set; }

    public int DoctorId { get; set; }

    public Doctor? Doctor { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public bool IsWorking { get; set; }

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
