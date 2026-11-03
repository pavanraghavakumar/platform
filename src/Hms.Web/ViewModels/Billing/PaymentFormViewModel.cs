using System.ComponentModel.DataAnnotations;

namespace Hms.Web.ViewModels.Billing;

/// <summary>The payment form: amount, method and date.</summary>
public class PaymentFormViewModel
{
    public int InvoiceId { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal? Amount { get; set; }

    public string Method { get; set; } = "Cash";

    public DateTime PaidAt { get; set; } = DateTime.Today;

    public static readonly string[] Methods = { "Cash", "Card", "UPI", "Bank Transfer" };
}
