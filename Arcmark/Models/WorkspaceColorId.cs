using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Arcmark.Models;

/// <summary>
/// Color identifier for a workspace. Serializes as lowercase strings to match
/// the macOS Swift enum's raw-value encoding (e.g. "sky", "blush").
/// Uses a custom converter since JsonStringEnumMemberName is .NET 9+ only.
/// </summary>
[JsonConverter(typeof(WorkspaceColorIdConverter))]
public enum WorkspaceColorId
{
    Blush,
    Apricot,
    Butter,
    Leaf,
    Mint,
    Sky,
    Periwinkle,
    Lavender,
}

/// <summary>
/// Custom JSON converter that serializes WorkspaceColorId as lowercase strings
/// for cross-platform compatibility with the macOS version.
/// </summary>
public class WorkspaceColorIdConverter : JsonConverter<WorkspaceColorId>
{
    public override WorkspaceColorId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.ToLowerInvariant() switch
        {
            "blush" => WorkspaceColorId.Blush,
            "apricot" => WorkspaceColorId.Apricot,
            "butter" => WorkspaceColorId.Butter,
            "leaf" => WorkspaceColorId.Leaf,
            "mint" => WorkspaceColorId.Mint,
            "sky" => WorkspaceColorId.Sky,
            "periwinkle" => WorkspaceColorId.Periwinkle,
            "lavender" => WorkspaceColorId.Lavender,
            _ => WorkspaceColorId.Sky, // Default fallback
        };
    }

    public override void Write(Utf8JsonWriter writer, WorkspaceColorId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            WorkspaceColorId.Blush => "blush",
            WorkspaceColorId.Apricot => "apricot",
            WorkspaceColorId.Butter => "butter",
            WorkspaceColorId.Leaf => "leaf",
            WorkspaceColorId.Mint => "mint",
            WorkspaceColorId.Sky => "sky",
            WorkspaceColorId.Periwinkle => "periwinkle",
            WorkspaceColorId.Lavender => "lavender",
            _ => "sky",
        });
    }
}

public static class WorkspaceColorIdExtensions
{
    private static readonly Random _rng = new();

    /// <summary>Returns the fully-opaque accent colour for each workspace colour.</summary>
    public static Color GetColor(this WorkspaceColorId colorId) => colorId switch
    {
        WorkspaceColorId.Blush      => Color.FromRgb(0xFF, 0xB3, 0xBA), // #FFB3BA
        WorkspaceColorId.Apricot    => Color.FromRgb(0xFF, 0xCC, 0xA1), // #FFCCA1
        WorkspaceColorId.Butter     => Color.FromRgb(0xFF, 0xF0, 0x9D), // #FFF09D
        WorkspaceColorId.Leaf       => Color.FromRgb(0xB5, 0xE5, 0x9D), // #B5E59D
        WorkspaceColorId.Mint       => Color.FromRgb(0x9D, 0xE8, 0xD0), // #9DE8D0
        WorkspaceColorId.Sky        => Color.FromRgb(0x9D, 0xD0, 0xFF), // #9DD0FF
        WorkspaceColorId.Periwinkle => Color.FromRgb(0xB3, 0xB9, 0xFF), // #B3B9FF
        WorkspaceColorId.Lavender   => Color.FromRgb(0xD5, 0xB3, 0xFF), // #D5B3FF
        _                           => Color.FromRgb(0x9D, 0xD0, 0xFF),
    };

    /// <summary>
    /// Background-tinted variant of the colour (~92% lightness / 0.92 alpha blend).
    /// Computed as a simple blend toward white.
    /// </summary>
    public static Color GetBackgroundColor(this WorkspaceColorId colorId)
    {
        var c = colorId.GetColor();
        const double alpha = 0.92;
        byte Blend(byte channel) => (byte)(channel * alpha + 255 * (1 - alpha));
        return Color.FromRgb(Blend(c.R), Blend(c.G), Blend(c.B));
    }

    /// <summary>The default workspace colour.</summary>
    public static WorkspaceColorId GetDefaultColor() => WorkspaceColorId.Sky;

    /// <summary>Returns a random workspace colour.</summary>
    public static WorkspaceColorId GetRandomColor()
    {
        var values = Enum.GetValues<WorkspaceColorId>();
        return values[_rng.Next(values.Length)];
    }

    /// <summary>Human-readable display name for the colour.</summary>
    public static string GetName(this WorkspaceColorId colorId) => colorId switch
    {
        WorkspaceColorId.Blush      => "Blush",
        WorkspaceColorId.Apricot    => "Apricot",
        WorkspaceColorId.Butter     => "Butter",
        WorkspaceColorId.Leaf       => "Leaf",
        WorkspaceColorId.Mint       => "Mint",
        WorkspaceColorId.Sky        => "Sky",
        WorkspaceColorId.Periwinkle => "Periwinkle",
        WorkspaceColorId.Lavender   => "Lavender",
        _                           => colorId.ToString(),
    };
}
