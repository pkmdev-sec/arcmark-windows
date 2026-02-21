using System.Text.Json;
using Xunit;
using Arcmark.Models;

namespace Arcmark.Tests;

public class JsonRoundTripTests
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new NodeJsonConverter(), new WorkspaceColorIdConverter() }
    };

    [Fact]
    public void AppState_RoundTrips()
    {
        var state = new AppState
        {
            SchemaVersion = 1,
            Workspaces =
            [
                new Workspace
                {
                    Id = Guid.NewGuid(),
                    Name = "Inbox",
                    ColorId = WorkspaceColorId.Sky,
                    Items =
                    [
                        new LinkNode
                        {
                            Link = new Link { Id = Guid.NewGuid(), Title = "Example", Url = "https://example.com", FaviconPath = null }
                        }
                    ],
                    PinnedLinks = []
                }
            ],
            SelectedWorkspaceId = Guid.NewGuid(),
            IsSettingsSelected = false
        };

        var json = JsonSerializer.Serialize(state, Options);
        var deserialized = JsonSerializer.Deserialize<AppState>(json, Options);

        Assert.NotNull(deserialized);
        Assert.Equal(state.SchemaVersion, deserialized!.SchemaVersion);
        Assert.Single(deserialized.Workspaces);
        Assert.Equal(state.Workspaces[0].Name, deserialized.Workspaces[0].Name);
        Assert.Equal(state.Workspaces[0].ColorId, deserialized.Workspaces[0].ColorId);
        Assert.Single(deserialized.Workspaces[0].Items);
    }

    [Fact]
    public void Node_FolderNode_SerializesCorrectly()
    {
        // Verify the tagged union format: {"type":"folder","folder":{...}}
        var folder = new FolderNode
        {
            Folder = new Folder { Id = Guid.NewGuid(), Name = "Work", Children = new(), IsExpanded = true }
        };

        var json = JsonSerializer.Serialize<Node>(folder, Options);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("type", out var typeEl));
        Assert.Equal("folder", typeEl.GetString());

        Assert.True(root.TryGetProperty("folder", out var folderEl));
        Assert.True(folderEl.TryGetProperty("name", out var nameEl));
        Assert.Equal("Work", nameEl.GetString());
    }

    [Fact]
    public void Node_LinkNode_SerializesCorrectly()
    {
        // Verify: {"type":"link","link":{...}}
        var link = new LinkNode
        {
            Link = new Link { Id = Guid.NewGuid(), Title = "GitHub", Url = "https://github.com", FaviconPath = null }
        };

        var json = JsonSerializer.Serialize<Node>(link, Options);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("type", out var typeEl));
        Assert.Equal("link", typeEl.GetString());

        Assert.True(root.TryGetProperty("link", out var linkEl));
        Assert.True(linkEl.TryGetProperty("url", out var urlEl));
        Assert.Equal("https://github.com", urlEl.GetString());
    }

    [Fact]
    public void WorkspaceColorId_SerializesAsLowercase()
    {
        // Verify: "sky" not "Sky"
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            ColorId = WorkspaceColorId.Sky,
            Items = [],
            PinnedLinks = []
        };

        var json = JsonSerializer.Serialize(workspace, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("colorId", out var colorEl));
        Assert.Equal("sky", colorEl.GetString());
    }

    [Fact]
    public void MacOsJsonFile_CanBeDeserialized()
    {
        // Test with a sample macOS-generated JSON to verify cross-platform compatibility
        var macOsJson = """
            {
                "schemaVersion": 1,
                "workspaces": [{
                    "id": "550e8400-e29b-41d4-a716-446655440000",
                    "name": "Inbox",
                    "colorId": "sky",
                    "items": [
                        {
                            "type": "folder",
                            "folder": {
                                "id": "660e8400-e29b-41d4-a716-446655440001",
                                "name": "Work",
                                "children": [
                                    {
                                        "type": "link",
                                        "link": {
                                            "id": "770e8400-e29b-41d4-a716-446655440002",
                                            "title": "GitHub",
                                            "url": "https://github.com",
                                            "faviconPath": null
                                        }
                                    }
                                ],
                                "isExpanded": true
                            }
                        }
                    ],
                    "pinnedLinks": []
                }],
                "selectedWorkspaceId": "550e8400-e29b-41d4-a716-446655440000",
                "isSettingsSelected": false
            }
            """;

        var state = JsonSerializer.Deserialize<AppState>(macOsJson, Options);

        Assert.NotNull(state);
        Assert.Equal(1, state!.SchemaVersion);
        Assert.Single(state.Workspaces);

        var workspace = state.Workspaces[0];
        Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), workspace.Id);
        Assert.Equal("Inbox", workspace.Name);
        Assert.Equal(WorkspaceColorId.Sky, workspace.ColorId);
        Assert.Single(workspace.Items);
        Assert.Empty(workspace.PinnedLinks);
        Assert.False(state.IsSettingsSelected);

        var folder = workspace.Items[0] as FolderNode;
        Assert.NotNull(folder);
        Assert.Equal(Guid.Parse("660e8400-e29b-41d4-a716-446655440001"), folder!.Id);
        Assert.Equal("Work", folder.Folder.Name);
        Assert.True(folder.Folder.IsExpanded);
        Assert.Single(folder.Folder.Children);

        var link = folder.Folder.Children[0] as LinkNode;
        Assert.NotNull(link);
        Assert.Equal(Guid.Parse("770e8400-e29b-41d4-a716-446655440002"), link!.Id);
        Assert.Equal("GitHub", link.Link.Title);
        Assert.Equal("https://github.com", link.Link.Url);
        Assert.Null(link.Link.FaviconPath);
    }
}
