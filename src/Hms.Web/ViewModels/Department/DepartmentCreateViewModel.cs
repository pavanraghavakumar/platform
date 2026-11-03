using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Department;

/// <summary>The New department form: only a name is entered; the status is set to Active on save.</summary>
public class DepartmentCreateViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150, ErrorMessage = "Name must be 150 characters or fewer.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;
}
