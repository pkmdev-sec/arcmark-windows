using System.Text.Json.Serialization;

namespace Arcmark.Models;

public class Folder
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("children")]
    public List<Node> Children { get; set; } = new();

    [JsonPropertyName("isExpanded")]
    public bool IsExpanded { get; set; }
}
