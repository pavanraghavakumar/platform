using Hms.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Data;

public class HmsDbContext : IdentityDbContext<ApplicationUser>
{
    public HmsDbContext(DbContextOptions<HmsDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<MedicineCategory> MedicineCategories => Set<MedicineCategory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();
    public DbSet<LaboratoryTest> LaboratoryTests => Set<LaboratoryTest>();
    public DbSet<LabRequest> LabRequests => Set<LabRequest>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200);
        });

        builder.Entity<Patient>(e =>
        {
            e.Property(x => x.PatientCode).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.PatientCode).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.User)
                .WithOne(x => x.Patient)
                .HasForeignKey<Patient>(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Department>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20);
        });

        builder.Entity<Doctor>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Specialization).HasMaxLength(150);
            e.Property(x => x.ConsultationFee).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Department)
                .WithMany(d => d.Doctors)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User)
                .WithOne(x => x.Doctor)
                .HasForeignKey<Doctor>(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DoctorSchedule>(e =>
        {
            e.HasIndex(x => new { x.DoctorId, x.DayOfWeek }).IsUnique();
            e.HasOne(x => x.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Staff>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Role).HasMaxLength(50);
            e.Property(x => x.Designation).HasMaxLength(150);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Department)
                .WithMany(d => d.Staff)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.User)
                .WithOne(x => x.Staff)
                .HasForeignKey<Staff>(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Appointment>(e =>
        {
            e.Property(x => x.Status).HasMaxLength(30);
            e.HasIndex(x => new { x.DoctorId, x.AppointmentDate, x.StartTime });
            e.HasOne(x => x.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MedicalRecord>(e =>
        {
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasIndex(x => x.AppointmentId).IsUnique();
            e.HasOne(x => x.Appointment)
                .WithOne(a => a.MedicalRecord)
                .HasForeignKey<MedicalRecord>(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Prescription>(e =>
        {
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Patient)
                .WithMany(p => p.Prescriptions)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MedicalRecord)
                .WithMany(m => m.Prescriptions)
                .HasForeignKey(x => x.MedicalRecordId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PrescriptionItem>(e =>
        {
            e.HasOne(x => x.Prescription)
                .WithMany(p => p.Items)
                .HasForeignKey(x => x.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Medicine)
                .WithMany(m => m.PrescriptionItems)
                .HasForeignKey(x => x.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MedicineCategory>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Supplier>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Medicine>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Category)
                .WithMany(c => c.Medicines)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Supplier)
                .WithMany(s => s.Medicines)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MedicineBatch>(e =>
        {
            e.Property(x => x.BatchNumber).HasMaxLength(100);
            e.HasOne(x => x.Medicine)
                .WithMany(m => m.Batches)
                .HasForeignKey(x => x.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LaboratoryTest>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
        });

        builder.Entity<LabRequest>(e =>
        {
            e.Property(x => x.Status).HasMaxLength(30);
            e.HasOne(x => x.Patient)
                .WithMany(p => p.LabRequests)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Appointment)
                .WithMany(a => a.LabRequests)
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.LaboratoryTest)
                .WithMany(t => t.Requests)
                .HasForeignKey(x => x.LaboratoryTestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Invoice>(e =>
        {
            e.Property(x => x.InvoiceNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.HasIndex(x => x.AppointmentId).IsUnique();
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.TaxRate).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasMaxLength(30);
            e.HasOne(x => x.Patient)
                .WithMany(p => p.Invoices)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Appointment)
                .WithOne(a => a.Invoice)
                .HasForeignKey<Invoice>(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InvoiceItem>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Method).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(e =>
        {
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.UserId, x.IsRead });
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditEntry>(e =>
        {
            e.Property(x => x.RecordType).HasMaxLength(50).IsRequired();
            e.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
            e.Property(x => x.UserName).HasMaxLength(256);
            e.HasIndex(x => new { x.RecordType, x.ChangedAt });
        });

        builder.Entity<SystemSetting>(e =>
        {
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Value).HasMaxLength(500);
        });
    }
}
