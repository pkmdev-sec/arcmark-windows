using System.Text.Json.Serialization;

namespace Arcmark.Models;

public class Link
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("faviconPath")]
    public string? FaviconPath { get; set; }
}
