namespace Hms.Web.Data;

/// <summary>The four roles every signed-in user holds exactly one of.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Receptionist = "Receptionist";
    public const string Patient = "Patient";

    public static readonly string[] All = { Admin, Doctor, Receptionist, Patient };
}
