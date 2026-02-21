using System.Text.Json.Serialization;

namespace Arcmark.Models;

public class AppState
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("workspaces")]
    public List<Workspace> Workspaces { get; set; } = new();

    [JsonPropertyName("selectedWorkspaceId")]
    public Guid? SelectedWorkspaceId { get; set; }

    [JsonPropertyName("isSettingsSelected")]
    public bool IsSettingsSelected { get; set; }
}
