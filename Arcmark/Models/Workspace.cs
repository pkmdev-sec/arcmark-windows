using System.Text.Json.Serialization;

namespace Arcmark.Models;

public class Workspace
{
    /// <summary>Maximum number of pinned links (4 columns × 3 rows).</summary>
    public const int MaxPinnedLinks = 12;

    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("colorId")]
    public WorkspaceColorId ColorId { get; set; } = WorkspaceColorId.Sky;

    [JsonPropertyName("items")]
    public List<Node> Items { get; set; } = new();

    /// <summary>
    /// Pinned links. Always serialized — even when empty — to stay compatible
    /// with the macOS schema which defaults to [].
    /// </summary>
    [JsonPropertyName("pinnedLinks")]
    public List<Link> PinnedLinks { get; set; } = new();
}
