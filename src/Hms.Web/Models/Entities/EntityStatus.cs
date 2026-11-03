namespace Hms.Web.Models.Entities;

/// <summary>Status strings shared by the entities, exactly as the requirements name them.</summary>
public static class EntityStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";

    public const string AppointmentPending = "Pending";
    public const string AppointmentConfirmed = "Confirmed";
    public const string AppointmentCheckedIn = "Checked In";
    public const string AppointmentInProgress = "In Progress";
    public const string AppointmentCompleted = "Completed";
    public const string AppointmentCancelled = "Cancelled";
    public const string AppointmentNoShow = "No Show";

    public const string RecordDraft = "Draft";
    public const string RecordFinal = "Final";

    public const string PrescriptionActive = "Active";
    public const string PrescriptionCompleted = "Completed";
    public const string PrescriptionCancelled = "Cancelled";

    public const string LabRequested = "Requested";
    public const string LabInProgress = "In Progress";
    public const string LabCompleted = "Completed";
    public const string LabCancelled = "Cancelled";

    public const string MedicineInStock = "In Stock";
    public const string MedicineLowStock = "Low Stock";
    public const string MedicineOutOfStock = "Out of Stock";
    public const string MedicineExpired = "Expired";

    public const string InvoiceUnpaid = "Unpaid";
    public const string InvoicePartiallyPaid = "Partially Paid";
    public const string InvoicePaid = "Paid";
    public const string InvoiceCancelled = "Cancelled";

    public const string PaymentRecorded = "Recorded";
    public const string PaymentRefunded = "Refunded";
}
