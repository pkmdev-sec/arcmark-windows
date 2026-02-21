using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Arcmark.Models;

/// <summary>
/// Color identifier for a workspace. The JSON string values are lowercase to match
/// the macOS Swift enum's default raw-value encoding (e.g. "sky", "blush").
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WorkspaceColorId>))]
public enum WorkspaceColorId
{
    [JsonStringEnumMemberName("blush")]     Blush,
    [JsonStringEnumMemberName("apricot")]   Apricot,
    [JsonStringEnumMemberName("butter")]    Butter,
    [JsonStringEnumMemberName("leaf")]      Leaf,
    [JsonStringEnumMemberName("mint")]      Mint,
    [JsonStringEnumMemberName("sky")]       Sky,
    [JsonStringEnumMemberName("periwinkle")]Periwinkle,
    [JsonStringEnumMemberName("lavender")] Lavender,
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
    /// Background-tinted variant of the colour (~92 % lightness / 0.92 alpha blend).
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
