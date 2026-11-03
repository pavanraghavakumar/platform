namespace Hms.Web.ViewModels.Pharmacy;

/// <summary>The medicine list with the inventory report counts (Requirements 9.6–9.9).</summary>
public class MedicineIndexViewModel
{
    public IReadOnlyList<MedicineListItemViewModel> Medicines { get; set; } = new List<MedicineListItemViewModel>();

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public int ExpiredCount { get; set; }
}
