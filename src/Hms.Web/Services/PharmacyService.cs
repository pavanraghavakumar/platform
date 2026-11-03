using Hms.Web.Data;
using Hms.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hms.Web.Services;

public interface IPharmacyService
{
    Task StockInAsync(int medicineId, string batchNumber, int quantity, DateTime expiryDate);

    Task StockOutAsync(int medicineId, int quantity, string? reason);

    Task<string> GetStatusAsync(int medicineId);
}

public class PharmacyService : IPharmacyService
{
    private readonly HmsDbContext _db;

    public PharmacyService(HmsDbContext db)
    {
        _db = db;
    }

    public async Task StockInAsync(int medicineId, string batchNumber, int quantity, DateTime expiryDate)
    {
        var medicine = await _db.Medicines.FindAsync(medicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        medicine.StockQuantity += quantity;
        medicine.UpdatedAt = DateTime.UtcNow;

        _db.MedicineBatches.Add(new MedicineBatch
        {
            MedicineId = medicineId,
            BatchNumber = batchNumber,
            Quantity = quantity,
            ExpiryDate = expiryDate.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await RefreshStatusAsync(medicine);
    }

    public async Task StockOutAsync(int medicineId, int quantity, string? reason)
    {
        var medicine = await _db.Medicines.FindAsync(medicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        if (quantity > medicine.StockQuantity)
        {
            throw new InvalidOperationException("Not enough stock available.");
        }

        medicine.StockQuantity -= quantity;
        medicine.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await RefreshStatusAsync(medicine);
    }

    public async Task<string> GetStatusAsync(int medicineId)
    {
        var medicine = await _db.Medicines.FindAsync(medicineId)
            ?? throw new InvalidOperationException("Medicine not found.");

        return await ComputeStatusAsync(medicine);
    }

    private async Task RefreshStatusAsync(Medicine medicine)
    {
        medicine.Status = await ComputeStatusAsync(medicine);
        await _db.SaveChangesAsync();
    }

    private async Task<string> ComputeStatusAsync(Medicine medicine)
    {
        var earliestExpiry = await _db.MedicineBatches
            .Where(b => b.MedicineId == medicine.Id)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => (DateTime?)b.ExpiryDate)
            .FirstOrDefaultAsync();

        if (earliestExpiry is not null && earliestExpiry.Value.Date < DateTime.UtcNow.Date)
        {
            return EntityStatus.MedicineExpired;
        }

        if (medicine.StockQuantity == 0)
        {
            return EntityStatus.MedicineOutOfStock;
        }

        if (medicine.StockQuantity <= medicine.ReorderLevel)
        {
            return EntityStatus.MedicineLowStock;
        }

        return EntityStatus.MedicineInStock;
    }
}
