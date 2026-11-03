using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Pharmacy;

/// <summary>The fields of the stock-out form (Requirements 9.4 and 9.5).</summary>
public class StockOutViewModel
{
    public int MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Display(Name = "Reason")]
    public string? Reason { get; set; }
}
