using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Arcmark.ViewModels;

namespace Arcmark.Views.Controls;

public partial class PinnedTabsGrid : UserControl
{
    // ── Dependency properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty PinnedLinksProperty =
        DependencyProperty.Register(nameof(PinnedLinks), typeof(ObservableCollection<LinkViewModel>),
            typeof(PinnedTabsGrid), new PropertyMetadata(null));

    public static readonly DependencyProperty OpenPinnedLinkCommandProperty =
        DependencyProperty.Register(nameof(OpenPinnedLinkCommand), typeof(ICommand),
            typeof(PinnedTabsGrid), new PropertyMetadata(null));

    public static readonly DependencyProperty UnpinLinkCommandProperty =
        DependencyProperty.Register(nameof(UnpinLinkCommand), typeof(ICommand),
            typeof(PinnedTabsGrid), new PropertyMetadata(null));

    // ── CLR wrappers ──────────────────────────────────────────────────────────

    public ObservableCollection<LinkViewModel>? PinnedLinks
    {
        get => (ObservableCollection<LinkViewModel>?)GetValue(PinnedLinksProperty);
        set => SetValue(PinnedLinksProperty, value);
    }

    public ICommand? OpenPinnedLinkCommand
    {
        get => (ICommand?)GetValue(OpenPinnedLinkCommandProperty);
        set => SetValue(OpenPinnedLinkCommandProperty, value);
    }

    public ICommand? UnpinLinkCommand
    {
        get => (ICommand?)GetValue(UnpinLinkCommandProperty);
        set => SetValue(UnpinLinkCommandProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public PinnedTabsGrid()
    {
        InitializeComponent();
    }

    // ── Context menu handler ──────────────────────────────────────────────────

    private void OnUnpinClicked(object sender, RoutedEventArgs e)
    {
        // Walk up: MenuItem → ContextMenu → Button (PlacementTarget)
        if (sender is System.Windows.Controls.MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is System.Windows.Controls.Button btn &&
            btn.DataContext is Arcmark.ViewModels.LinkViewModel vm)
        {
            UnpinLinkCommand?.Execute(vm.Id);
        }
    }
}
