using Hms.Web.Controllers;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hms.Tests.Report;

public class ReportTests
{
    [Fact]
    public async Task Report_service_builds_all_required_period_figures_and_tables()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HmsDbContext>().UseSqlite(connection).Options;
        await using var db = new HmsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var day = new DateTime(2026, 10, 15);

        var department = new Department { Name = "Report Cardiology" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        var doctor = new Doctor { FullName = "Dr Report", Email = "report.doctor@test.local", Phone = "1", DepartmentId = department.Id };
        var patient = new Patient { PatientCode = "P-REPORT", FullName = "Report Patient", Phone = "2", Gender = "Other", CreatedAt = day };
        db.AddRange(doctor, patient);
        var category = new MedicineCategory { Name = "Report Category" };
        db.MedicineCategories.Add(category);
        await db.SaveChangesAsync();
        var medicine = new Medicine { Name = "Report Medicine", CategoryId = category.Id, StockQuantity = 2, ReorderLevel = 5 };
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync();
        db.MedicineBatches.Add(new MedicineBatch { MedicineId = medicine.Id, BatchNumber = "REPORT", Quantity = 2, ExpiryDate = day.AddMonths(6) });
        var appointment = new Appointment { PatientId = patient.Id, DoctorId = doctor.Id, AppointmentDate = day, StartTime = TimeSpan.FromHours(9), Status = EntityStatus.AppointmentCompleted };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        db.MedicalRecords.Add(new MedicalRecord { AppointmentId = appointment.Id, PatientId = patient.Id, DoctorId = doctor.Id, Diagnosis = "Report", Status = EntityStatus.RecordFinal, CreatedAt = day, UpdatedAt = day });
        var test = new LaboratoryTest { Name = "Report Test", Category = "Blood", Price = 20 };
        db.LaboratoryTests.Add(test);
        await db.SaveChangesAsync();
        db.LabRequests.Add(new LabRequest { PatientId = patient.Id, DoctorId = doctor.Id, AppointmentId = appointment.Id, LaboratoryTestId = test.Id, Status = EntityStatus.LabCompleted, CreatedAt = day, ResultEnteredAt = day });
        var invoice = new Invoice { InvoiceNumber = "INV-REPORT", PatientId = patient.Id, AppointmentId = appointment.Id, Total = 500, PaidAmount = 300, Status = EntityStatus.InvoicePartiallyPaid, CreatedAt = day };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        db.Payments.AddRange(
            new Payment { InvoiceId = invoice.Id, Amount = 300, Status = EntityStatus.PaymentRecorded, PaidAt = day },
            new Payment { InvoiceId = invoice.Id, Amount = 50, Status = EntityStatus.PaymentRefunded, PaidAt = day, UpdatedAt = day });
        await db.SaveChangesAsync();

        var result = await new ReportService(db).GetReportsAsync(day.AddDays(-1), day.AddDays(1));

        Assert.Contains(result.Patients.Patients, p => p.PatientCode == "P-REPORT");
        Assert.Contains(result.Appointments.Statuses, s => s.Status == EntityStatus.AppointmentCompleted && s.Count == 1);
        Assert.Contains(result.Departments.Departments, d => d.DepartmentName == department.Name && d.DoctorCount == 1 && d.AppointmentCount == 1);
        Assert.Contains(result.Doctors.Doctors, d => d.DoctorName == doctor.FullName && d.CompletedConsultations == 1 && d.Revenue == 300);
        Assert.Equal(1, result.Laboratory.CompletedResults);
        Assert.Contains(result.Pharmacy.Medicines, m => m.MedicineName == medicine.Name && m.Status == EntityStatus.MedicineLowStock);
        Assert.Equal(350, result.Revenue.TotalPaymentsRecorded);
        Assert.Equal(50, result.Revenue.TotalRefunded);
        Assert.Equal(300, result.Revenue.NetRevenue);
        Assert.Equal(200, result.Revenue.OutstandingBalance);
        Assert.Contains(result.Revenue.DailyRevenue, p => p.Date == day && p.NetRevenue == 300);
    }

    [Fact]
    public async Task Revenue_report_counts_collection_and_refund_on_their_actual_days()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HmsDbContext>().UseSqlite(connection).Options;
        await using var db = new HmsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var collectionDay = DateTime.UtcNow.Date.AddDays(-1);
        var refundDay = DateTime.UtcNow.Date;
        var patient = new Patient
        {
            PatientCode = "P-REFUND-REPORT",
            FullName = "Refund Report Patient",
            Phone = "3",
            Gender = "Other"
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-REFUND-REPORT",
            PatientId = patient.Id,
            Total = 125m,
            Status = EntityStatus.InvoiceUnpaid,
            CreatedAt = collectionDay
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var billing = new BillingService(db, new AuditService(db), new NotificationService(db));
        var payment = await billing.RecordPaymentAsync(invoice.Id, 125m, "Cash", collectionDay, "reception@test.local");
        Assert.True(await billing.RefundAsync(payment.Id, "reception@test.local"));

        var report = await new ReportService(db).GetRevenueReportAsync(new ReportPeriod(collectionDay, refundDay));

        Assert.Equal(125m, report.TotalPaymentsRecorded);
        Assert.Equal(125m, report.TotalRefunded);
        Assert.Equal(0m, report.NetRevenue);
        Assert.Contains(report.DailyRevenue, point => point.Date == collectionDay && point.NetRevenue == 125m);
        Assert.Contains(report.DailyRevenue, point => point.Date == refundDay && point.NetRevenue == -125m);
    }

    [Fact]
    public async Task Doctor_report_counts_a_draft_finalized_during_the_period()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HmsDbContext>().UseSqlite(connection).Options;
        await using var db = new HmsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var reportDay = DateTime.UtcNow.Date;
        var department = new Department { Name = "Finalization Report Department" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        var doctor = new Doctor
        {
            FullName = "Dr Finalization Report",
            Email = "finalization.report@test.local",
            Phone = "4",
            DepartmentId = department.Id
        };
        var patient = new Patient
        {
            PatientCode = "P-FINALIZATION-REPORT",
            FullName = "Finalization Report Patient",
            Phone = "5",
            Gender = "Other"
        };
        db.AddRange(doctor, patient);
        await db.SaveChangesAsync();
        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = reportDay,
            StartTime = TimeSpan.FromHours(10),
            Status = EntityStatus.AppointmentInProgress
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        var draft = new MedicalRecord
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Diagnosis = "Draft before period",
            Status = EntityStatus.RecordDraft,
            CreatedAt = reportDay.AddDays(-2),
            UpdatedAt = reportDay.AddDays(-2)
        };
        db.MedicalRecords.Add(draft);
        await db.SaveChangesAsync();

        var consultation = new ConsultationService(db, new AuditService(db));
        Assert.True(await consultation.FinalizeAsync(draft.Id));

        var report = await new ReportService(db).GetDoctorReportAsync(new ReportPeriod(reportDay, reportDay));

        Assert.Contains(report.Doctors, row => row.DoctorName == doctor.FullName && row.CompletedConsultations == 1);
    }

    [Fact]
    public void Report_controller_is_restricted_to_admins()
    {
        var attribute = Assert.Single(typeof(ReportController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(Roles.Admin, attribute.Roles);
    }

    [Fact]
    public void Reversed_dates_are_normalized_to_an_inclusive_period()
    {
        var period = ReportPeriod.Create(new DateTime(2026, 10, 20), new DateTime(2026, 10, 10));
        Assert.Equal(new DateTime(2026, 10, 10), period.StartDate);
        Assert.Equal(new DateTime(2026, 10, 20), period.EndDate);
    }
}
