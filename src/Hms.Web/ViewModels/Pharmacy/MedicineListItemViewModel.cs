namespace Hms.Web.ViewModels.Pharmacy;

/// <summary>One row of the medicine list and of the pharmacy inventory report (Requirements 9.6–9.9).</summary>
public class MedicineListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Supplier { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int ReorderLevel { get; set; }

    public int StockQuantity { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? EarliestExpiryDate { get; set; }
}
