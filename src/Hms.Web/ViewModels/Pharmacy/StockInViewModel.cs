using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Pharmacy;

/// <summary>The fields of the stock-in form (Requirement 9.3).</summary>
public class StockInViewModel
{
    public int MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    [Display(Name = "Batch number")]
    public string BatchNumber { get; set; } = string.Empty;

    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Display(Name = "Expiry date")]
    public DateTime? ExpiryDate { get; set; }
}
