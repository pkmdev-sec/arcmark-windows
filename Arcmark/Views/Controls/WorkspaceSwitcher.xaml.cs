using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Arcmark.ViewModels;

namespace Arcmark.Views.Controls;

public partial class WorkspaceSwitcher : UserControl
{
    // ── Dependency properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty WorkspacesProperty =
        DependencyProperty.Register(nameof(Workspaces), typeof(ObservableCollection<WorkspaceViewModel>),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty IsSettingsSelectedProperty =
        DependencyProperty.Register(nameof(IsSettingsSelected), typeof(bool),
            typeof(WorkspaceSwitcher), new PropertyMetadata(false));

    public static readonly DependencyProperty SelectWorkspaceCommandProperty =
        DependencyProperty.Register(nameof(SelectWorkspaceCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectSettingsCommandProperty =
        DependencyProperty.Register(nameof(SelectSettingsCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty CreateWorkspaceCommandProperty =
        DependencyProperty.Register(nameof(CreateWorkspaceCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty RenameWorkspaceCommandProperty =
        DependencyProperty.Register(nameof(RenameWorkspaceCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty CommitRenameWorkspaceCommandProperty =
        DependencyProperty.Register(nameof(CommitRenameWorkspaceCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty DeleteWorkspaceCommandProperty =
        DependencyProperty.Register(nameof(DeleteWorkspaceCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty MoveWorkspaceLeftCommandProperty =
        DependencyProperty.Register(nameof(MoveWorkspaceLeftCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty MoveWorkspaceRightCommandProperty =
        DependencyProperty.Register(nameof(MoveWorkspaceRightCommand), typeof(ICommand),
            typeof(WorkspaceSwitcher), new PropertyMetadata(null));

    // ── CLR wrappers ──────────────────────────────────────────────────────────

    public ObservableCollection<WorkspaceViewModel>? Workspaces
    {
        get => (ObservableCollection<WorkspaceViewModel>?)GetValue(WorkspacesProperty);
        set => SetValue(WorkspacesProperty, value);
    }

    public bool IsSettingsSelected
    {
        get => (bool)GetValue(IsSettingsSelectedProperty);
        set => SetValue(IsSettingsSelectedProperty, value);
    }

    public ICommand? SelectWorkspaceCommand
    {
        get => (ICommand?)GetValue(SelectWorkspaceCommandProperty);
        set => SetValue(SelectWorkspaceCommandProperty, value);
    }

    public ICommand? SelectSettingsCommand
    {
        get => (ICommand?)GetValue(SelectSettingsCommandProperty);
        set => SetValue(SelectSettingsCommandProperty, value);
    }

    public ICommand? CreateWorkspaceCommand
    {
        get => (ICommand?)GetValue(CreateWorkspaceCommandProperty);
        set => SetValue(CreateWorkspaceCommandProperty, value);
    }

    public ICommand? RenameWorkspaceCommand
    {
        get => (ICommand?)GetValue(RenameWorkspaceCommandProperty);
        set => SetValue(RenameWorkspaceCommandProperty, value);
    }

    public ICommand? CommitRenameWorkspaceCommand
    {
        get => (ICommand?)GetValue(CommitRenameWorkspaceCommandProperty);
        set => SetValue(CommitRenameWorkspaceCommandProperty, value);
    }

    public ICommand? DeleteWorkspaceCommand
    {
        get => (ICommand?)GetValue(DeleteWorkspaceCommandProperty);
        set => SetValue(DeleteWorkspaceCommandProperty, value);
    }

    public ICommand? MoveWorkspaceLeftCommand
    {
        get => (ICommand?)GetValue(MoveWorkspaceLeftCommandProperty);
        set => SetValue(MoveWorkspaceLeftCommandProperty, value);
    }

    public ICommand? MoveWorkspaceRightCommand
    {
        get => (ICommand?)GetValue(MoveWorkspaceRightCommandProperty);
        set => SetValue(MoveWorkspaceRightCommandProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public WorkspaceSwitcher()
    {
        InitializeComponent();
    }

    // ── Scroll tab list with mouse wheel ─────────────────────────────────────

    private void OnTabScrollWheel(object sender, MouseWheelEventArgs e)
    {
        if (TabScrollViewer is null) return;
        TabScrollViewer.ScrollToHorizontalOffset(
            TabScrollViewer.HorizontalOffset - e.Delta * 0.5);
        e.Handled = true;
    }
}
