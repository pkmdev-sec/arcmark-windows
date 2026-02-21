using System.IO;
using System.Text.Json;
using Arcmark.Models;

namespace Arcmark.Services;

/// <summary>
/// Imports from the Arc browser on Windows.
/// Arc stores data at: %LOCALAPPDATA%\Arc\User Data\Default\StorableSidebar.json
/// </summary>
public class ArcImportService
{
    private static readonly string DefaultDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Arc", "User Data", "Default");

    private static string StorableSidebarPath =>
        Path.Combine(DefaultDataPath, "StorableSidebar.json");

    /// <summary>
    /// Returns true if Arc's StorableSidebar.json file exists on this machine.
    /// </summary>
    public bool IsAvailable() => File.Exists(StorableSidebarPath);

    /// <summary>
    /// Parses Arc's StorableSidebar.json and converts each space to a workspace.
    /// </summary>
    public ImportResult Import()
    {
        if (!IsAvailable())
            return ImportResult.Failure("Arc browser data not found. Arc may not be installed.");

        try
        {
            var json = File.ReadAllText(StorableSidebarPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Arc's sidebar JSON structure: { sidebar: { containers: [...spaces] } }
            // Each space has: title, items (array of item objects)
            var workspaces = new List<Workspace>();

            // Try to navigate to spaces/containers
            JsonElement spacesElement = default;
            bool foundSpaces = false;

            // Try sidebar.containers path
            if (root.TryGetProperty("sidebar", out var sidebar) &&
                sidebar.TryGetProperty("containers", out spacesElement))
            {
                foundSpaces = true;
            }
            // Try top-level spaces
            else if (root.TryGetProperty("spaces", out spacesElement))
            {
                foundSpaces = true;
            }
            // Try sidebarSyncState.spaces
            else if (root.TryGetProperty("sidebarSyncState", out var syncState) &&
                     syncState.TryGetProperty("spaces", out spacesElement))
            {
                foundSpaces = true;
            }

            if (!foundSpaces)
            {
                // Try to parse as a flat items structure
                return ParseFlatStructure(root);
            }

            foreach (var space in spacesElement.EnumerateArray())
            {
                var ws = ParseSpace(space);
                if (ws != null) workspaces.Add(ws);
            }

            if (workspaces.Count == 0)
                return ImportResult.Failure("No spaces found in Arc data.");

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
            return ImportResult.Failure($"Failed to import Arc data: {ex.Message}");
        }
    }

    private static Workspace? ParseSpace(JsonElement space)
    {
        // Space title
        var title = "Arc Space";
        if (space.TryGetProperty("title", out var titleProp))
            title = titleProp.GetString() ?? title;

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = title,
            ColorId = WorkspaceColorIdExtensions.GetRandomColor()
        };

        // Try to get items from multiple possible locations
        JsonElement itemsElement = default;
        bool hasItems = false;

        if (space.TryGetProperty("items", out itemsElement))
            hasItems = true;
        else if (space.TryGetProperty("tabs", out itemsElement))
            hasItems = true;
        else if (space.TryGetProperty("pinnedTabs", out itemsElement))
            hasItems = true;

        if (hasItems)
        {
            ParseArcItems(itemsElement, workspace.Items, space);
        }

        if (workspace.Items.Count == 0) return null;
        return workspace;
    }

    private static void ParseArcItems(JsonElement itemsElement, List<Node> target, JsonElement spaceElement)
    {
        // Build a lookup of all items by ID for resolving childrenIds
        var allItemsById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        CollectItemsById(spaceElement, allItemsById);

        foreach (var item in itemsElement.EnumerateArray())
        {
            var node = ParseArcItem(item, allItemsById);
            if (node != null) target.Add(node);
        }
    }

    private static void CollectItemsById(JsonElement element, Dictionary<string, JsonElement> lookup)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("id", out var idProp))
            {
                var id = idProp.GetString();
                if (!string.IsNullOrEmpty(id))
                    lookup[id] = element;
            }

            foreach (var prop in element.EnumerateObject())
                CollectItemsById(prop.Value, lookup);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                CollectItemsById(child, lookup);
        }
    }

    private static Node? ParseArcItem(JsonElement item, Dictionary<string, JsonElement> allItems)
    {
        // Arc item structure: { data: { tab: { savedTitle, savedURL } }, childrenIds: [...] }
        string? title = null;
        string? url = null;

        // Try data.tab path
        if (item.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("tab", out var tab))
            {
                if (tab.TryGetProperty("savedTitle", out var t)) title = t.GetString();
                if (tab.TryGetProperty("savedURL", out var u)) url = u.GetString();
                // Also try title/url directly in tab
                if (title == null && tab.TryGetProperty("title", out var t2)) title = t2.GetString();
                if (url == null && tab.TryGetProperty("url", out var u2)) url = u2.GetString();
            }
        }

        // Fallback: try title/url directly on item
        if (title == null && item.TryGetProperty("title", out var directTitle))
            title = directTitle.GetString();
        if (url == null && item.TryGetProperty("url", out var directUrl))
            url = directUrl.GetString();

        // Check for children (folder-like item)
        List<string>? childrenIds = null;
        if (item.TryGetProperty("childrenIds", out var childrenIdsProp) &&
            childrenIdsProp.ValueKind == JsonValueKind.Array)
        {
            childrenIds = childrenIdsProp.EnumerateArray()
                .Select(c => c.GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToList();
        }

        bool isFolder = childrenIds != null && childrenIds.Count > 0;

        if (isFolder)
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = title ?? "Folder",
                IsExpanded = false
            };

            foreach (var childId in childrenIds!)
            {
                if (allItems.TryGetValue(childId, out var childElement))
                {
                    var childNode = ParseArcItem(childElement, allItems);
                    if (childNode != null)
                        folder.Children.Add(childNode);
                }
            }

            return new FolderNode { Folder = folder };
        }
        else if (!string.IsNullOrWhiteSpace(url))
        {
            return new LinkNode
            {
                Link = new Link
                {
                    Id = Guid.NewGuid(),
                    Title = title ?? url,
                    Url = url
                }
            };
        }

        return null;
    }

    private static ImportResult ParseFlatStructure(JsonElement root)
    {
        // Fallback: treat the whole JSON as a single workspace
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Arc Import",
            ColorId = WorkspaceColorId.Sky
        };

        // Collect any URL-like entries we can find
        var allItemsById = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        CollectItemsById(root, allItemsById);

        foreach (var (_, item) in allItemsById)
        {
            string? url = null;
            string? title = null;

            if (item.TryGetProperty("savedURL", out var u)) url = u.GetString();
            if (item.TryGetProperty("savedTitle", out var t)) title = t.GetString();

            if (!string.IsNullOrWhiteSpace(url))
            {
                workspace.Items.Add(new LinkNode
                {
                    Link = new Link
                    {
                        Id = Guid.NewGuid(),
                        Title = title ?? url,
                        Url = url
                    }
                });
            }
        }

        if (workspace.Items.Count == 0)
            return ImportResult.Failure("No bookmarks found in Arc data.");

        return new ImportResult
        {
            Success = true,
            Workspaces = new List<Workspace> { workspace },
            WorkspaceCount = 1,
            LinkCount = workspace.Items.OfType<LinkNode>().Count(),
            FolderCount = 0
        };
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
