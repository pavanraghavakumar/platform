using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Lab;

/// <summary>The create form of the laboratory test catalog (Requirement 8.1).</summary>
public class LabTestFormViewModel
{
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Category")]
    public string? Category { get; set; }

    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Display(Name = "Normal range")]
    public string? NormalRange { get; set; }
}
