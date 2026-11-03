using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Pharmacy;

/// <summary>The fields of the "New medicine" form (Requirement 9.1).</summary>
public class MedicineCreateViewModel
{
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Unit price")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Reorder level")]
    public int ReorderLevel { get; set; }

    [Display(Name = "Stock quantity")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
    public int StockQuantity { get; set; }
}
