using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Data;

/// <summary>
/// Creates the demo records and the four demo logins only when the database holds no
/// records, and never when <c>ASPNETCORE_ENVIRONMENT</c> is <c>Production</c>.
/// </summary>
public class DemoDataSeeder
{
    public const string AdminEmail = "admin@hms.local";
    public const string AdminPassword = "Admin@123";
    public const string DoctorEmail = "dr.arjun@hms.local";
    public const string DoctorPassword = "Doctor@123";
    public const string ReceptionistEmail = "reception@hms.local";
    public const string ReceptionistPassword = "Reception@123";
    public const string PatientEmail = "patient.ravi@hms.local";
    public const string PatientPassword = "Patient@123";

    private readonly HmsDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        HmsDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        IHostEnvironment environment,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _users = users;
        _roles = roles;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_environment.IsProduction())
        {
            _logger.LogInformation("Demo data seeding skipped in Production.");
            return;
        }

        await EnsureRolesAsync();

        if (await _db.Patients.AnyAsync() || await _db.Departments.AnyAsync())
        {
            _logger.LogInformation("Demo data seeding skipped: the database already holds records.");
            return;
        }

        var now = DateTime.UtcNow;

        // Departments
        var cardiology = new Department { Name = "Cardiology", Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now };
        var general = new Department { Name = "General Medicine", Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now };
        var ortho = new Department { Name = "Orthopaedics", Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now };
        var paediatrics = new Department { Name = "Paediatrics", Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now };
        _db.Departments.AddRange(cardiology, general, ortho, paediatrics);
        await _db.SaveChangesAsync();

        // Demo logins
        var adminUser = await CreateUserAsync(AdminEmail, AdminPassword, "System Administrator", Roles.Admin);
        var doctorUser = await CreateUserAsync(DoctorEmail, DoctorPassword, "Dr. Arjun Mehta", Roles.Doctor);
        var receptionUser = await CreateUserAsync(ReceptionistEmail, ReceptionistPassword, "Priya Sharma", Roles.Receptionist);
        var patientUser = await CreateUserAsync(PatientEmail, PatientPassword, "Ravi Kumar", Roles.Patient);

        // Doctors
        var doctorArjun = new Doctor
        {
            FullName = "Dr. Arjun Mehta",
            Email = DoctorEmail,
            Phone = "9876500001",
            Specialization = "Cardiology",
            DepartmentId = cardiology.Id,
            Qualifications = "MBBS, MD (Cardiology)",
            YearsOfExperience = 12,
            ConsultationFee = 800m,
            Status = EntityStatus.Active,
            UserId = doctorUser.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        var doctorNeha = new Doctor
        {
            FullName = "Dr. Neha Rao",
            Email = "dr.neha@hms.local",
            Phone = "9876500002",
            Specialization = "General Medicine",
            DepartmentId = general.Id,
            Qualifications = "MBBS, MD",
            YearsOfExperience = 8,
            ConsultationFee = 500m,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var doctorVikram = new Doctor
        {
            FullName = "Dr. Vikram Singh",
            Email = "dr.vikram@hms.local",
            Phone = "9876500003",
            Specialization = "Orthopaedics",
            DepartmentId = ortho.Id,
            Qualifications = "MBBS, MS (Ortho)",
            YearsOfExperience = 15,
            ConsultationFee = 900m,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var doctorAnita = new Doctor
        {
            FullName = "Dr. Anita Desai",
            Email = "dr.anita@hms.local",
            Phone = "9876500004",
            Specialization = "Paediatrics",
            DepartmentId = paediatrics.Id,
            Qualifications = "MBBS, DCH",
            YearsOfExperience = 10,
            ConsultationFee = 600m,
            Status = EntityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Doctors.AddRange(doctorArjun, doctorNeha, doctorVikram, doctorAnita);
        await _db.SaveChangesAsync();

        // Working hours Monday to Saturday, 09:00 to 17:00
        foreach (var doctor in new[] { doctorArjun, doctorNeha, doctorVikram, doctorAnita })
        {
            for (var day = DayOfWeek.Monday; day <= DayOfWeek.Saturday; day++)
            {
                _db.DoctorSchedules.Add(new DoctorSchedule
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = day,
                    IsWorking = true,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
        await _db.SaveChangesAsync();

        // Staff
        _db.Staff.AddRange(
            new Staff { FullName = "Priya Sharma", Email = ReceptionistEmail, Phone = "9876510001", Role = Roles.Receptionist, Designation = "Front Desk Executive", DepartmentId = general.Id, Status = EntityStatus.Active, UserId = receptionUser.Id, CreatedAt = now, UpdatedAt = now },
            new Staff { FullName = "Ramesh Iyer", Email = "ramesh@hms.local", Phone = "9876510002", Role = Roles.Receptionist, Designation = "Billing Officer", DepartmentId = general.Id, Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now },
            new Staff { FullName = "Sunita Verma", Email = "sunita@hms.local", Phone = "9876510003", Role = Roles.Admin, Designation = "Hospital Administrator", DepartmentId = general.Id, Status = EntityStatus.Active, CreatedAt = now, UpdatedAt = now });
        await _db.SaveChangesAsync();

        // Patients
        var patientNames = new[]
        {
            "Ravi Kumar", "Meera Joshi", "Sanjay Patel", "Lakshmi Nair", "Arun Reddy",
            "Kavita Menon", "Deepak Gupta", "Pooja Iyer", "Rahul Bose", "Anjali Shah"
        };
        var patients = new List<Patient>();
        for (var i = 0; i < patientNames.Length; i++)
        {
            var patient = new Patient
            {
                PatientCode = $"P-{i + 1:D5}",
                FullName = patientNames[i],
                DateOfBirth = new DateTime(1980 + i, 1 + (i % 12), 1 + (i % 27), 0, 0, 0, DateTimeKind.Utc),
                Gender = i % 2 == 0 ? "Male" : "Female",
                Phone = $"98765{20000 + i}",
                Email = i == 0 ? PatientEmail : $"patient{i + 1}@hms.local",
                Address = $"{i + 1} MG Road, Bengaluru",
                EmergencyContactName = "Family Contact",
                EmergencyContactPhone = $"98765{30000 + i}",
                MedicalHistory = "No major past illness.",
                Allergies = i % 3 == 0 ? "Penicillin" : "None",
                Status = EntityStatus.Active,
                UserId = i == 0 ? patientUser.Id : null,
                CreatedAt = now.AddDays(-30 + i),
                UpdatedAt = now
            };
            patients.Add(patient);
        }
        _db.Patients.AddRange(patients);
        await _db.SaveChangesAsync();

        // Medicines
        var categoryTablets = new MedicineCategory { Name = "Tablets", CreatedAt = now, UpdatedAt = now };
        var categorySyrups = new MedicineCategory { Name = "Syrups", CreatedAt = now, UpdatedAt = now };
        var supplier = new Supplier { Name = "MediSupply Pvt Ltd", Phone = "0801234567", Email = "sales@medisupply.local", CreatedAt = now, UpdatedAt = now };
        _db.MedicineCategories.AddRange(categoryTablets, categorySyrups);
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        var medicineNames = new[]
        {
            "Paracetamol 500mg", "Amoxicillin 250mg", "Ibuprofen 400mg", "Cetirizine 10mg", "Omeprazole 20mg",
            "Metformin 500mg", "Amlodipine 5mg", "Azithromycin 500mg", "Cough Syrup", "ORS Powder"
        };
        var medicines = new List<Medicine>();
        for (var i = 0; i < medicineNames.Length; i++)
        {
            var medicine = new Medicine
            {
                Name = medicineNames[i],
                CategoryId = i == 8 ? categorySyrups.Id : categoryTablets.Id,
                SupplierId = supplier.Id,
                UnitPrice = 20m + (i * 15m),
                ReorderLevel = 20,
                StockQuantity = 100 + (i * 10),
                Status = EntityStatus.MedicineInStock,
                CreatedAt = now,
                UpdatedAt = now
            };
            medicines.Add(medicine);
        }
        _db.Medicines.AddRange(medicines);
        await _db.SaveChangesAsync();

        foreach (var medicine in medicines)
        {
            _db.MedicineBatches.Add(new MedicineBatch
            {
                MedicineId = medicine.Id,
                BatchNumber = $"B{medicine.Id:D4}",
                Quantity = medicine.StockQuantity,
                ExpiryDate = now.AddYears(2),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await _db.SaveChangesAsync();

        // Laboratory tests
        var labTests = new[]
        {
            new LaboratoryTest { Name = "Complete Blood Count", Category = "Haematology", Price = 350m, NormalRange = "4.5-11.0 x10^9/L", CreatedAt = now, UpdatedAt = now },
            new LaboratoryTest { Name = "Blood Sugar (Fasting)", Category = "Biochemistry", Price = 200m, NormalRange = "70-100 mg/dL", CreatedAt = now, UpdatedAt = now },
            new LaboratoryTest { Name = "Lipid Profile", Category = "Biochemistry", Price = 600m, NormalRange = "Total < 200 mg/dL", CreatedAt = now, UpdatedAt = now },
            new LaboratoryTest { Name = "Thyroid Profile", Category = "Hormones", Price = 550m, NormalRange = "TSH 0.4-4.0 mIU/L", CreatedAt = now, UpdatedAt = now },
            new LaboratoryTest { Name = "Urine Routine", Category = "Pathology", Price = 150m, NormalRange = "Normal", CreatedAt = now, UpdatedAt = now },
            new LaboratoryTest { Name = "ECG", Category = "Cardiology", Price = 400m, NormalRange = "Normal sinus rhythm", CreatedAt = now, UpdatedAt = now }
        };
        _db.LaboratoryTests.AddRange(labTests);
        await _db.SaveChangesAsync();

        // Appointments across all 7 statuses: 12 appointments, the first 6 Completed so each
        // can carry its own medical record (one record per appointment), the rest cover the
        // other 6 statuses.
        var statuses = new[]
        {
            EntityStatus.AppointmentCompleted, EntityStatus.AppointmentCompleted, EntityStatus.AppointmentCompleted,
            EntityStatus.AppointmentCompleted, EntityStatus.AppointmentCompleted, EntityStatus.AppointmentCompleted,
            EntityStatus.AppointmentPending, EntityStatus.AppointmentConfirmed, EntityStatus.AppointmentCheckedIn,
            EntityStatus.AppointmentInProgress, EntityStatus.AppointmentCancelled, EntityStatus.AppointmentNoShow
        };
        var appointments = new List<Appointment>();
        for (var i = 0; i < 12; i++)
        {
            var appointment = new Appointment
            {
                PatientId = patients[i % patients.Count].Id,
                DoctorId = new[] { doctorArjun, doctorNeha, doctorVikram, doctorAnita }[i % 4].Id,
                AppointmentDate = now.Date.AddDays(i - 6),
                StartTime = new TimeSpan(9 + (i % 8), (i % 2) * 30, 0),
                Status = statuses[i],
                Notes = "Demo appointment.",
                CreatedAt = now.AddDays(-7 + i),
                UpdatedAt = now
            };
            appointments.Add(appointment);
        }
        _db.Appointments.AddRange(appointments);
        await _db.SaveChangesAsync();

        // Medical records: one per completed appointment
        var completedAppointments = appointments.Where(a => a.Status == EntityStatus.AppointmentCompleted).ToList();
        var records = new List<MedicalRecord>();
        for (var i = 0; i < 6; i++)
        {
            var appointment = completedAppointments[i];
            records.Add(new MedicalRecord
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Symptoms = "Fever and fatigue.",
                Diagnosis = "Viral infection",
                BloodPressure = "120/80",
                Temperature = "38.2 C",
                Pulse = "88",
                Weight = "70 kg",
                TreatmentPlan = "Rest and hydration.",
                Observations = "Stable.",
                FollowUpRecommendations = "Review in 5 days.",
                Status = i % 2 == 0 ? EntityStatus.RecordFinal : EntityStatus.RecordDraft,
                CreatedAt = now.AddDays(-5 + i),
                UpdatedAt = now
            });
        }
        _db.MedicalRecords.AddRange(records);
        await _db.SaveChangesAsync();

        // Prescriptions
        var prescriptions = new List<Prescription>();
        for (var i = 0; i < 5; i++)
        {
            var record = records[i % records.Count];
            var prescription = new Prescription
            {
                PatientId = record.PatientId,
                DoctorId = record.DoctorId,
                MedicalRecordId = record.Id,
                Status = EntityStatus.PrescriptionActive,
                Notes = "Take after meals.",
                CreatedAt = now.AddDays(-4 + i),
                UpdatedAt = now
            };
            prescription.Items.Add(new PrescriptionItem
            {
                MedicineId = medicines[i % medicines.Count].Id,
                Dosage = "1 tablet",
                Frequency = "Twice a day",
                DurationDays = 5,
                Instructions = "After food",
                CreatedAt = now,
                UpdatedAt = now
            });
            prescriptions.Add(prescription);
        }
        _db.Prescriptions.AddRange(prescriptions);
        await _db.SaveChangesAsync();

        // Lab requests
        var labRequests = new List<LabRequest>();
        for (var i = 0; i < 6; i++)
        {
            var appointment = appointments[i % appointments.Count];
            labRequests.Add(new LabRequest
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentId = appointment.Id,
                LaboratoryTestId = labTests[i % labTests.Length].Id,
                Status = i < 3 ? EntityStatus.LabCompleted : EntityStatus.LabRequested,
                ResultValue = i < 3 ? "Within normal range" : null,
                Remarks = i < 3 ? "Normal" : null,
                ResultEnteredAt = i < 3 ? now.AddDays(-1) : null,
                CreatedAt = now.AddDays(-3 + i),
                UpdatedAt = now
            });
        }
        _db.LabRequests.AddRange(labRequests);
        await _db.SaveChangesAsync();

        // Invoices and payments
        var invoices = new List<Invoice>();
        for (var i = 0; i < 8; i++)
        {
            var patient = patients[i % patients.Count];
            var doctor = new[] { doctorArjun, doctorNeha, doctorVikram, doctorAnita }[i % 4];
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{now:yyyyMMdd}-{i + 1:D4}",
                PatientId = patient.Id,
                DiscountAmount = 0m,
                TaxRate = 0m,
                CreatedAt = now.AddDays(-2 + (i % 3)),
                UpdatedAt = now
            };
            invoice.Items.Add(new InvoiceItem
            {
                Description = $"Consultation fee - Dr. {doctor.FullName}",
                Amount = doctor.ConsultationFee,
                CreatedAt = now,
                UpdatedAt = now
            });
            invoice.Total = doctor.ConsultationFee;
            invoice.PaidAmount = i < 4 ? doctor.ConsultationFee : 0m;
            invoice.Status = i < 4 ? EntityStatus.InvoicePaid : EntityStatus.InvoiceUnpaid;
            invoices.Add(invoice);
        }
        _db.Invoices.AddRange(invoices);
        await _db.SaveChangesAsync();

        for (var i = 0; i < 8; i++)
        {
            var invoice = invoices[i % invoices.Count];
            _db.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                Amount = i < 4 ? invoice.Total : invoice.Total / 2,
                Method = i % 2 == 0 ? "Cash" : "UPI",
                PaidAt = now.AddDays(-1),
                Status = EntityStatus.PaymentRecorded,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await _db.SaveChangesAsync();

        // Hospital name setting
        if (!await _db.SystemSettings.AnyAsync(s => s.Key == SystemSetting.HospitalNameKey))
        {
            _db.SystemSettings.Add(new SystemSetting
            {
                Key = SystemSetting.HospitalNameKey,
                Value = SystemSetting.DefaultHospitalName,
                CreatedAt = now,
                UpdatedAt = now
            });
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("Demo data seeded.");
    }

    public async Task EnsureRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await _roles.RoleExistsAsync(role))
            {
                await _roles.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string password, string fullName, string role)
    {
        var existing = await _users.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the demo user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        await _users.AddToRoleAsync(user, role);
        return user;
    }
}
