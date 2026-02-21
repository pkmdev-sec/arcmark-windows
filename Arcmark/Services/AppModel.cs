using Arcmark.Models;

namespace Arcmark.Services;

/// <summary>
/// Central state manager. Owns AppState, exposes read-only properties, and fires
/// onChange after every mutation. All tree operations are recursive mirrors of the
/// macOS AppModel.swift implementation.
/// </summary>
public class AppModel
{
    private readonly DataStore _store;
    private AppState _state;

    /// <summary>Fired after every state mutation that should update the UI.</summary>
    public event Action? OnChange;

    public AppModel(DataStore? store = null)
    {
        _store = store ?? new DataStore();
        _state = _store.Load();

        // Restore last selected workspace
        if (!_state.IsSettingsSelected)
        {
            var savedId = UserSettings.Current.LastSelectedWorkspaceId;
            if (savedId != null && Guid.TryParse(savedId, out var uuid)
                && _state.Workspaces.Any(w => w.Id == uuid))
            {
                _state.SelectedWorkspaceId = uuid;
            }
            if (_state.SelectedWorkspaceId == null)
            {
                _state.SelectedWorkspaceId = _state.Workspaces.FirstOrDefault()?.Id;
            }
        }
    }

    // ── Read-only surface ──────────────────────────────────────────────────────

    public AppState State => _state;

    public IReadOnlyList<Workspace> Workspaces => _state.Workspaces;

    public Workspace CurrentWorkspace
    {
        get
        {
            if (_state.SelectedWorkspaceId.HasValue)
            {
                var ws = _state.Workspaces.FirstOrDefault(w => w.Id == _state.SelectedWorkspaceId.Value);
                if (ws != null) return ws;
            }

            var first = _state.Workspaces.FirstOrDefault();
            if (first != null) return first;

            // Emergency fallback — create an Inbox workspace
            var fallback = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = "Inbox",
                ColorId = WorkspaceColorId.Sky
            };
            _state.Workspaces.Add(fallback);
            _state.SelectedWorkspaceId = fallback.Id;
            Persist();
            return fallback;
        }
    }

    // ── Workspace mutations ────────────────────────────────────────────────────

    public void SelectWorkspace(Guid id)
    {
        if (!_state.Workspaces.Any(w => w.Id == id)) return;
        _state.SelectedWorkspaceId = id;
        _state.IsSettingsSelected = false;
        UserSettings.Current.LastSelectedWorkspaceId = id.ToString();
        Persist();
    }

    public void SelectSettings()
    {
        _state.IsSettingsSelected = true;
        _state.SelectedWorkspaceId = null;
        Persist();
    }

    public Guid CreateWorkspace(string name, WorkspaceColorId colorId)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name,
            ColorId = colorId
        };
        _state.Workspaces.Add(workspace);
        _state.SelectedWorkspaceId = workspace.Id;
        UserSettings.Current.LastSelectedWorkspaceId = workspace.Id.ToString();
        Persist();
        return workspace.Id;
    }

    public void RenameWorkspace(Guid id, string newName)
    {
        UpdateWorkspace(id, ws => ws.Name = newName);
    }

    public void UpdateWorkspaceColor(Guid id, WorkspaceColorId colorId)
    {
        UpdateWorkspace(id, ws => ws.ColorId = colorId);
    }

    public void DeleteWorkspace(Guid id)
    {
        if (_state.Workspaces.Count <= 1) return;

        _state.Workspaces.RemoveAll(w => w.Id == id);

        if (_state.SelectedWorkspaceId == id)
        {
            _state.SelectedWorkspaceId = _state.Workspaces.FirstOrDefault()?.Id;
            if (_state.SelectedWorkspaceId.HasValue)
                UserSettings.Current.LastSelectedWorkspaceId = _state.SelectedWorkspaceId.Value.ToString();
        }
        Persist();
    }

    public void MoveWorkspace(Guid id, WorkspaceMoveDirection direction)
    {
        var currentIndex = _state.Workspaces.FindIndex(w => w.Id == id);
        if (currentIndex < 0) return;

        int newIndex = direction switch
        {
            WorkspaceMoveDirection.Left when currentIndex > 0 => currentIndex - 1,
            WorkspaceMoveDirection.Right when currentIndex < _state.Workspaces.Count - 1 => currentIndex + 1,
            _ => -1
        };
        if (newIndex < 0) return;

        var workspace = _state.Workspaces[currentIndex];
        _state.Workspaces.RemoveAt(currentIndex);
        _state.Workspaces.Insert(newIndex, workspace);
        Persist();
    }

    public void ReorderWorkspace(Guid id, int toIndex)
    {
        var currentIndex = _state.Workspaces.FindIndex(w => w.Id == id);
        if (currentIndex < 0) return;
        if (toIndex < 0 || toIndex >= _state.Workspaces.Count) return;
        if (currentIndex == toIndex) return;

        var workspace = _state.Workspaces[currentIndex];
        _state.Workspaces.RemoveAt(currentIndex);
        _state.Workspaces.Insert(toIndex, workspace);
        Persist();
    }

    // ── Node mutations ─────────────────────────────────────────────────────────

    public Guid AddFolder(string name, Guid? parentId, bool isExpanded = true)
    {
        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = name,
            Children = new List<Node>(),
            IsExpanded = isExpanded
        };
        InsertNode(new FolderNode { Folder = folder }, parentId);
        return folder.Id;
    }

    public Guid AddLink(string urlString, string title, Guid? parentId)
    {
        var link = new Link
        {
            Id = Guid.NewGuid(),
            Title = title,
            Url = urlString,
            FaviconPath = null
        };
        InsertNode(new LinkNode { Link = link }, parentId);
        return link.Id;
    }

    public void RenameNode(Guid id, string newName)
    {
        UpdateNode(id, node =>
        {
            switch (node)
            {
                case FolderNode fn: fn.Folder.Name = newName; break;
                case LinkNode ln: ln.Link.Title = newName; break;
            }
        });
    }

    public void DeleteNode(Guid id)
    {
        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            RemoveNode(id, ws.Items);
        });
    }

    public void MoveNode(Guid id, Guid? toParentId, int index)
    {
        var location = FindNodeLocation(id, CurrentWorkspace.Items);
        if (location == null) return;

        // Prevent moving a node into one of its own descendants
        if (toParentId.HasValue && IsDescendant(toParentId.Value, id)) return;

        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            var removed = RemoveNode(id, ws.Items);
            if (removed == null) return;

            int targetIndex = Math.Max(0, index);
            if (location.ParentId == toParentId && location.Index < targetIndex)
                targetIndex--;

            InsertNodeIntoList(removed, toParentId, targetIndex, ws.Items);
        });
    }

    public void MoveNodeToWorkspace(Guid id, Guid workspaceId)
    {
        if (workspaceId == CurrentWorkspace.Id) return;

        Node? removedNode = null;
        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            removedNode = RemoveNode(id, ws.Items);
        }, notify: false);

        if (removedNode == null) return;

        UpdateWorkspace(workspaceId, ws =>
        {
            ws.Items.Add(removedNode);
        });
    }

    public void SetFolderExpanded(Guid id, bool isExpanded)
    {
        UpdateNode(id, node =>
        {
            if (node is FolderNode fn)
                fn.Folder.IsExpanded = isExpanded;
        });
    }

    public void UpdateLinkFaviconPath(Guid id, string? path, bool notify = true)
    {
        // Skip if no change
        var existing = NodeById(id);
        if (existing is LinkNode existingLn && existingLn.Link.FaviconPath == path) return;

        UpdateNode(id, node =>
        {
            if (node is LinkNode ln)
                ln.Link.FaviconPath = path;
        }, notify: notify);
    }

    public void UpdateLinkUrl(Guid id, string newUrl)
    {
        UpdateNode(id, node =>
        {
            if (node is LinkNode ln)
            {
                ln.Link.Url = newUrl;
                ln.Link.FaviconPath = null;
            }
        });
    }

    /// <summary>
    /// Updates the link title only if the current title equals the auto-generated default
    /// (the hostname, or the raw URL). Returns true if the title was updated.
    /// </summary>
    public bool UpdateLinkTitleIfDefault(Guid id, string newTitle)
    {
        var trimmed = newTitle.Trim();
        if (string.IsNullOrEmpty(trimmed)) return false;

        var node = NodeById(id);
        if (node is not LinkNode ln) return false;

        var link = ln.Link;
        string defaultTitle = Uri.TryCreate(link.Url, UriKind.Absolute, out var uri)
            ? (uri.Host ?? link.Url)
            : link.Url;

        if (link.Title != defaultTitle) return false;
        if (link.Title == trimmed) return false;

        UpdateNode(id, n =>
        {
            if (n is LinkNode l)
                l.Link.Title = trimmed;
        });

        return true;
    }

    public void MarkLinkOpened(Guid id)
    {
        // LastOpened tracking not yet in the Windows Link model — no-op for now.
    }

    // ── Pinned link operations ─────────────────────────────────────────────────

    public bool CanPinMore => CurrentWorkspace.PinnedLinks.Count < Workspace.MaxPinnedLinks;

    public Link? PinnedLinkById(Guid id) =>
        CurrentWorkspace.PinnedLinks.FirstOrDefault(l => l.Id == id);

    public void PinLink(Guid id)
    {
        if (!CanPinMore) return;
        var node = NodeById(id);
        if (node is not LinkNode ln) return;
        if (CurrentWorkspace.PinnedLinks.Any(l => l.Id == id)) return;

        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            RemoveNode(id, ws.Items);
            ws.PinnedLinks.Add(ln.Link);
        });
    }

    public void UnpinLink(Guid id)
    {
        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            var index = ws.PinnedLinks.FindIndex(l => l.Id == id);
            if (index < 0) return;
            var link = ws.PinnedLinks[index];
            ws.PinnedLinks.RemoveAt(index);
            ws.Items.Add(new LinkNode { Link = link });
        });
    }

    public void UpdatePinnedLinkFaviconPath(Guid id, string? path)
    {
        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            var index = ws.PinnedLinks.FindIndex(l => l.Id == id);
            if (index >= 0)
                ws.PinnedLinks[index].FaviconPath = path;
        });
    }

    // ── Bulk operations ────────────────────────────────────────────────────────

    public void MoveNodesToWorkspace(IEnumerable<Guid> nodeIds, Guid toWorkspaceId)
    {
        if (toWorkspaceId == CurrentWorkspace.Id) return;
        var ids = nodeIds.ToList();
        if (ids.Count == 0) return;

        var nodesToMove = new List<Node>();

        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            foreach (var nodeId in ids)
            {
                var removed = RemoveNode(nodeId, ws.Items);
                if (removed != null) nodesToMove.Add(removed);
            }
        }, notify: false);

        UpdateWorkspace(toWorkspaceId, ws =>
        {
            ws.Items.AddRange(nodesToMove);
        });
    }

    public Guid? GroupNodesInNewFolder(IEnumerable<Guid> nodeIds, string folderName)
    {
        var ids = nodeIds.ToList();
        if (ids.Count == 0) return null;

        var nodesToGroup = new List<Node>();

        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            foreach (var nodeId in ids)
            {
                var removed = RemoveNode(nodeId, ws.Items);
                if (removed != null) nodesToGroup.Add(removed);
            }
        }, notify: false);

        if (nodesToGroup.Count == 0) return null;

        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = folderName,
            Children = nodesToGroup,
            IsExpanded = true
        };

        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            ws.Items.Add(new FolderNode { Folder = folder });
        });

        return folder.Id;
    }

    // ── Node lookup helpers ────────────────────────────────────────────────────

    public NodeLocation? Location(Guid nodeId) =>
        FindNodeLocation(nodeId, CurrentWorkspace.Items);

    public Node? NodeById(Guid id) =>
        NodeById(id, CurrentWorkspace.Items);

    public Node? FindNode(Guid id, IList<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id) return node;
            if (node is FolderNode fn)
            {
                var found = FindNode(id, fn.Folder.Children);
                if (found != null) return found;
            }
        }
        return null;
    }

    // ── Duplicate detection ────────────────────────────────────────────────────

    public (string WorkspaceName, string LinkTitle)? FindDuplicateLink(string url)
    {
        var normalized = NormalizeUrl(url);
        foreach (var workspace in _state.Workspaces)
        {
            var match = FindLinkInNodes(normalized, workspace.Items);
            if (match != null)
                return (workspace.Name, match.Title);
        }
        return null;
    }

    private static string NormalizeUrl(string url)
    {
        var normalized = url.ToLowerInvariant().Trim();

        if (normalized.StartsWith("http://"))
            normalized = "https://" + normalized["http://".Length..];

        if (normalized.EndsWith("/"))
            normalized = normalized[..^1];

        var wwwTag = "://www.";
        var wwwIdx = normalized.IndexOf(wwwTag, StringComparison.Ordinal);
        if (wwwIdx >= 0)
            normalized = normalized[..(wwwIdx + 3)] + normalized[(wwwIdx + wwwTag.Length)..];

        var hashIdx = normalized.IndexOf('#');
        if (hashIdx >= 0)
            normalized = normalized[..hashIdx];

        if (normalized.EndsWith("/"))
            normalized = normalized[..^1];

        return normalized;
    }

    private Link? FindLinkInNodes(string normalizedUrl, IList<Node> nodes)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case LinkNode ln when NormalizeUrl(ln.Link.Url) == normalizedUrl:
                    return ln.Link;
                case FolderNode fn:
                    var found = FindLinkInNodes(normalizedUrl, fn.Folder.Children);
                    if (found != null) return found;
                    break;
            }
        }
        return null;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void InsertNode(Node node, Guid? parentId)
    {
        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            InsertNodeIntoList(node, parentId, null, ws.Items);
        });
    }

    private static void InsertNodeIntoList(Node node, Guid? parentId, int? index, IList<Node> nodes)
    {
        if (parentId.HasValue)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is FolderNode fn)
                {
                    if (fn.Folder.Id == parentId.Value)
                    {
                        if (index.HasValue)
                        {
                            int idx = Math.Max(0, Math.Min(index.Value, fn.Folder.Children.Count));
                            fn.Folder.Children.Insert(idx, node);
                        }
                        else
                        {
                            fn.Folder.Children.Add(node);
                        }
                        return;
                    }
                    // Recurse into children
                    InsertNodeIntoList(node, parentId, index, fn.Folder.Children);
                }
            }
        }
        else
        {
            if (index.HasValue)
            {
                int idx = Math.Max(0, Math.Min(index.Value, nodes.Count));
                nodes.Insert(idx, node);
            }
            else
            {
                nodes.Add(node);
            }
        }
    }

    private void UpdateWorkspace(Guid id, Action<Workspace> mutate, bool notify = true)
    {
        var workspace = _state.Workspaces.FirstOrDefault(w => w.Id == id);
        if (workspace == null) return;
        mutate(workspace);
        Persist(notify);
    }

    private void UpdateNode(Guid id, Action<Node> mutate, bool notify = true)
    {
        UpdateWorkspace(CurrentWorkspace.Id, ws =>
        {
            UpdateNodeInList(id, ws.Items, mutate);
        }, notify: notify);
    }

    private static bool UpdateNodeInList(Guid id, IList<Node> nodes, Action<Node> mutate)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Id == id)
            {
                mutate(node);
                return true;
            }
            if (node is FolderNode fn)
            {
                if (UpdateNodeInList(id, fn.Folder.Children, mutate))
                    return true;
            }
        }
        return false;
    }

    private static Node? RemoveNode(Guid id, IList<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Id == id)
            {
                nodes.RemoveAt(i);
                return node;
            }
            if (node is FolderNode fn)
            {
                var removed = RemoveNode(id, fn.Folder.Children);
                if (removed != null) return removed;
            }
        }
        return null;
    }

    private static NodeLocation? FindNodeLocation(Guid id, IList<Node> nodes, Guid? parentId = null)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Id == id)
                return new NodeLocation(parentId, i);

            if (node is FolderNode fn)
            {
                var loc = FindNodeLocation(id, fn.Folder.Children, fn.Folder.Id);
                if (loc != null) return loc;
            }
        }
        return null;
    }

    private bool IsDescendant(Guid nodeId, Guid potentialAncestorId)
    {
        var ancestor = NodeById(potentialAncestorId, CurrentWorkspace.Items);
        if (ancestor == null) return false;
        return ContainsNode(nodeId, ancestor);
    }

    private static bool ContainsNode(Guid id, Node node)
    {
        if (node.Id == id) return true;
        if (node is FolderNode fn)
            return fn.Folder.Children.Any(child => ContainsNode(id, child));
        return false;
    }

    private static Node? NodeById(Guid id, IList<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id) return node;
            if (node is FolderNode fn)
            {
                var found = NodeById(id, fn.Folder.Children);
                if (found != null) return found;
            }
        }
        return null;
    }

    private void Persist(bool notify = true)
    {
        _store.Save(_state);
        if (notify)
            OnChange?.Invoke();
    }
}
