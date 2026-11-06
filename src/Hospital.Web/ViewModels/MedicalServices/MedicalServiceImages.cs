namespace Hospital.Web.ViewModels.MedicalServices;

public static class MedicalServiceImages
{
    private static readonly int[] ResponsiveWidths = { 480, 768, 1200 };

    // A local, CSP-compatible illustration keeps every service card visual even before licensed media is uploaded.
    public const string Default = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='960' height='640' viewBox='0 0 960 640'%3E%3Crect width='960' height='640' fill='%23e6f6f7'/%3E%3Ccircle cx='480' cy='320' r='190' fill='%23ffffff'/%3E%3Cpath d='M430 190h100v80h80v100h-80v80H430v-80h-80V270h80z' fill='%23087f8c'/%3E%3Cpath d='M250 510c105-95 355-95 460 0' fill='none' stroke='%230b2d4d' stroke-width='28' stroke-linecap='round'/%3E%3C/svg%3E";

    public static string Resolve(string? imagePath) =>
        !IsLocalImagePath(imagePath)
            ? Default
            : imagePath!;

    public static string? SourceSet(string? imagePath)
    {
        var source = Resolve(imagePath);
        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.Join(", ", ResponsiveWidths.Select(width =>
            $"/media/responsive/{width}.webp?source={Uri.EscapeDataString(source)} {width}w"));
    }

    public static bool IsSupportedWidth(int width) => ResponsiveWidths.Contains(width);

    public static bool IsLocalImagePath(string? imagePath) =>
        !string.IsNullOrWhiteSpace(imagePath)
        && imagePath.StartsWith("/", StringComparison.Ordinal)
        && !imagePath.StartsWith("//", StringComparison.Ordinal)
        && !imagePath.Contains('\\', StringComparison.Ordinal)
        && !imagePath.Contains('?', StringComparison.Ordinal)
        && !imagePath.Contains('#', StringComparison.Ordinal);
}
