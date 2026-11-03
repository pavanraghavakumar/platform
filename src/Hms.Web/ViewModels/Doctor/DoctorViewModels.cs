using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Doctor;

public class DoctorListViewModel
{
    public int? DepartmentId { get; set; }
    public IReadOnlyList<DoctorDepartmentOption> Departments { get; set; } = Array.Empty<DoctorDepartmentOption>();
    public IReadOnlyList<DoctorListRow> Rows { get; set; } = Array.Empty<DoctorListRow>();
}

public class DoctorListRow
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DoctorDepartmentOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DoctorCreateViewModel
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Specialization { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Choose a department.")]
    public int DepartmentId { get; set; }

    [Required]
    public string Qualifications { get; set; } = string.Empty;

    [Range(0, 80)]
    public int YearsOfExperience { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Consultation fee cannot be negative.")]
    public decimal ConsultationFee { get; set; }

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class DoctorEditViewModel
{
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Specialization { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Choose a department.")]
    public int DepartmentId { get; set; }

    [Required]
    public string Qualifications { get; set; } = string.Empty;

    [Range(0, 80)]
    public int YearsOfExperience { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Consultation fee cannot be negative.")]
    public decimal ConsultationFee { get; set; }

    public List<DoctorScheduleInput> Schedules { get; set; } = new();
}

public class DoctorScheduleInput
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorking { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

public class DoctorDetailsViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public decimal ConsultationFee { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<DoctorScheduleInput> Schedules { get; set; } = Array.Empty<DoctorScheduleInput>();
    public IReadOnlyList<DoctorAppointmentHistoryRow> Appointments { get; set; } = Array.Empty<DoctorAppointmentHistoryRow>();
}

public class DoctorAppointmentHistoryRow
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
