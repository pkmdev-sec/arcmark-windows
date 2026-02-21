using System.IO;
using System.Text.Json;
using Arcmark.Models;

namespace Arcmark.Services;

/// <summary>
/// Imports bookmarks from Google Chrome's Bookmarks JSON file.
/// Chrome stores bookmarks at: %LOCALAPPDATA%\Google\Chrome\User Data\Default\Bookmarks
/// </summary>
public class ChromeImportService
{
    private static readonly string DefaultBookmarksPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Google", "Chrome", "User Data", "Default", "Bookmarks");

    /// <summary>
    /// Returns true if Chrome's bookmarks file exists on this machine.
    /// </summary>
    public bool IsAvailable() => File.Exists(DefaultBookmarksPath);

    /// <summary>
    /// Parses Chrome's bookmarks file and returns a list of workspaces.
    /// Top-level folders (bookmark_bar, other) become separate workspaces
    /// unless <paramref name="mergeIntoSingle"/> is true.
    /// </summary>
    public ImportResult Import(bool mergeIntoSingle = false)
    {
        if (!IsAvailable())
            return ImportResult.Failure("Chrome bookmarks file not found.");

        try
        {
            var json = File.ReadAllText(DefaultBookmarksPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("roots", out var roots))
                return ImportResult.Failure("Invalid Chrome bookmarks format: missing 'roots'.");

            var workspaces = new List<Workspace>();

            // Parse bookmark_bar
            if (roots.TryGetProperty("bookmark_bar", out var bar))
            {
                var ws = ParseChromeFolder(bar, "Bookmarks Bar");
                if (ws != null) workspaces.Add(ws);
            }

            // Parse other bookmarks
            if (roots.TryGetProperty("other", out var other))
            {
                var ws = ParseChromeFolder(other, "Other Bookmarks");
                if (ws != null) workspaces.Add(ws);
            }

            // Parse mobile bookmarks (optional)
            if (roots.TryGetProperty("synced", out var synced))
            {
                var ws = ParseChromeFolder(synced, "Mobile Bookmarks");
                if (ws != null) workspaces.Add(ws);
            }

            if (mergeIntoSingle && workspaces.Count > 1)
            {
                var merged = new Workspace
                {
                    Id = Guid.NewGuid(),
                    Name = "Chrome Bookmarks",
                    ColorId = WorkspaceColorId.Sky
                };
                foreach (var ws in workspaces)
                    merged.Items.AddRange(ws.Items);
                workspaces = new List<Workspace> { merged };
            }

            int linkCount = workspaces.Sum(w => CountLinks(w.Items));
            int folderCount = workspaces.Sum(w => CountFolders(w.Items));

            return new ImportResult
            {
                Success = true,
                Workspaces = workspaces,
                WorkspaceCount = workspaces.Count,
                LinkCount = linkCount,
                FolderCount = folderCount
            };
        }
        catch (Exception ex)
        {
            return ImportResult.Failure($"Failed to import Chrome bookmarks: {ex.Message}");
        }
    }

    private static Workspace? ParseChromeFolder(JsonElement element, string defaultName)
    {
        var name = element.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString() ?? defaultName
            : defaultName;

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name,
            ColorId = WorkspaceColorIdExtensions.GetRandomColor()
        };

        if (element.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                var node = ParseNode(child);
                if (node != null)
                    workspace.Items.Add(node);
            }
        }

        // Skip workspaces with no items
        if (workspace.Items.Count == 0) return null;

        return workspace;
    }

    private static Node? ParseNode(JsonElement element)
    {
        if (!element.TryGetProperty("type", out var typeProp)) return null;
        var type = typeProp.GetString();

        var name = element.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString() ?? string.Empty
            : string.Empty;

        if (type == "url")
        {
            var url = element.TryGetProperty("url", out var urlProp)
                ? urlProp.GetString() ?? string.Empty
                : string.Empty;

            return new LinkNode
            {
                Link = new Link
                {
                    Id = Guid.NewGuid(),
                    Title = name,
                    Url = url
                }
            };
        }
        else if (type == "folder")
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsExpanded = false
            };

            if (element.TryGetProperty("children", out var children))
            {
                foreach (var child in children.EnumerateArray())
                {
                    var childNode = ParseNode(child);
                    if (childNode != null)
                        folder.Children.Add(childNode);
                }
            }

            return new FolderNode { Folder = folder };
        }

        return null;
    }

    private static int CountLinks(IList<Node> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node is LinkNode) count++;
            else if (node is FolderNode fn) count += CountLinks(fn.Folder.Children);
        }
        return count;
    }

    private static int CountFolders(IList<Node> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node is FolderNode fn)
            {
                count++;
                count += CountFolders(fn.Folder.Children);
            }
        }
        return count;
    }
}

