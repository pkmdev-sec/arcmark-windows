using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Arcmark.ViewModels;

namespace Arcmark.Views.Controls;

public partial class NodeListControl : UserControl
{
    // ── Dependency properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty NodesProperty =
        DependencyProperty.Register(nameof(Nodes), typeof(ObservableCollection<NodeViewModel>),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty WorkspaceBackgroundProperty =
        DependencyProperty.Register(nameof(WorkspaceBackground), typeof(SolidColorBrush),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty OpenLinkCommandProperty =
        DependencyProperty.Register(nameof(OpenLinkCommand), typeof(ICommand),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty DeleteNodeCommandProperty =
        DependencyProperty.Register(nameof(DeleteNodeCommand), typeof(ICommand),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty RenameNodeCommandProperty =
        DependencyProperty.Register(nameof(RenameNodeCommand), typeof(ICommand),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty CommitRenameNodeCommandProperty =
        DependencyProperty.Register(nameof(CommitRenameNodeCommand), typeof(ICommand),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ToggleFolderExpandedCommandProperty =
        DependencyProperty.Register(nameof(ToggleFolderExpandedCommand), typeof(ICommand),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty PinLinkCommandProperty =
        DependencyProperty.Register(nameof(PinLinkCommand), typeof(ICommand),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty MoveNodeToWorkspaceCommandProperty =
        DependencyProperty.Register(nameof(MoveNodeToWorkspaceCommand), typeof(ICommand),
            typeof(NodeListControl), new PropertyMetadata(null));

    public static readonly DependencyProperty OtherWorkspacesProperty =
        DependencyProperty.Register(nameof(OtherWorkspaces), typeof(ObservableCollection<WorkspaceViewModel>),
            typeof(NodeListControl), new PropertyMetadata(null));

    // ── CLR wrappers ──────────────────────────────────────────────────────────

    public ObservableCollection<NodeViewModel>? Nodes
    {
        get => (ObservableCollection<NodeViewModel>?)GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    public SolidColorBrush? WorkspaceBackground
    {
        get => (SolidColorBrush?)GetValue(WorkspaceBackgroundProperty);
        set => SetValue(WorkspaceBackgroundProperty, value);
    }

    public ICommand? OpenLinkCommand
    {
        get => (ICommand?)GetValue(OpenLinkCommandProperty);
        set => SetValue(OpenLinkCommandProperty, value);
    }

    public ICommand? DeleteNodeCommand
    {
        get => (ICommand?)GetValue(DeleteNodeCommandProperty);
        set => SetValue(DeleteNodeCommandProperty, value);
    }

    public ICommand? RenameNodeCommand
    {
        get => (ICommand?)GetValue(RenameNodeCommandProperty);
        set => SetValue(RenameNodeCommandProperty, value);
    }

    public ICommand? CommitRenameNodeCommand
    {
        get => (ICommand?)GetValue(CommitRenameNodeCommandProperty);
        set => SetValue(CommitRenameNodeCommandProperty, value);
    }

    public ICommand? ToggleFolderExpandedCommand
    {
        get => (ICommand?)GetValue(ToggleFolderExpandedCommandProperty);
        set => SetValue(ToggleFolderExpandedCommandProperty, value);
    }

    public ICommand? PinLinkCommand
    {
        get => (ICommand?)GetValue(PinLinkCommandProperty);
        set => SetValue(PinLinkCommandProperty, value);
    }

    public ICommand? MoveNodeToWorkspaceCommand
    {
        get => (ICommand?)GetValue(MoveNodeToWorkspaceCommandProperty);
        set => SetValue(MoveNodeToWorkspaceCommandProperty, value);
    }

    public ObservableCollection<WorkspaceViewModel>? OtherWorkspaces
    {
        get => (ObservableCollection<WorkspaceViewModel>?)GetValue(OtherWorkspacesProperty);
        set => SetValue(OtherWorkspacesProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public NodeListControl()
    {
        InitializeComponent();
    }

    // ── Row interaction ───────────────────────────────────────────────────────

    private NodeViewModel? GetNodeVm(object sender)
        => (sender as FrameworkElement)?.DataContext as NodeViewModel;

    private void OnRowMouseEnter(object sender, MouseEventArgs e)
    {
        if (GetNodeVm(sender) is { } vm) vm.IsHovered = true;
    }

    private void OnRowMouseLeave(object sender, MouseEventArgs e)
    {
        if (GetNodeVm(sender) is { } vm) vm.IsHovered = false;
    }

    private void OnRowClicked(object sender, MouseButtonEventArgs e)
    {
        var vm = GetNodeVm(sender);
        if (vm == null) return;

        if (vm.IsFolder)
        {
            ToggleFolderExpandedCommand?.Execute(vm.Id);
        }
        else if (!vm.IsRenaming)
        {
            OpenLinkCommand?.Execute(vm.Id);
        }
        e.Handled = true;
    }

    private void OnFolderChevronClicked(object sender, MouseButtonEventArgs e)
    {
        var vm = GetNodeVm(sender);
        if (vm?.IsFolder == true)
        {
            ToggleFolderExpandedCommand?.Execute(vm.Id);
            e.Handled = true;
        }
    }

    private void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        // Command binding handles deletion; stop event from triggering row click
        e.Handled = true;
    }

    // ── Inline rename ─────────────────────────────────────────────────────────

    private void OnRenameCommit(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && GetNodeVm(tb) is { } vm)
        {
            CommitRenameNodeCommand?.Execute((vm.Id, tb.Text));
        }
    }

    private void OnRenameKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && GetNodeVm(tb) is { } vm)
        {
            if (e.Key == Key.Return)
            {
                CommitRenameNodeCommand?.Execute((vm.Id, tb.Text));
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                vm.IsRenaming = false;
                e.Handled = true;
            }
        }
    }

    // ── Scroll shadow visibility ──────────────────────────────────────────────

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (TopShadow != null)
            TopShadow.Opacity = NodeScrollViewer.VerticalOffset > 2 ? 1 : 0;

        if (BottomShadow != null)
        {
            bool atBottom = NodeScrollViewer.VerticalOffset >=
                            NodeScrollViewer.ScrollableHeight - 2;
            BottomShadow.Opacity = atBottom ? 0 : 1;
        }
    }
}
