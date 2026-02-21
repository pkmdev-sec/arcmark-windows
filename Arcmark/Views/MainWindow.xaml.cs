using System.Windows;
using System.Windows.Input;
using Arcmark.ViewModels;

namespace Arcmark.Views;

public partial class MainWindow : Window
{
    private MainViewModel _viewModel = null!;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Get the AppModel that was created in App.OnStartup
        var appModel = App.AppModel;
        _viewModel   = new MainViewModel(appModel);
        DataContext  = _viewModel;

        // Allow dragging the window by clicking the background
        MouseLeftButtonDown += OnWindowMouseDown;

        // Window state change
        StateChanged += OnStateChanged;
    }

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Only drag when clicking on the window chrome/background (not controls)
        if (e.OriginalSource is System.Windows.Controls.Grid or
            System.Windows.Shapes.Rectangle or
            Border { Name: "" })
        {
            DragMove();
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Minimize to taskbar on minimise; no special treatment needed
        if (WindowState == WindowState.Minimized)
        {
            // No-op — let Windows handle it
        }
    }

    // ── Global hotkey (Win+Shift+A to show/hide) ─────────────────────────────

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Register a global hotkey via Win32 if desired.
        // For now we expose a keyboard shortcut within the app.
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+T → new workspace
        if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.CreateWorkspaceCommand.Execute(null);
            e.Handled = true;
        }
        // Ctrl+F → focus search (handled by SearchBar)
        // Escape → clear search
        if (e.Key == Key.Escape && !string.IsNullOrEmpty(_viewModel.SearchQuery))
        {
            _viewModel.SearchQuery = string.Empty;
            e.Handled = true;
        }
        // Ctrl+V → paste links
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.PasteLinksCommand.Execute(null);
            e.Handled = true;
        }
    }

    // ── Always on top ─────────────────────────────────────────────────────────

    public void SetAlwaysOnTop(bool value)
    {
        Topmost = value;
    }
}
