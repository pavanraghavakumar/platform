using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.PublicContent.Departments;
using Hospital.Infrastructure.PublicContent.HealthPackages;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Data;

public sealed class HospitalDbContext : IdentityDbContext<ApplicationUser>
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options)
        : base(options)
    {
    }

    public DbSet<DepartmentRecord> Departments => Set<DepartmentRecord>();

    public DbSet<DepartmentDoctorRecord> DepartmentDoctors => Set<DepartmentDoctorRecord>();

    public DbSet<HealthPackageRecord> HealthPackages => Set<HealthPackageRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DepartmentRecord>(department =>
        {
            department.ToTable("PublicDepartments");
            department.HasKey(item => item.Id);
            department.Property(item => item.Name).HasMaxLength(160).IsRequired();
            department.Property(item => item.Slug).HasMaxLength(180).IsRequired();
            department.Property(item => item.ImagePath).HasMaxLength(500).IsRequired();
            department.Property(item => item.Description).HasMaxLength(500).IsRequired();
            department.Property(item => item.Details).HasMaxLength(4000).IsRequired();
            department.Property(item => item.PublicationState).HasConversion<string>().HasMaxLength(24);
            department.HasIndex(item => item.Slug).IsUnique();
            department.HasIndex(item => item.PublicationState);
        });

        builder.Entity<DepartmentDoctorRecord>(doctor =>
        {
            doctor.ToTable("PublicDepartmentDoctors");
            doctor.HasKey(item => item.Id);
            doctor.Property(item => item.Name).HasMaxLength(160).IsRequired();
            doctor.Property(item => item.Slug).HasMaxLength(180).IsRequired();
            doctor.Property(item => item.PhotoPath).HasMaxLength(500).IsRequired();
            doctor.Property(item => item.Qualifications).HasMaxLength(300).IsRequired();
            doctor.Property(item => item.Specialization).HasMaxLength(200).IsRequired();
            doctor.Property(item => item.PublicationState).HasConversion<string>().HasMaxLength(24);
            doctor.HasIndex(item => item.Slug).IsUnique();
            doctor.HasIndex(item => new { item.DepartmentId, item.PublicationState });
            doctor.HasOne(item => item.Department)
                .WithMany(item => item.Doctors)
                .HasForeignKey(item => item.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HealthPackageRecord>(package =>
        {
            package.ToTable("HealthPackages");
            package.HasKey(item => item.Id);
            package.Property(item => item.Slug).HasMaxLength(180).IsRequired();
            package.Property(item => item.Name).HasMaxLength(160).IsRequired();
            package.Property(item => item.Description).HasMaxLength(1000).IsRequired();
            package.Property(item => item.Details).HasMaxLength(8000).IsRequired();
            package.Property(item => item.PublicationState).HasConversion<string>().HasMaxLength(24);
            package.HasIndex(item => item.Slug).IsUnique();
            package.HasIndex(item => item.PublicationState);
        });
    }
}
