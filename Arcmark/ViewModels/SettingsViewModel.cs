using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Arcmark.Models;
using Arcmark.Services;

namespace Arcmark.ViewModels;

/// <summary>
/// ViewModel for the Settings panel.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppModel _appModel;
    private readonly ChromeImportService _chromeImport;
    private readonly ArcImportService _arcImport;

    [ObservableProperty]
    private string _appVersion = "0.1.4";

    [ObservableProperty]
    private bool _alwaysOnTopEnabled;

    [ObservableProperty]
    private bool _sidebarAttachmentEnabled;

    [ObservableProperty]
    private SidebarPosition _sidebarPosition;

    [ObservableProperty]
    private string _selectedBrowser = string.Empty;

    [ObservableProperty]
    private ObservableCollection<BrowserInfo> _availableBrowsers = new();

    [ObservableProperty]
    private ObservableCollection<WorkspaceSettingsViewModel> _workspaces = new();

    [ObservableProperty]
    private string _shortcutDisplay = string.Empty;

    [ObservableProperty]
    private bool _isRecordingShortcut;

    [ObservableProperty]
    private string _importStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isChromeAvailable;

    [ObservableProperty]
    private bool _isArcAvailable;

    public SettingsViewModel() : this(new AppModel()) { }

    public SettingsViewModel(AppModel appModel)
    {
        _appModel = appModel;
        _chromeImport = new ChromeImportService();
        _arcImport = new ArcImportService();

        LoadSettings();
        LoadWorkspaces();
        LoadBrowsers();
        CheckImportSources();

        _appModel.OnChange += OnAppModelChanged;
    }

    private void LoadSettings()
    {
        var settings = UserSettings.Current;
        AlwaysOnTopEnabled = settings.AlwaysOnTopEnabled;
        SidebarAttachmentEnabled = settings.SidebarAttachmentEnabled;
        SidebarPosition = settings.SidebarPosition.Equals("left", StringComparison.OrdinalIgnoreCase)
            ? SidebarPosition.Left
            : SidebarPosition.Right;

        ShortcutDisplay = settings.ToggleSidebarShortcutString ?? string.Empty;

        var version = System.Reflection.Assembly.GetExecutingAssembly()
            .GetName()
            .Version;
        if (version != null)
            AppVersion = $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private void LoadWorkspaces()
    {
        Workspaces.Clear();
        foreach (var ws in _appModel.Workspaces)
            Workspaces.Add(new WorkspaceSettingsViewModel(ws));
    }

    private void LoadBrowsers()
    {
        AvailableBrowsers.Clear();
        var browsers = BrowserManager.GetInstalledBrowsers();
        foreach (var (name, path) in browsers)
            AvailableBrowsers.Add(new BrowserInfo(name, path, null));

        var defaultPath = UserSettings.Current.DefaultBrowserPath;
        if (!string.IsNullOrEmpty(defaultPath))
        {
            var match = AvailableBrowsers.FirstOrDefault(b =>
                string.Equals(b.ExecutablePath, defaultPath, StringComparison.OrdinalIgnoreCase));
            SelectedBrowser = match?.Name ?? string.Empty;
        }
        else
        {
            var defaultBrowserPath = BrowserManager.GetDefaultBrowserPath();
            var match = AvailableBrowsers.FirstOrDefault(b =>
                string.Equals(b.ExecutablePath, defaultBrowserPath, StringComparison.OrdinalIgnoreCase));
            SelectedBrowser = match?.Name ?? string.Empty;
        }
    }

    private void CheckImportSources()
    {
        IsChromeAvailable = _chromeImport.IsAvailable();
        IsArcAvailable = _arcImport.IsAvailable();
    }

    private void OnAppModelChanged()
    {
        Application.Current?.Dispatcher.Invoke(LoadWorkspaces);
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleAlwaysOnTop()
    {
        AlwaysOnTopEnabled = !AlwaysOnTopEnabled;
        UserSettings.Current.AlwaysOnTopEnabled = AlwaysOnTopEnabled;

        if (Application.Current?.MainWindow != null)
            Application.Current.MainWindow.Topmost = AlwaysOnTopEnabled;
    }

    [RelayCommand]
    private void ToggleSidebarAttachment()
    {
        SidebarAttachmentEnabled = !SidebarAttachmentEnabled;
        UserSettings.Current.SidebarAttachmentEnabled = SidebarAttachmentEnabled;
    }

    [RelayCommand]
    private void ChangeSidebarPosition(SidebarPosition position)
    {
        SidebarPosition = position;
        UserSettings.Current.SidebarPosition = position == SidebarPosition.Left ? "left" : "right";
    }

    [RelayCommand]
    private void SelectBrowser(BrowserInfo? browser)
    {
        if (browser == null) return;
        SelectedBrowser = browser.Name;
        UserSettings.Current.DefaultBrowserPath = browser.ExecutablePath;
    }

    [RelayCommand]
    private void ImportFromChrome()
    {
        ImportStatusMessage = "Importing from Chrome...";
        var result = _chromeImport.Import();
        HandleImportResult(result);
    }

    [RelayCommand]
    private void ImportFromArc()
    {
        ImportStatusMessage = "Importing from Arc...";
        var result = _arcImport.Import();
        HandleImportResult(result);
    }

    private void HandleImportResult(ImportResult result)
    {
        if (!result.Success)
        {
            ImportStatusMessage = result.ErrorMessage ?? "Import failed.";
            return;
        }

        // Add each imported workspace to the app model
        foreach (var ws in result.Workspaces)
        {
            var newId = _appModel.CreateWorkspace(ws.Name, ws.ColorId);
            // Copy items
            var newWs = _appModel.Workspaces.FirstOrDefault(w => w.Id == newId);
            if (newWs != null)
            {
                newWs.Items.AddRange(ws.Items);
            }
        }

        ImportStatusMessage = result.ToString();
        LoadWorkspaces();
    }

    [RelayCommand]
    private void CheckForUpdates()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/arcmark/arcmark-windows/releases",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CheckForUpdates failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RecordShortcut()
    {
        IsRecordingShortcut = true;
        ShortcutDisplay = "Press shortcut...";
    }

    [RelayCommand]
    private void ClearShortcut()
    {
        ShortcutDisplay = string.Empty;
        IsRecordingShortcut = false;
        UserSettings.Current.ToggleSidebarShortcutString = null;
    }

    /// <summary>
    /// Called by the ShortcutRecorder control when a key combination is captured.
    /// </summary>
    public void OnShortcutCaptured(Key key, ModifierKeys modifiers)
    {
        if (key == Key.Escape)
        {
            IsRecordingShortcut = false;
            ShortcutDisplay = UserSettings.Current.ToggleSidebarShortcutString ?? string.Empty;
            return;
        }

        // Build display string
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());

        ShortcutDisplay = string.Join("+", parts);
        UserSettings.Current.ToggleSidebarShortcutString = ShortcutDisplay;
        IsRecordingShortcut = false;
    }

    // ── Workspace management ───────────────────────────────────────────────────

    [RelayCommand]
    private void RenameWorkspace(WorkspaceSettingsViewModel? vm)
    {
        if (vm == null) return;
        vm.IsRenaming = true;
    }

    [RelayCommand]
    private void CommitRenameWorkspace(WorkspaceSettingsViewModel? vm)
    {
        if (vm == null) return;
        vm.IsRenaming = false;
        if (!string.IsNullOrWhiteSpace(vm.Name))
            _appModel.RenameWorkspace(vm.Id, vm.Name);
    }

    [RelayCommand]
    private void ChangeWorkspaceColor(object? parameter)
    {
        // parameter is expected to be a (WorkspaceSettingsViewModel, WorkspaceColorId) tuple
        if (parameter is not (WorkspaceSettingsViewModel vm, WorkspaceColorId colorId))
            return;
        vm.ColorId = colorId;
        _appModel.UpdateWorkspaceColor(vm.Id, colorId);
    }

    [RelayCommand]
    private void DeleteWorkspace(WorkspaceSettingsViewModel? vm)
    {
        if (vm == null) return;
        if (_appModel.Workspaces.Count <= 1) return;
        _appModel.DeleteWorkspace(vm.Id);
    }

    [RelayCommand]
    private void MoveWorkspaceUp(WorkspaceSettingsViewModel? vm)
    {
        if (vm == null) return;
        _appModel.MoveWorkspace(vm.Id, WorkspaceMoveDirection.Left);
        LoadWorkspaces();
    }

    [RelayCommand]
    private void MoveWorkspaceDown(WorkspaceSettingsViewModel? vm)
    {
        if (vm == null) return;
        _appModel.MoveWorkspace(vm.Id, WorkspaceMoveDirection.Right);
        LoadWorkspaces();
    }
}
