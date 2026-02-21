using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Arcmark.Models;
using Arcmark.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcmark.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppModel _model;

    // ── Observable state ──────────────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<WorkspaceViewModel> _workspaces = new();
    [ObservableProperty] private WorkspaceViewModel? _selectedWorkspace;
    [ObservableProperty] private bool _isSettingsSelected;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private ObservableCollection<NodeViewModel> _visibleNodes = new();
    [ObservableProperty] private ObservableCollection<LinkViewModel> _pinnedLinks = new();
    [ObservableProperty] private SolidColorBrush _workspaceBackground = new(Colors.White);
    [ObservableProperty] private bool _hasPinnedLinks;
    [ObservableProperty] private bool _canPasteLinks;
    [ObservableProperty] private string _pasteButtonLabel = "Add links from clipboard";

    // Context-menu data for workspace right-click
    [ObservableProperty] private ObservableCollection<WorkspaceViewModel> _otherWorkspaces = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainViewModel(AppModel model)
    {
        _model = model;
        _model.OnChange += OnModelChanged;
        ReloadData();
    }

    // ── Model change handler ──────────────────────────────────────────────────

    private void OnModelChanged()
    {
        Application.Current?.Dispatcher.Invoke(ReloadData);
    }

    // ── Reload ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds all observable collections from the current model state.
    /// Called after every AppModel mutation.
    /// </summary>
    public void ReloadData()
    {
        var state = _model.State;

        // ── Workspaces ────────────────────────────────────────────────────────
        Workspaces.Clear();
        foreach (var ws in _model.Workspaces)
        {
            Workspaces.Add(WorkspaceViewModel.FromWorkspace(ws, state.SelectedWorkspaceId));
        }

        IsSettingsSelected = state.IsSettingsSelected;
        SelectedWorkspace  = Workspaces.FirstOrDefault(vm => vm.IsSelected);

        // ── Background color ──────────────────────────────────────────────────
        if (!state.IsSettingsSelected)
        {
            var bgColor = _model.CurrentWorkspace.ColorId.GetBackgroundColor();
            WorkspaceBackground = new SolidColorBrush(bgColor);
        }
        else
        {
            WorkspaceBackground = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        }

        // ── Pinned links ──────────────────────────────────────────────────────
        PinnedLinks.Clear();
        if (!state.IsSettingsSelected)
        {
            foreach (var link in _model.CurrentWorkspace.PinnedLinks)
            {
                PinnedLinks.Add(LinkViewModel.FromLink(link));
            }
        }
        HasPinnedLinks = PinnedLinks.Count > 0;

        // ── Other workspaces (for context menu move-to) ───────────────────────
        OtherWorkspaces.Clear();
        foreach (var ws in _model.Workspaces)
        {
            if (!state.IsSettingsSelected && ws.Id != _model.CurrentWorkspace.Id)
                OtherWorkspaces.Add(WorkspaceViewModel.FromWorkspace(ws, state.SelectedWorkspaceId));
        }

        // ── Node tree ─────────────────────────────────────────────────────────
        BuildVisibleNodes();
    }

    // ── Node tree flattening ──────────────────────────────────────────────────

    /// <summary>
    /// Flattens the workspace tree into a display list, respecting expanded state
    /// and the current search query.
    /// </summary>
    private void BuildVisibleNodes()
    {
        VisibleNodes.Clear();

        if (_model.State.IsSettingsSelected) return;

        var items  = _model.CurrentWorkspace.Items;
        var query  = SearchQuery.Trim().ToLowerInvariant();
        var hasSearch = !string.IsNullOrEmpty(query);

        if (hasSearch)
        {
            // Search: flatten everything and filter by name/url
            FlattenFiltered(items, query, 0);
        }
        else
        {
            // Normal: respect folder expansion
            FlattenExpanded(items, 0);
        }
    }

    private void FlattenExpanded(IList<Node> nodes, int depth)
    {
        foreach (var node in nodes)
        {
            var vm = NodeViewModel.FromNode(node, depth);
            VisibleNodes.Add(vm);

            if (node is FolderNode fn && fn.Folder.IsExpanded)
            {
                FlattenExpanded(fn.Folder.Children, depth + 1);
            }
        }
    }

    private void FlattenFiltered(IList<Node> nodes, string query, int depth)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case LinkNode ln:
                    if (ln.Link.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        ln.Link.Url.Contains(query,   StringComparison.OrdinalIgnoreCase))
                    {
                        VisibleNodes.Add(NodeViewModel.FromNode(node, depth));
                    }
                    break;

                case FolderNode fn:
                    // Include folder if name matches OR if any child matches
                    bool folderMatch = fn.Folder.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                    if (folderMatch)
                    {
                        // Include folder and all its descendants expanded
                        VisibleNodes.Add(NodeViewModel.FromNode(node, depth));
                        FlattenFiltered(fn.Folder.Children, query, depth + 1);
                    }
                    else
                    {
                        // Check if any descendant matches
                        var childMatches = new ObservableCollection<NodeViewModel>();
                        CollectMatches(fn.Folder.Children, query, depth + 1, childMatches);
                        if (childMatches.Count > 0)
                        {
                            VisibleNodes.Add(NodeViewModel.FromNode(node, depth));
                            foreach (var c in childMatches) VisibleNodes.Add(c);
                        }
                    }
                    break;
            }
        }
    }

    private static void CollectMatches(IList<Node> nodes, string query, int depth,
        ObservableCollection<NodeViewModel> results)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case LinkNode ln when
                    ln.Link.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    ln.Link.Url.Contains(query,   StringComparison.OrdinalIgnoreCase):
                    results.Add(NodeViewModel.FromNode(node, depth));
                    break;
                case FolderNode fn:
                    bool folderMatch = fn.Folder.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                    if (folderMatch)
                    {
                        results.Add(NodeViewModel.FromNode(node, depth));
                        CollectMatches(fn.Folder.Children, query, depth + 1, results);
                    }
                    else
                    {
                        var sub = new ObservableCollection<NodeViewModel>();
                        CollectMatches(fn.Folder.Children, query, depth + 1, sub);
                        if (sub.Count > 0)
                        {
                            results.Add(NodeViewModel.FromNode(node, depth));
                            foreach (var s in sub) results.Add(s);
                        }
                    }
                    break;
            }
        }
    }

    // ── Search ────────────────────────────────────────────────────────────────

    partial void OnSearchQueryChanged(string value)
    {
        BuildVisibleNodes();
    }

    // ── Parameter resolution helper ───────────────────────────────────────────

    private static Guid ResolveId(object? parameter) => parameter switch
    {
        Guid g => g,
        WorkspaceViewModel vm => vm.Id,
        NodeViewModel nvm => nvm.Id,
        LinkViewModel lvm => lvm.Id,
        _ => Guid.Empty
    };

    // ── Commands: workspace ───────────────────────────────────────────────────

    [RelayCommand]
    private void SelectWorkspace(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        _model.SelectWorkspace(id);
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void SelectSettings()
    {
        _model.SelectSettings();
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void CreateWorkspace()
    {
        var colorId = WorkspaceColorIdExtensions.GetRandomColor();
        _model.CreateWorkspace("New Workspace", colorId);
    }

    [RelayCommand]
    private void RenameWorkspace(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        var vm = Workspaces.FirstOrDefault(w => w.Id == id);
        if (vm != null) vm.IsRenaming = true;
    }

    [RelayCommand]
    private void CommitRenameWorkspace((Guid Id, string Name) args)
    {
        var trimmed = args.Name.Trim();
        if (!string.IsNullOrEmpty(trimmed))
            _model.RenameWorkspace(args.Id, trimmed);

        var vm = Workspaces.FirstOrDefault(w => w.Id == args.Id);
        if (vm != null) vm.IsRenaming = false;
    }

    [RelayCommand]
    private void DeleteWorkspace(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        _model.DeleteWorkspace(id);
    }

    [RelayCommand]
    private void MoveWorkspaceLeft(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        _model.MoveWorkspace(id, WorkspaceMoveDirection.Left);
    }

    [RelayCommand]
    private void MoveWorkspaceRight(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        _model.MoveWorkspace(id, WorkspaceMoveDirection.Right);
    }

    [RelayCommand]
    private void UpdateWorkspaceColor((Guid Id, WorkspaceColorId ColorId) args)
    {
        _model.UpdateWorkspaceColor(args.Id, args.ColorId);
    }

    // ── Commands: nodes ───────────────────────────────────────────────────────

    [RelayCommand]
    private void CreateFolder()
    {
        _model.AddFolder("New Folder", parentId: null, isExpanded: true);
    }

    [RelayCommand]
    private void DeleteNode(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        _model.DeleteNode(id);
    }

    [RelayCommand]
    private void RenameNode(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        var vm = VisibleNodes.FirstOrDefault(n => n.Id == id);
        if (vm != null) vm.IsRenaming = true;
    }

    [RelayCommand]
    private void CommitRenameNode((Guid Id, string Name) args)
    {
        var trimmed = args.Name.Trim();
        if (!string.IsNullOrEmpty(trimmed))
            _model.RenameNode(args.Id, trimmed);

        var vm = VisibleNodes.FirstOrDefault(n => n.Id == args.Id);
        if (vm != null) vm.IsRenaming = false;
    }

    [RelayCommand]
    private void ToggleFolderExpanded(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        var node = _model.NodeById(id);
        if (node is FolderNode fn)
        {
            _model.SetFolderExpanded(id, !fn.Folder.IsExpanded);
        }
    }

    [RelayCommand]
    private void MoveNodeToWorkspace((Guid NodeId, Guid WorkspaceId) args)
    {
        _model.MoveNodeToWorkspace(args.NodeId, args.WorkspaceId);
    }

    [RelayCommand]
    private void OpenLink(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        var node = _model.NodeById(id);
        if (node is LinkNode ln)
        {
            OpenUrl(ln.Link.Url);
            _model.MarkLinkOpened(id);
        }
    }

    [RelayCommand]
    private void PinLink(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        _model.PinLink(id);
    }

    // ── Commands: pinned tabs ─────────────────────────────────────────────────

    [RelayCommand]
    private void OpenPinnedLink(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        var link = _model.PinnedLinkById(id);
        if (link != null) OpenUrl(link.Url);
    }

    [RelayCommand]
    private void UnpinLink(object? parameter)
    {
        var id = ResolveId(parameter);
        if (id == Guid.Empty) return;
        _model.UnpinLink(id);
    }

    // ── Commands: clipboard paste ─────────────────────────────────────────────

    [RelayCommand]
    private void PasteLinks()
    {
        if (!Clipboard.ContainsText()) return;
        var text = Clipboard.GetText();
        var urls = ExtractUrls(text);
        if (urls.Count == 0) return;

        foreach (var url in urls)
        {
            // Use hostname as default title
            var title = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
            _model.AddLink(url, title, parentId: null);
        }
    }

    private static readonly Regex UrlRegex = new(
        @"https?://[^\s<>""\{\}\|\\\^\[\]`]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static List<string> ExtractUrls(string text)
    {
        var matches = UrlRegex.Matches(text);
        return matches.Select(m => m.Value).Distinct().ToList();
    }

    // ── Favicon updates (called by FaviconService) ────────────────────────────

    public void HandleFaviconUpdate(Guid nodeId, string? faviconPath)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var vm = VisibleNodes.FirstOrDefault(n => n.Id == nodeId);
            if (vm != null)
                vm.Icon = NodeViewModel.LoadFaviconStatic(faviconPath);

            var pinnedVm = PinnedLinks.FirstOrDefault(p => p.Id == nodeId);
            if (pinnedVm != null)
                pinnedVm.Favicon = LinkViewModel.LoadFaviconStatic(faviconPath);
        });
    }

    // ── Always on top / window helpers ───────────────────────────────────────

    [ObservableProperty] private bool _isAlwaysOnTop;

    [RelayCommand]
    private void ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
        if (Application.Current.MainWindow is Window win)
            win.Topmost = IsAlwaysOnTop;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = url,
                UseShellExecute = true
            });
        }
        catch { /* swallow */ }
    }
}
