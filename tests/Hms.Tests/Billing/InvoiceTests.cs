using System.Security.Claims;
using Hms.Web.Controllers;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Billing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Hms.Tests.Billing;

/// <summary>
/// Billing and payments: invoice generation from an appointment, the additional service charge,
/// discount and tax totals, payments, refunds, the audit trail and the printable invoice.
/// Each test migrates its own throwaway SQLite file and asserts only on the rows it writes.
/// </summary>
public class InvoiceTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public InvoiceTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-invoice-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<HmsDbContext>(options => options.UseSqlite(_connection));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<HmsDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBillingService, BillingService>();

        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Generate_adds_consultation_fee_completed_labs_and_prescription_medicines()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        var items = await db.InvoiceItems.Where(i => i.InvoiceId == invoice.Id).ToListAsync();
        Assert.Contains(items, i => i.Description.Contains("Consultation fee") && i.Amount == 500m);
        Assert.Contains(items, i => i.Description.Contains("Complete Blood Count") && i.Amount == 300m);
        Assert.Contains(items, i => i.Description.Contains("Paracetamol") && i.Amount == 20m);
        // The requested (not completed) lab request is not billed.
        Assert.DoesNotContain(items, i => i.Description.Contains("Lipid Profile"));
        Assert.Equal(820m, invoice.Total);
        Assert.Equal(EntityStatus.InvoiceUnpaid, invoice.Status);
    }

    [Fact]
    public async Task Generate_adds_the_additional_service_charge_as_a_line()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, "Dressing", 150m);

        var items = await db.InvoiceItems.Where(i => i.InvoiceId == invoice.Id).ToListAsync();
        Assert.Contains(items, i => i.Description == "Dressing" && i.Amount == 150m);
        Assert.Equal(970m, invoice.Total);
    }

    [Fact]
    public async Task Total_applies_discount_then_tax_and_rounds_to_two_decimals()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

        // Lines total 820, discount 20 -> taxable 800, tax 5% -> 40, total 840.00.
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 20m, 5m, null, 0m);

        Assert.Equal(840.00m, invoice.Total);
        Assert.Equal(EntityStatus.InvoiceUnpaid, invoice.Status);
    }

    [Fact]
    public void ComputeTotal_rounds_to_two_decimal_places()
    {
        // 100 - 10 = 90; 90 * 7.5% = 6.75; total 96.75.
        Assert.Equal(96.75m, IBillingService.ComputeTotal(100m, 10m, 7.5m));
        // 100 - 0 = 100; 100 * 0% = 0; total 100.00.
        Assert.Equal(100.00m, IBillingService.ComputeTotal(100m, 0m, 0m));
        // 33.33 * 18% = 5.9994 -> 39.33.
        Assert.Equal(39.33m, IBillingService.ComputeTotal(33.33m, 0m, 18m));
    }

    [Fact]
    public void ComputeTotal_caps_discount_at_lines_total_and_tax_at_one_hundred_percent()
    {
        Assert.Equal(0m, IBillingService.ComputeTotal(820m, 1000m, 5m));
        Assert.Equal(1640m, IBillingService.ComputeTotal(820m, 0m, 500m));
    }

    [Fact]
    public async Task Generate_caps_excessive_discount_and_tax_before_saving()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 1000m, 500m, null, 0m);

        Assert.Equal(820m, invoice.DiscountAmount);
        Assert.Equal(100m, invoice.TaxRate);
        Assert.Equal(0m, invoice.Total);
        Assert.Equal(EntityStatus.InvoiceUnpaid, invoice.Status);
    }

    [Fact]
    public async Task Payment_partially_pays_then_fully_pays_the_invoice()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        var payment = await billing.RecordPaymentAsync(invoice.Id, 50m, "Cash", DateTime.Today, "reception@hms.local");

        var reloaded = await db.Invoices.FindAsync(invoice.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(50m, reloaded!.PaidAmount);
        Assert.Equal(EntityStatus.InvoicePartiallyPaid, reloaded.Status);
        Assert.Equal("Cash", payment.Method);
        Assert.Equal(DateTime.Today, payment.PaidAt);
        Assert.Equal(770m, reloaded.Total - reloaded.PaidAmount);

        await billing.RecordPaymentAsync(invoice.Id, 770m, "Card", DateTime.Today, "reception@hms.local");

        var paid = await db.Invoices.FindAsync(invoice.Id);
        Assert.Equal(820m, paid!.PaidAmount);
        Assert.Equal(EntityStatus.InvoicePaid, paid.Status);
        Assert.Equal(0m, paid.Total - paid.PaidAmount);
    }

    [Fact]
    public async Task Payment_larger_than_outstanding_is_rejected_and_not_saved()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => billing.RecordPaymentAsync(invoice.Id, 900m, "Cash", DateTime.Today, "reception@hms.local"));

        Assert.Equal("Payment cannot be more than the outstanding balance.", ex.Message);
        Assert.Empty(await db.Payments.Where(p => p.InvoiceId == invoice.Id).ToListAsync());
        var reloaded = await db.Invoices.FindAsync(invoice.Id);
        Assert.Equal(0m, reloaded!.PaidAmount);
        Assert.Equal(EntityStatus.InvoiceUnpaid, reloaded.Status);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-50")]
    public async Task Non_positive_payment_is_rejected_by_the_service_without_changing_the_invoice(string amountText)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);
        var amount = decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => billing.RecordPaymentAsync(invoice.Id, amount, "Cash", DateTime.Today, "reception@hms.local"));

        Assert.Equal("Payment amount must be greater than zero.", ex.Message);
        Assert.Empty(await db.Payments.Where(p => p.InvoiceId == invoice.Id).ToListAsync());
        Assert.Empty(await db.AuditEntries.Where(a => a.RecordType == "Invoice" && a.RecordId == invoice.Id).ToListAsync());
        var reloaded = await db.Invoices.FindAsync(invoice.Id);
        Assert.Equal(0m, reloaded!.PaidAmount);
        Assert.Equal(EntityStatus.InvoiceUnpaid, reloaded.Status);
    }

    [Fact]
    public async Task Refund_reverses_the_paid_amount_sets_status_back_and_notifies_the_patient()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);
        var payment = await billing.RecordPaymentAsync(invoice.Id, 820m, "UPI", DateTime.Today, "reception@hms.local");

        var refunded = await billing.RefundAsync(payment.Id, "reception@hms.local");

        Assert.True(refunded);
        var reloaded = await db.Invoices.FindAsync(invoice.Id);
        Assert.Equal(0m, reloaded!.PaidAmount);
        Assert.Equal(EntityStatus.InvoiceUnpaid, reloaded.Status);

        var refundedPayment = await db.Payments.FindAsync(payment.Id);
        Assert.Equal(EntityStatus.PaymentRefunded, refundedPayment!.Status);

        var notifications = await db.Notifications.Where(n => n.UserId == fixture.PatientUserId).ToListAsync();
        Assert.Contains(notifications, n => n.Message.Contains("refunded"));
    }

    [Fact]
    public async Task Recording_a_payment_writes_an_audit_entry_with_the_status_change()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        await billing.RecordPaymentAsync(invoice.Id, 50m, "Cash", DateTime.Today, "reception@hms.local");

        var entries = await db.AuditEntries
            .Where(a => a.RecordType == "Invoice" && a.RecordId == invoice.Id)
            .ToListAsync();

        var entry = Assert.Single(entries);
        Assert.Equal("Status", entry.FieldName);
        Assert.Equal(EntityStatus.InvoiceUnpaid, entry.OldValue);
        Assert.Equal(EntityStatus.InvoicePartiallyPaid, entry.NewValue);
        Assert.Equal("reception@hms.local", entry.UserName);
    }

    [Fact]
    public async Task Every_partial_payment_writes_an_audit_entry_even_when_status_is_unchanged()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        await billing.RecordPaymentAsync(invoice.Id, 50m, "Cash", DateTime.Today, "first.reception@hms.local");
        var beforeSecondPayment = DateTime.UtcNow;
        await billing.RecordPaymentAsync(invoice.Id, 50m, "Card", DateTime.Today, "second.reception@hms.local");

        var entries = await db.AuditEntries
            .Where(a => a.RecordType == "Invoice" && a.RecordId == invoice.Id && a.FieldName == "Status")
            .OrderBy(a => a.Id)
            .ToListAsync();

        Assert.Equal(2, entries.Count);
        Assert.Equal(EntityStatus.InvoiceUnpaid, entries[0].OldValue);
        Assert.Equal(EntityStatus.InvoicePartiallyPaid, entries[0].NewValue);
        Assert.Equal("first.reception@hms.local", entries[0].UserName);
        Assert.Equal(EntityStatus.InvoicePartiallyPaid, entries[1].OldValue);
        Assert.Equal(EntityStatus.InvoicePartiallyPaid, entries[1].NewValue);
        Assert.Equal("second.reception@hms.local", entries[1].UserName);
        Assert.True(entries[1].ChangedAt >= beforeSecondPayment);
    }

    [Fact]
    public async Task Pay_action_overpayment_shows_the_rule_message_and_saves_nothing()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        var controller = CreateController(scope, Roles.Receptionist);
        var result = await controller.Pay(invoice.Id, 900m, "Cash", DateTime.Today);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Payment cannot be more than the outstanding balance.", controller.TempData["Error"]);
        Assert.Empty(await db.Payments.Where(p => p.InvoiceId == invoice.Id).ToListAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-10")]
    public async Task Pay_action_rejects_missing_zero_and_negative_amounts(string? amountText)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);
        decimal? amount = amountText is null
            ? null
            : decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);

        var controller = CreateController(scope, Roles.Receptionist);
        var result = await controller.Pay(invoice.Id, amount, "Cash", DateTime.Today);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Payment amount must be greater than zero.", controller.TempData["Error"]);
        Assert.Empty(await db.Payments.Where(p => p.InvoiceId == invoice.Id).ToListAsync());
        var reloaded = await db.Invoices.FindAsync(invoice.Id);
        Assert.Equal(0m, reloaded!.PaidAmount);
        Assert.Equal(EntityStatus.InvoiceUnpaid, reloaded.Status);
    }

    [Fact]
    public async Task Print_page_shows_the_hospital_name_lines_and_totals()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        db.SystemSettings.Add(new SystemSetting
        {
            Key = SystemSetting.HospitalNameKey,
            Value = SystemSetting.DefaultHospitalName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 20m, 5m, "Dressing", 150m);

        var controller = CreateController(scope, Roles.Receptionist);
        var result = await controller.Print(invoice.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceDetailsViewModel>(view.Model);
        Assert.Equal("Greenfield General Hospital", SystemSetting.DefaultHospitalName);
        Assert.Equal(invoice.Id, model.Invoice.Id);
        Assert.Contains(model.Items, i => i.Description.Contains("Consultation fee"));
        Assert.Contains(model.Items, i => i.Description.Contains("Complete Blood Count"));
        Assert.Contains(model.Items, i => i.Description.Contains("Paracetamol"));
        Assert.Contains(model.Items, i => i.Description == "Dressing");
        Assert.Equal(20m, model.Invoice.DiscountAmount);
        Assert.Equal(5m, model.Invoice.TaxRate);
        // 970 lines - 20 discount = 950; 5% tax = 47.50; total 997.50.
        Assert.Equal(997.50m, model.Invoice.Total);
        Assert.Equal(997.50m, model.Outstanding);
    }

    [Fact]
    public async Task Details_page_rejects_a_patient_who_does_not_own_the_invoice()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        var otherUser = new ApplicationUser
        {
            UserName = $"other.{Guid.NewGuid():N}@hms.local",
            Email = $"other.{Guid.NewGuid():N}@hms.local",
            EmailConfirmed = true,
            FullName = "Other patient",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();
        db.Patients.Add(new Patient
        {
            PatientCode = "P-90002",
            FullName = "Other patient",
            Phone = "9000000003",
            Gender = "Female",
            Status = EntityStatus.Active,
            UserId = otherUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, Roles.Patient, otherUser);
        var result = await controller.Details(invoice.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccessDenied", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
    }

    [Fact]
    public async Task Details_page_opens_for_the_patient_who_owns_the_invoice()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        var owner = await db.Users.FindAsync(fixture.PatientUserId);
        var controller = CreateController(scope, Roles.Patient, owner!);
        var result = await controller.Details(invoice.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceDetailsViewModel>(view.Model);
        Assert.Equal(invoice.Id, model.Invoice.Id);
        Assert.False(model.CanRecordPayment);
    }

    [Fact]
    public async Task Invoice_list_filters_by_status()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        await db.Database.MigrateAsync();

        var fixture = await SeedAppointmentAsync(db);
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var invoice = await billing.GenerateFromAppointmentAsync(fixture.AppointmentId, 0m, 0m, null, 0m);

        var controller = CreateController(scope, Roles.Receptionist);
        var unpaid = Assert.IsType<ViewResult>(await controller.Index(EntityStatus.InvoiceUnpaid));
        var unpaidList = Assert.IsAssignableFrom<IReadOnlyList<Invoice>>(unpaid.Model);
        Assert.Contains(unpaidList, i => i.Id == invoice.Id);

        var paid = Assert.IsType<ViewResult>(await controller.Index(EntityStatus.InvoicePaid));
        var paidList = Assert.IsAssignableFrom<IReadOnlyList<Invoice>>(paid.Model);
        Assert.DoesNotContain(paidList, i => i.Id == invoice.Id);
    }

    private static InvoiceController CreateController(IServiceScope scope, string role, ApplicationUser? user = null)
    {
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var controller = new InvoiceController(db, billing, userManager);

        var claims = new List<Claim> { new(ClaimTypes.Name, user?.UserName ?? "reception@hms.local") };
        if (user is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
        }

        claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());

        return controller;
    }

    private static async Task<AppointmentFixture> SeedAppointmentAsync(HmsDbContext db)
    {
        var now = DateTime.UtcNow;

        var department = new Department { Name = $"Billing Dept {Guid.NewGuid():N}", Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = "Dr. Billing Test",
            Email = $"dr.billing.{Guid.NewGuid():N}@hms.local",
            Phone = "9000000001",
            Specialization = "General Medicine",
            DepartmentId = department.Id,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Doctors.Add(doctor);

        var patientUser = new ApplicationUser
        {
            UserName = $"patient.billing.{Guid.NewGuid():N}@hms.local",
            Email = $"patient.billing.{Guid.NewGuid():N}@hms.local",
            EmailConfirmed = true,
            FullName = "Invoice patient",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Users.Add(patientUser);
        await db.SaveChangesAsync();

        var patient = new Patient
        {
            PatientCode = "P-90001",
            FullName = "Invoice patient",
            Phone = "9000000002",
            Gender = "Male",
            Status = EntityStatus.Active,
            UserId = patientUser.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateTime.Today,
            StartTime = new TimeSpan(10, 0, 0),
            Status = EntityStatus.AppointmentCompleted,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var record = new MedicalRecord
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Diagnosis = "Fever",
            Status = EntityStatus.RecordFinal,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();

        var medicine = new Medicine
        {
            Name = "Paracetamol",
            UnitPrice = 20m,
            StockQuantity = 100,
            Status = EntityStatus.MedicineInStock,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Medicines.Add(medicine);

        var completedTest = new LaboratoryTest { Name = "Complete Blood Count", Price = 300m, CreatedAt = now, UpdatedAt = now };
        var requestedTest = new LaboratoryTest { Name = "Lipid Profile", Price = 400m, CreatedAt = now, UpdatedAt = now };
        db.LaboratoryTests.AddRange(completedTest, requestedTest);
        await db.SaveChangesAsync();

        var prescription = new Prescription
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            MedicalRecordId = record.Id,
            Status = EntityStatus.PrescriptionActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync();

        db.PrescriptionItems.Add(new PrescriptionItem
        {
            PrescriptionId = prescription.Id,
            MedicineId = medicine.Id,
            Dosage = "500mg",
            Frequency = "Twice a day",
            DurationDays = 5,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.LabRequests.Add(new LabRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentId = appointment.Id,
            LaboratoryTestId = completedTest.Id,
            Status = EntityStatus.LabCompleted,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.LabRequests.Add(new LabRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentId = appointment.Id,
            LaboratoryTestId = requestedTest.Id,
            Status = EntityStatus.LabRequested,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync();

        return new AppointmentFixture(appointment.Id, patientUser.Id);
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private sealed record AppointmentFixture(int AppointmentId, string PatientUserId);

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Hms.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _data = new Dictionary<string, object>();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _data = values;
    }
}
