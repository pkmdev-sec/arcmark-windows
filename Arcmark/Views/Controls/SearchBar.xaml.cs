using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Arcmark.Views.Controls;

public partial class SearchBar : UserControl
{
    // ── SearchQuery dependency property ───────────────────────────────────────

    public static readonly DependencyProperty SearchQueryProperty =
        DependencyProperty.Register(nameof(SearchQuery), typeof(string),
            typeof(SearchBar),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSearchQueryChanged));

    public string SearchQuery
    {
        get => (string)GetValue(SearchQueryProperty);
        set => SetValue(SearchQueryProperty, value);
    }

    private static void OnSearchQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchBar sb)
            sb.HasText = !string.IsNullOrEmpty(e.NewValue as string);
    }

    // ── HasText dependency property ───────────────────────────────────────────

    public static readonly DependencyProperty HasTextProperty =
        DependencyProperty.Register(nameof(HasText), typeof(bool), typeof(SearchBar),
            new PropertyMetadata(false));

    public bool HasText
    {
        get => (bool)GetValue(HasTextProperty);
        set => SetValue(HasTextProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public SearchBar()
    {
        InitializeComponent();
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        SearchQuery = string.Empty;
        SearchTextBox.Focus();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SearchQuery = string.Empty;
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

}
