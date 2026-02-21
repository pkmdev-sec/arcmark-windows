using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Arcmark.Views.Controls;

/// <summary>
/// Keyboard shortcut capture control.
/// Click to start recording, press a key combination to set it.
/// Escape cancels recording. Clear button removes the shortcut.
/// </summary>
public partial class ShortcutRecorder : UserControl
{
    // ── Dependency Properties ──────────────────────────────────────────────────

    public static readonly DependencyProperty ShortcutTextProperty =
        DependencyProperty.Register(
            nameof(ShortcutText),
            typeof(string),
            typeof(ShortcutRecorder),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnShortcutTextChanged));

    public string ShortcutText
    {
        get => (string)GetValue(ShortcutTextProperty);
        set => SetValue(ShortcutTextProperty, value);
    }

    public static readonly DependencyProperty IsRecordingProperty =
        DependencyProperty.Register(
            nameof(IsRecording),
            typeof(bool),
            typeof(ShortcutRecorder),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsRecordingChanged));

    public bool IsRecording
    {
        get => (bool)GetValue(IsRecordingProperty);
        set => SetValue(IsRecordingProperty, value);
    }

    // ── Events ─────────────────────────────────────────────────────────────────

    public event EventHandler<(Key Key, ModifierKeys Modifiers)>? ShortcutCaptured;
    public event EventHandler? ShortcutCleared;

    public ShortcutRecorder()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    // ── Property change handlers ───────────────────────────────────────────────

    private static void OnShortcutTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShortcutRecorder recorder)
            recorder.UpdateDisplay();
    }

    private static void OnIsRecordingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShortcutRecorder recorder)
            recorder.UpdateDisplay();
    }

    // ── Display ────────────────────────────────────────────────────────────────

    private void UpdateDisplay()
    {
        if (IsRecording)
        {
            ShortcutLabel.Text = "Press shortcut...";
            ShortcutLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            RecordButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0x9D, 0xD0, 0xFF));
            RecordButton.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF));
            ClearButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            var hasShortcut = !string.IsNullOrWhiteSpace(ShortcutText);
            ShortcutLabel.Text = hasShortcut ? ShortcutText : "Click to set shortcut";
            ShortcutLabel.Foreground = hasShortcut
                ? new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27))
                : new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
            RecordButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB));
            RecordButton.Background = new SolidColorBrush(Colors.White);
            ClearButton.Visibility = hasShortcut ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        IsRecording = true;
        Focus(); // Capture keyboard events
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ShortcutText = string.Empty;
        IsRecording = false;
        ShortcutCleared?.Invoke(this, EventArgs.Empty);
        UpdateDisplay();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsRecording) return;

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Escape cancels recording
        if (key == Key.Escape)
        {
            IsRecording = false;
            UpdateDisplay();
            return;
        }

        // Ignore standalone modifier keys
        if (key is Key.LeftCtrl or Key.RightCtrl or
                  Key.LeftAlt or Key.RightAlt or
                  Key.LeftShift or Key.RightShift or
                  Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;

        // Require at least one modifier for a valid shortcut
        if (modifiers == ModifierKeys.None) return;

        // Build display string
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());

        ShortcutText = string.Join("+", parts);
        IsRecording = false;
        UpdateDisplay();

        ShortcutCaptured?.Invoke(this, (key, modifiers));
    }

    private void OnKeyDown(object sender, KeyEventArgs e) => OnKeyDown(e);
}
