using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Arcmark.Models;

namespace Arcmark.Views.Controls;

/// <summary>
/// Visual toggle for sidebar position (Left / Right).
/// Shows two mini browser-window diagrams — the selected one is highlighted.
/// </summary>
public partial class SidebarPositionSelector : UserControl
{
    public static readonly DependencyProperty SelectedPositionProperty =
        DependencyProperty.Register(
            nameof(SelectedPosition),
            typeof(SidebarPosition),
            typeof(SidebarPositionSelector),
            new FrameworkPropertyMetadata(SidebarPosition.Right,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedPositionChanged));

    public SidebarPosition SelectedPosition
    {
        get => (SidebarPosition)GetValue(SelectedPositionProperty);
        set => SetValue(SelectedPositionProperty, value);
    }

    public event EventHandler<SidebarPosition>? PositionChanged;

    public SidebarPositionSelector()
    {
        InitializeComponent();
        UpdateButtonStates();
    }

    private static void OnSelectedPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarPositionSelector selector)
            selector.UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        var selectedBorder = new SolidColorBrush(Color.FromRgb(0x9D, 0xD0, 0xFF)); // sky
        var selectedBackground = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF));
        var defaultBorder = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB));
        var defaultBackground = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));

        if (SelectedPosition == SidebarPosition.Left)
        {
            LeftButton.BorderBrush = selectedBorder;
            LeftButton.Background = selectedBackground;
            RightButton.BorderBrush = defaultBorder;
            RightButton.Background = defaultBackground;
        }
        else
        {
            RightButton.BorderBrush = selectedBorder;
            RightButton.Background = selectedBackground;
            LeftButton.BorderBrush = defaultBorder;
            LeftButton.Background = defaultBackground;
        }
    }

    private void LeftButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = SidebarPosition.Left;
        PositionChanged?.Invoke(this, SidebarPosition.Left);
    }

    private void RightButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = SidebarPosition.Right;
        PositionChanged?.Invoke(this, SidebarPosition.Right);
    }
}
