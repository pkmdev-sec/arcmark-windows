using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Arcmark.Views.Controls;

public partial class PasteButton : UserControl
{
    // ── Dependency properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty PasteLinksCommandProperty =
        DependencyProperty.Register(nameof(PasteLinksCommand), typeof(ICommand),
            typeof(PasteButton), new PropertyMetadata(null));

    public static readonly DependencyProperty CreateFolderCommandProperty =
        DependencyProperty.Register(nameof(CreateFolderCommand), typeof(ICommand),
            typeof(PasteButton), new PropertyMetadata(null));

    // ── CLR wrappers ──────────────────────────────────────────────────────────

    public ICommand? PasteLinksCommand
    {
        get => (ICommand?)GetValue(PasteLinksCommandProperty);
        set => SetValue(PasteLinksCommandProperty, value);
    }

    public ICommand? CreateFolderCommand
    {
        get => (ICommand?)GetValue(CreateFolderCommandProperty);
        set => SetValue(CreateFolderCommandProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public PasteButton()
    {
        InitializeComponent();
    }
}
