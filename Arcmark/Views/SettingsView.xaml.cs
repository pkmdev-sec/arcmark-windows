using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Arcmark.Models;
using Arcmark.Services;
using Arcmark.ViewModels;

namespace Arcmark.Views;

/// <summary>
/// Code-behind for the Settings panel UserControl.
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public SettingsView(AppModel appModel)
    {
        InitializeComponent();
        DataContext = new SettingsViewModel(appModel);
    }

    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    // ── Sidebar position ───────────────────────────────────────────────────────

    private void PositionSelector_PositionChanged(object sender, SidebarPosition position)
    {
        ViewModel?.ChangeSidebarPositionCommand.Execute(position);
    }

    // ── Browser selection ──────────────────────────────────────────────────────

    private void Browser_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is BrowserInfo browser)
            ViewModel?.SelectBrowserCommand.Execute(browser);
    }

    // ── Shortcut recorder ──────────────────────────────────────────────────────

    private void ShortcutRecorder_ShortcutCaptured(object sender,
        (System.Windows.Input.Key Key, ModifierKeys Modifiers) e)
    {
        ViewModel?.OnShortcutCaptured(e.Key, e.Modifiers);
    }

    private void ShortcutRecorder_ShortcutCleared(object sender, EventArgs e)
    {
        ViewModel?.ClearShortcutCommand.Execute(null);
    }

    // ── Workspace rename ───────────────────────────────────────────────────────

    private void RenameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Escape)
        {
            if (sender is TextBox tb && tb.DataContext is WorkspaceSettingsViewModel wsVm)
                ViewModel?.CommitRenameWorkspaceCommand.Execute(wsVm);
            e.Handled = true;
        }
    }

    // ── Color swatches ─────────────────────────────────────────────────────────

    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not WorkspaceSettingsViewModel wsVm) return;
        if (btn.Tag is not string colorName) return;

        if (!Enum.TryParse<WorkspaceColorId>(colorName, out var colorId)) return;

        ViewModel?.ChangeWorkspaceColorCommand.Execute((wsVm, colorId));
    }

    // ── Hyperlink navigation ───────────────────────────────────────────────────

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hyperlink navigate failed: {ex.Message}");
        }
        e.Handled = true;
    }
}
