namespace Hms.Web.Models;

/// <summary>The model of the shared <c>_KpiCard</c> partial.</summary>
public class KpiCardModel
{
    public KpiCardModel()
    {
    }

    public KpiCardModel(string icon, string value, string label)
    {
        Icon = icon;
        Value = value;
        Label = label;
    }

    public string Icon { get; set; } = "bi-graph-up";

    public string Value { get; set; } = "—";

    public string Label { get; set; } = string.Empty;
}
