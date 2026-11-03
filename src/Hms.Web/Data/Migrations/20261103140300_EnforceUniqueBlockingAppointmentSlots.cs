using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Web.Data.Migrations;

public partial class EnforceUniqueBlockingAppointmentSlots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Databases created by the previous schema could contain concurrent bookings
        // for one blocking slot because its index was not unique. Preserve the oldest
        // booking and deterministically free every later duplicate before enforcing
        // uniqueness, so an existing local installation can always finish upgrading.
        migrationBuilder.Sql("""
            UPDATE "Appointments" AS "duplicate"
            SET "Status" = 'Cancelled',
                "UpdatedAt" = CURRENT_TIMESTAMP
            WHERE "duplicate"."Status" IN ('Pending', 'Confirmed', 'Checked In', 'In Progress')
              AND EXISTS (
                  SELECT 1
                  FROM "Appointments" AS "winner"
                  WHERE "winner"."DoctorId" = "duplicate"."DoctorId"
                    AND "winner"."AppointmentDate" = "duplicate"."AppointmentDate"
                    AND "winner"."StartTime" = "duplicate"."StartTime"
                    AND "winner"."Status" IN ('Pending', 'Confirmed', 'Checked In', 'In Progress')
                    AND "winner"."Id" < "duplicate"."Id"
              );
            """);

        migrationBuilder.DropIndex(
            name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
            table: "Appointments");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
            table: "Appointments",
            columns: new[] { "DoctorId", "AppointmentDate", "StartTime" },
            unique: true,
            filter: "\"Status\" IN ('Pending', 'Confirmed', 'Checked In', 'In Progress')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
            table: "Appointments");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
            table: "Appointments",
            columns: new[] { "DoctorId", "AppointmentDate", "StartTime" });
    }
}
