using Arcmark.Models;

namespace Arcmark.Services;

/// <summary>
/// Recursive tree filtering. Preserves hierarchy: parent folders are kept
/// when any of their children match. Folders containing matches are auto-expanded.
/// Matching is case-insensitive on display names and URLs.
/// </summary>
public static class NodeFiltering
{
    /// <summary>
    /// Filters <paramref name="nodes"/> to those whose display name or URL contains
    /// <paramref name="query"/> (case-insensitive). Parent folders are included when
    /// at least one descendant matches.
    /// </summary>
    public static List<Node> FilterNodes(List<Node> nodes, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return nodes;

        var lower = query.ToLowerInvariant();
        return nodes
            .Select(n => FilterNode(n, lower))
            .Where(n => n != null)
            .Select(n => n!)
            .ToList();
    }

    private static Node? FilterNode(Node node, string lowerQuery)
    {
        switch (node)
        {
            case LinkNode ln:
            {
                var nameMatch = ln.Link.Title.ToLowerInvariant().Contains(lowerQuery);
                var urlMatch  = ln.Link.Url.ToLowerInvariant().Contains(lowerQuery);
                return (nameMatch || urlMatch) ? node : null;
            }

            case FolderNode fn:
            {
                // Recurse into children first
                var matchingChildren = fn.Folder.Children
                    .Select(c => FilterNode(c, lowerQuery))
                    .Where(c => c != null)
                    .Select(c => c!)
                    .ToList();

                if (matchingChildren.Count > 0)
                {
                    // Return a copy with only the matching children, expanded
                    var filtered = new Folder
                    {
                        Id         = fn.Folder.Id,
                        Name       = fn.Folder.Name,
                        Children   = matchingChildren,
                        IsExpanded = true
                    };
                    return new FolderNode { Folder = filtered };
                }

                // Also match the folder itself by name
                if (fn.Folder.Name.ToLowerInvariant().Contains(lowerQuery))
                    return node;

                return null;
            }

            default:
                return null;
        }
    }
}
