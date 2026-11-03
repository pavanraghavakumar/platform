using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Web.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AspNetUsers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AuditEntries",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RecordType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                RecordId = table.Column<int>(type: "INTEGER", nullable: false),
                FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                OldValue = table.Column<string>(type: "TEXT", nullable: true),
                NewValue = table.Column<string>(type: "TEXT", nullable: true),
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AuditEntries", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Departments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Departments", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LaboratoryTests",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Category = table.Column<string>(type: "TEXT", nullable: true),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                NormalRange = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LaboratoryTests", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MedicineCategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MedicineCategories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                Phone = table.Column<string>(type: "TEXT", nullable: true),
                Email = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Suppliers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SystemSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SystemSettings", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<string>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                UserId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                RoleId = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Doctors",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                Specialization = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                Qualifications = table.Column<string>(type: "TEXT", nullable: true),
                YearsOfExperience = table.Column<int>(type: "INTEGER", nullable: false),
                ConsultationFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Doctors", x => x.Id);
                table.ForeignKey(
                    name: "FK_Doctors_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Doctors_Departments_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "Departments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PatientCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: false),
                Gender = table.Column<string>(type: "TEXT", nullable: false),
                Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Address = table.Column<string>(type: "TEXT", nullable: true),
                EmergencyContactName = table.Column<string>(type: "TEXT", nullable: true),
                EmergencyContactPhone = table.Column<string>(type: "TEXT", nullable: true),
                MedicalHistory = table.Column<string>(type: "TEXT", nullable: true),
                Allergies = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Patients", x => x.Id);
                table.ForeignKey(
                    name: "FK_Patients_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Staff",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Designation = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                DepartmentId = table.Column<int>(type: "INTEGER", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                UserId = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Staff", x => x.Id);
                table.ForeignKey(
                    name: "FK_Staff_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Staff_Departments_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "Departments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Medicines",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ReorderLevel = table.Column<int>(type: "INTEGER", nullable: false),
                StockQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Medicines", x => x.Id);
                table.ForeignKey(
                    name: "FK_Medicines_MedicineCategories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "MedicineCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Medicines_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "DoctorSchedules",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DoctorId = table.Column<int>(type: "INTEGER", nullable: false),
                DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                IsWorking = table.Column<bool>(type: "INTEGER", nullable: false),
                StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                EndTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DoctorSchedules", x => x.Id);
                table.ForeignKey(
                    name: "FK_DoctorSchedules_Doctors_DoctorId",
                    column: x => x.DoctorId,
                    principalTable: "Doctors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                DoctorId = table.Column<int>(type: "INTEGER", nullable: false),
                AppointmentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Appointments_Doctors_DoctorId",
                    column: x => x.DoctorId,
                    principalTable: "Doctors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Appointments_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MedicineBatches",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                MedicineId = table.Column<int>(type: "INTEGER", nullable: false),
                BatchNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MedicineBatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_MedicineBatches_Medicines_MedicineId",
                    column: x => x.MedicineId,
                    principalTable: "Medicines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                AppointmentId = table.Column<int>(type: "INTEGER", nullable: true),
                DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_Invoices_Appointments_AppointmentId",
                    column: x => x.AppointmentId,
                    principalTable: "Appointments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Invoices_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LabRequests",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                DoctorId = table.Column<int>(type: "INTEGER", nullable: false),
                AppointmentId = table.Column<int>(type: "INTEGER", nullable: true),
                LaboratoryTestId = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                ResultValue = table.Column<string>(type: "TEXT", nullable: true),
                Remarks = table.Column<string>(type: "TEXT", nullable: true),
                ResultEnteredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LabRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_LabRequests_Appointments_AppointmentId",
                    column: x => x.AppointmentId,
                    principalTable: "Appointments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_LabRequests_Doctors_DoctorId",
                    column: x => x.DoctorId,
                    principalTable: "Doctors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LabRequests_LaboratoryTests_LaboratoryTestId",
                    column: x => x.LaboratoryTestId,
                    principalTable: "LaboratoryTests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LabRequests_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MedicalRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AppointmentId = table.Column<int>(type: "INTEGER", nullable: false),
                PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                DoctorId = table.Column<int>(type: "INTEGER", nullable: false),
                Symptoms = table.Column<string>(type: "TEXT", nullable: true),
                Diagnosis = table.Column<string>(type: "TEXT", nullable: true),
                BloodPressure = table.Column<string>(type: "TEXT", nullable: true),
                Temperature = table.Column<string>(type: "TEXT", nullable: true),
                Pulse = table.Column<string>(type: "TEXT", nullable: true),
                Weight = table.Column<string>(type: "TEXT", nullable: true),
                TreatmentPlan = table.Column<string>(type: "TEXT", nullable: true),
                Observations = table.Column<string>(type: "TEXT", nullable: true),
                FollowUpRecommendations = table.Column<string>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MedicalRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_MedicalRecords_Appointments_AppointmentId",
                    column: x => x.AppointmentId,
                    principalTable: "Appointments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MedicalRecords_Doctors_DoctorId",
                    column: x => x.DoctorId,
                    principalTable: "Doctors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MedicalRecords_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "InvoiceItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_InvoiceItems_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Method = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                PaidAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Prescriptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                DoctorId = table.Column<int>(type: "INTEGER", nullable: false),
                MedicalRecordId = table.Column<int>(type: "INTEGER", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Prescriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Prescriptions_Doctors_DoctorId",
                    column: x => x.DoctorId,
                    principalTable: "Doctors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Prescriptions_MedicalRecords_MedicalRecordId",
                    column: x => x.MedicalRecordId,
                    principalTable: "MedicalRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Prescriptions_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PrescriptionItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PrescriptionId = table.Column<int>(type: "INTEGER", nullable: false),
                MedicineId = table.Column<int>(type: "INTEGER", nullable: false),
                Dosage = table.Column<string>(type: "TEXT", nullable: false),
                Frequency = table.Column<string>(type: "TEXT", nullable: false),
                DurationDays = table.Column<int>(type: "INTEGER", nullable: false),
                Instructions = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_PrescriptionItems_Medicines_MedicineId",
                    column: x => x.MedicineId,
                    principalTable: "Medicines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                    column: x => x.PrescriptionId,
                    principalTable: "Prescriptions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "EmailIndex", table: "AspNetUsers", column: "NormalizedEmail");
        migrationBuilder.CreateIndex(name: "UserNameIndex", table: "AspNetUsers", column: "NormalizedUserName", unique: true);
        migrationBuilder.CreateIndex(name: "IX_AspNetRoleClaims_RoleId", table: "AspNetRoleClaims", column: "RoleId");
        migrationBuilder.CreateIndex(name: "RoleNameIndex", table: "AspNetRoles", column: "NormalizedName", unique: true);
        migrationBuilder.CreateIndex(name: "IX_AspNetUserClaims_UserId", table: "AspNetUserClaims", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserLogins_UserId", table: "AspNetUserLogins", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserRoles_RoleId", table: "AspNetUserRoles", column: "RoleId");
        migrationBuilder.CreateIndex(name: "IX_AuditEntries_RecordType_ChangedAt", table: "AuditEntries", columns: new[] { "RecordType", "ChangedAt" });
        migrationBuilder.CreateIndex(name: "IX_Departments_Name", table: "Departments", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Doctors_DepartmentId", table: "Doctors", column: "DepartmentId");
        migrationBuilder.CreateIndex(name: "IX_Doctors_UserId", table: "Doctors", column: "UserId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_DoctorSchedules_DoctorId_DayOfWeek", table: "DoctorSchedules", columns: new[] { "DoctorId", "DayOfWeek" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_Appointments_DoctorId_AppointmentDate_StartTime", table: "Appointments", columns: new[] { "DoctorId", "AppointmentDate", "StartTime" });
        migrationBuilder.CreateIndex(name: "IX_Appointments_PatientId", table: "Appointments", column: "PatientId");
        migrationBuilder.CreateIndex(name: "IX_InvoiceItems_InvoiceId", table: "InvoiceItems", column: "InvoiceId");
        migrationBuilder.CreateIndex(name: "IX_Invoices_AppointmentId", table: "Invoices", column: "AppointmentId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Invoices_InvoiceNumber", table: "Invoices", column: "InvoiceNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Invoices_PatientId", table: "Invoices", column: "PatientId");
        migrationBuilder.CreateIndex(name: "IX_LabRequests_AppointmentId", table: "LabRequests", column: "AppointmentId");
        migrationBuilder.CreateIndex(name: "IX_LabRequests_DoctorId", table: "LabRequests", column: "DoctorId");
        migrationBuilder.CreateIndex(name: "IX_LabRequests_LaboratoryTestId", table: "LabRequests", column: "LaboratoryTestId");
        migrationBuilder.CreateIndex(name: "IX_LabRequests_PatientId", table: "LabRequests", column: "PatientId");
        migrationBuilder.CreateIndex(name: "IX_MedicalRecords_AppointmentId", table: "MedicalRecords", column: "AppointmentId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_MedicalRecords_DoctorId", table: "MedicalRecords", column: "DoctorId");
        migrationBuilder.CreateIndex(name: "IX_MedicalRecords_PatientId", table: "MedicalRecords", column: "PatientId");
        migrationBuilder.CreateIndex(name: "IX_MedicineBatches_MedicineId", table: "MedicineBatches", column: "MedicineId");
        migrationBuilder.CreateIndex(name: "IX_MedicineCategories_Name", table: "MedicineCategories", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Medicines_CategoryId", table: "Medicines", column: "CategoryId");
        migrationBuilder.CreateIndex(name: "IX_Medicines_SupplierId", table: "Medicines", column: "SupplierId");
        migrationBuilder.CreateIndex(name: "IX_Notifications_UserId_IsRead", table: "Notifications", columns: new[] { "UserId", "IsRead" });
        migrationBuilder.CreateIndex(name: "IX_Patients_PatientCode", table: "Patients", column: "PatientCode", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Patients_UserId", table: "Patients", column: "UserId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Payments_InvoiceId", table: "Payments", column: "InvoiceId");
        migrationBuilder.CreateIndex(name: "IX_PrescriptionItems_MedicineId", table: "PrescriptionItems", column: "MedicineId");
        migrationBuilder.CreateIndex(name: "IX_PrescriptionItems_PrescriptionId", table: "PrescriptionItems", column: "PrescriptionId");
        migrationBuilder.CreateIndex(name: "IX_Prescriptions_DoctorId", table: "Prescriptions", column: "DoctorId");
        migrationBuilder.CreateIndex(name: "IX_Prescriptions_MedicalRecordId", table: "Prescriptions", column: "MedicalRecordId");
        migrationBuilder.CreateIndex(name: "IX_Prescriptions_PatientId", table: "Prescriptions", column: "PatientId");
        migrationBuilder.CreateIndex(name: "IX_Staff_DepartmentId", table: "Staff", column: "DepartmentId");
        migrationBuilder.CreateIndex(name: "IX_Staff_UserId", table: "Staff", column: "UserId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Suppliers_Name", table: "Suppliers", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_SystemSettings_Key", table: "SystemSettings", column: "Key", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AspNetRoleClaims");
        migrationBuilder.DropTable(name: "AspNetUserClaims");
        migrationBuilder.DropTable(name: "AspNetUserLogins");
        migrationBuilder.DropTable(name: "AspNetUserRoles");
        migrationBuilder.DropTable(name: "AspNetUserTokens");
        migrationBuilder.DropTable(name: "AuditEntries");
        migrationBuilder.DropTable(name: "DoctorSchedules");
        migrationBuilder.DropTable(name: "InvoiceItems");
        migrationBuilder.DropTable(name: "LabRequests");
        migrationBuilder.DropTable(name: "MedicineBatches");
        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "Payments");
        migrationBuilder.DropTable(name: "PrescriptionItems");
        migrationBuilder.DropTable(name: "Prescriptions");
        migrationBuilder.DropTable(name: "MedicalRecords");
        migrationBuilder.DropTable(name: "Invoices");
        migrationBuilder.DropTable(name: "Appointments");
        migrationBuilder.DropTable(name: "LaboratoryTests");
        migrationBuilder.DropTable(name: "Medicines");
        migrationBuilder.DropTable(name: "MedicineCategories");
        migrationBuilder.DropTable(name: "Suppliers");
        migrationBuilder.DropTable(name: "Staff");
        migrationBuilder.DropTable(name: "Patients");
        migrationBuilder.DropTable(name: "Doctors");
        migrationBuilder.DropTable(name: "Departments");
        migrationBuilder.DropTable(name: "SystemSettings");
        migrationBuilder.DropTable(name: "AspNetRoles");
        migrationBuilder.DropTable(name: "AspNetUsers");
    }
}
