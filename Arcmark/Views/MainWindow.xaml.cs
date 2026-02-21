using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Arcmark.Models;
using Arcmark.Services;
using Arcmark.Utilities.Win32;
using Arcmark.ViewModels;

namespace Arcmark.Views;

public partial class MainWindow : Window, ISidebarAttachmentDelegate
{
    private MainViewModel _viewModel = null!;
    private bool _isAttached;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var appModel = App.AppModel;
        _viewModel = new MainViewModel(appModel);
        DataContext = _viewModel;

        MouseLeftButtonDown += OnWindowMouseDown;
        StateChanged += OnStateChanged;

        // Wire up sidebar attachment
        WindowAttachmentService.Instance.Delegate = this;
        if (UserSettings.Current.SidebarAttachmentEnabled)
            EnableAttachment();

        // Wire up global hotkey
        SetupGlobalHotkey();
    }

    // ── Sidebar Attachment ─────────────────────────────────────────────────

    public void EnableAttachment()
    {
        var processName = DetectBrowserProcessName();
        if (string.IsNullOrEmpty(processName)) return;

        var position = UserSettings.Current.SidebarPosition
            .Equals("left", StringComparison.OrdinalIgnoreCase)
            ? SidebarPosition.Left
            : SidebarPosition.Right;

        _isAttached = true;
        WindowAttachmentService.Instance.Enable(processName, position);
    }

    public void DisableAttachment()
    {
        _isAttached = false;
        WindowAttachmentService.Instance.Disable();
    }

    /// <summary>
    /// Detect the browser process name from settings or auto-detect common browsers.
    /// </summary>
    private static string DetectBrowserProcessName()
    {
        var browserPath = UserSettings.Current.DefaultBrowserPath;
        if (!string.IsNullOrEmpty(browserPath))
            return Path.GetFileNameWithoutExtension(browserPath);

        // Auto-detect: check which common browsers are running
        string[] commonBrowsers = ["msedge", "chrome", "brave", "firefox", "arc"];
        foreach (var name in commonBrowsers)
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(name);
            if (processes.Length > 0)
            {
                foreach (var p in processes) p.Dispose();
                return name;
            }
        }

        // Fallback to Edge (default on Windows)
        return "msedge";
    }

    // ── ISidebarAttachmentDelegate ─────────────────────────────────────────

    public void ShouldPositionWindow(Rect frame)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ShouldPositionWindow(frame));
            return;
        }

        // Account for the window margin (used for drop shadow)
        // The root Border has Margin="12,8,12,16" so actual content is inset
        Left = frame.X - 12;
        Top = frame.Y - 8;
        Width = frame.Width + 12 + 12;   // left + right margin
        Height = frame.Height + 8 + 16;  // top + bottom margin

        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
    }

    public void ShouldHideWindow()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(ShouldHideWindow);
            return;
        }

        if (_isAttached && IsVisible)
            Hide();
    }

    public void ShouldShowWindow()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(ShouldShowWindow);
            return;
        }

        if (_isAttached && !IsVisible)
        {
            Show();
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
        }
    }

    // ── Global Hotkey ──────────────────────────────────────────────────────

    private void SetupGlobalHotkey()
    {
        var shortcutStr = UserSettings.Current.ToggleSidebarShortcutString;
        if (string.IsNullOrEmpty(shortcutStr)) return;

        var shortcut = KeyboardShortcut.Parse(shortcutStr);
        if (shortcut != null)
        {
            GlobalHotkeyService.Instance.HotkeyTriggered += OnGlobalHotkeyPressed;
            GlobalHotkeyService.Instance.Register(shortcut);
        }
    }

    /// <summary>
    /// Re-registers the global hotkey after the user changes it in Settings.
    /// Called from SettingsViewModel.
    /// </summary>
    public void UpdateGlobalHotkey()
    {
        // Unregister old
        GlobalHotkeyService.Instance.HotkeyTriggered -= OnGlobalHotkeyPressed;
        GlobalHotkeyService.Instance.Unregister();

        // Register new (if set)
        SetupGlobalHotkey();
    }

    private void OnGlobalHotkeyPressed(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (IsVisible)
                Hide();
            else
            {
                Show();
                Activate();
            }
        });
    }

    // ── Window chrome drag ─────────────────────────────────────────────────

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isAttached) return; // Don't allow manual drag when attached

        if (e.OriginalSource is System.Windows.Controls.Grid or
            System.Windows.Shapes.Rectangle or
            Border { Name: "" })
        {
            DragMove();
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _isAttached)
        {
            // When attached, minimizing should just hide
            WindowState = WindowState.Normal;
            Hide();
        }
    }

    // ── Keyboard shortcuts ─────────────────────────────────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.CreateWorkspaceCommand.Execute(null);
            e.Handled = true;
        }
        if (e.Key == Key.Escape && !string.IsNullOrEmpty(_viewModel.SearchQuery))
        {
            _viewModel.SearchQuery = string.Empty;
            e.Handled = true;
        }
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.PasteLinksCommand.Execute(null);
            e.Handled = true;
        }
    }

    // ── Always on top ──────────────────────────────────────────────────────

    public void SetAlwaysOnTop(bool value) => Topmost = value;

    // ── Cleanup ────────────────────────────────────────────────────────────

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        GlobalHotkeyService.Instance.HotkeyTriggered -= OnGlobalHotkeyPressed;
        GlobalHotkeyService.Instance.Dispose();
        WindowAttachmentService.Instance.Dispose();
    }
}
