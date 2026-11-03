using Hms.Web.Controllers;
using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Hms.Web.Services;
using Hms.Web.ViewModels.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hms.Tests.Admin;

public class AuditTrailTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;
    private readonly HmsDbContext _db;

    public AuditTrailTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"hms-audit-{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        var options = new DbContextOptionsBuilder<HmsDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new HmsDbContext(options);
        _db.Database.Migrate();
    }

    [Fact]
    public async Task RecordAsync_saves_every_required_value_for_a_change()
    {
        var service = new AuditService(_db);

        await service.RecordAsync("MedicalRecord", 42, "Diagnosis", "Cold", "Flu", "doctor@hms.local");

        var entry = await _db.AuditEntries.SingleAsync(a => a.RecordType == "MedicalRecord" && a.RecordId == 42);
        Assert.Equal("Diagnosis", entry.FieldName);
        Assert.Equal("Cold", entry.OldValue);
        Assert.Equal("Flu", entry.NewValue);
        Assert.Equal("doctor@hms.local", entry.UserName);
        Assert.NotEqual(default, entry.ChangedAt);
    }

    [Fact]
    public async Task RecordAsync_saves_nothing_when_the_value_did_not_change()
    {
        var service = new AuditService(_db);

        await service.RecordAsync("Prescription", 73, "Status", "Active", "Active", "doctor@hms.local");

        Assert.False(await _db.AuditEntries.AnyAsync(a => a.RecordType == "Prescription" && a.RecordId == 73));
    }

    [Fact]
    public async Task Refunding_a_payment_audits_its_status_when_the_invoice_status_is_unchanged()
    {
        var patient = new Patient
        {
            PatientCode = "P-AUDIT-1",
            FullName = "Audit Patient",
            Phone = "9000000000"
        };
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-AUDIT-1",
            Patient = patient,
            Total = 200m,
            PaidAmount = 100m,
            Status = EntityStatus.InvoicePartiallyPaid
        };
        var payment = new Payment
        {
            Invoice = invoice,
            Amount = 25m,
            Method = "Cash",
            Status = EntityStatus.PaymentRecorded
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var service = new BillingService(
            _db,
            new AuditService(_db),
            new NotificationService(_db));

        Assert.True(await service.RefundAsync(payment.Id, "reception@hms.local"));

        var entry = await _db.AuditEntries.SingleAsync(a =>
            a.RecordType == "Payment" && a.RecordId == payment.Id);
        Assert.Equal("Status", entry.FieldName);
        Assert.Equal(EntityStatus.PaymentRecorded, entry.OldValue);
        Assert.Equal(EntityStatus.PaymentRefunded, entry.NewValue);
        Assert.Equal("reception@hms.local", entry.UserName);
        Assert.Equal(EntityStatus.InvoicePartiallyPaid, invoice.Status);
    }

    [Fact]
    public async Task Index_lists_newest_first_twenty_per_page()
    {
        var start = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        _db.AuditEntries.AddRange(Enumerable.Range(1, 25).Select(number => Entry(
            "Invoice", number, start.AddMinutes(number))));
        await _db.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await new AuditController(_db).Index(null, null, null, 1));
        var model = Assert.IsType<AuditListViewModel>(result.Model);

        Assert.Equal(20, model.Entries.Count);
        Assert.Equal(25, model.TotalCount);
        Assert.Equal(2, model.TotalPages);
        Assert.Equal(25, model.Entries.First().RecordId);
        Assert.Equal(6, model.Entries.Last().RecordId);
    }

    [Fact]
    public async Task Index_clamps_a_page_beyond_the_end_to_the_last_page()
    {
        var start = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        _db.AuditEntries.AddRange(Enumerable.Range(1, 25).Select(number => Entry(
            "Invoice", number, start.AddMinutes(number))));
        await _db.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await new AuditController(_db).Index(null, null, null, 999));
        var model = Assert.IsType<AuditListViewModel>(result.Model);

        Assert.Equal(2, model.Page);
        Assert.Equal(2, model.TotalPages);
        Assert.Equal(5, model.Entries.Count);
        Assert.Equal(5, model.Entries.First().RecordId);
        Assert.Equal(1, model.Entries.Last().RecordId);
    }

    [Fact]
    public async Task Index_filters_by_record_type_and_inclusive_date_range()
    {
        _db.AuditEntries.AddRange(
            Entry("Invoice", 101, new DateTime(2026, 6, 9, 23, 59, 0, DateTimeKind.Utc)),
            Entry("Invoice", 102, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)),
            Entry("Invoice", 103, new DateTime(2026, 6, 12, 23, 59, 0, DateTimeKind.Utc)),
            Entry("Invoice", 104, new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc)),
            Entry("Payment", 105, new DateTime(2026, 6, 11, 12, 0, 0, DateTimeKind.Utc)));
        await _db.SaveChangesAsync();

        var from = new DateTime(2026, 6, 10);
        var to = new DateTime(2026, 6, 12);
        var result = Assert.IsType<ViewResult>(await new AuditController(_db).Index("Invoice", from, to, 1));
        var model = Assert.IsType<AuditListViewModel>(result.Model);

        Assert.Equal(new[] { 103, 102 }, model.Entries.Select(entry => entry.RecordId));
        Assert.Equal("Invoice", model.RecordType);
        Assert.Equal(from, model.From);
        Assert.Equal(to, model.To);
    }

    [Fact]
    public void Controller_requires_the_Admin_role()
    {
        var attribute = Assert.Single(typeof(AuditController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(Roles.Admin, attribute.Roles);
    }

    private static AuditEntry Entry(string recordType, int recordId, DateTime changedAt)
    {
        return new AuditEntry
        {
            RecordType = recordType,
            RecordId = recordId,
            FieldName = "Status",
            OldValue = "Old",
            NewValue = "New",
            UserName = "audit-test@hms.local",
            ChangedAt = changedAt,
            CreatedAt = changedAt,
            UpdatedAt = changedAt
        };
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
