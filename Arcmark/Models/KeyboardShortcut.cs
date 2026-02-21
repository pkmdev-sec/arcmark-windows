using System.Windows.Input;

namespace Arcmark.Models;

/// <summary>A keyboard shortcut composed of a key and optional modifier keys.</summary>
public class KeyboardShortcut
{
    public Key Key { get; init; }
    public ModifierKeys Modifiers { get; init; }

    /// <summary>
    /// Human-readable string such as "Ctrl+Shift+A".
    /// Modifier order follows Windows conventions: Ctrl → Alt → Shift → Win.
    /// </summary>
    public string DisplayString
    {
        get
        {
            var parts = new List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))     parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))   parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

            parts.Add(Key.ToString());

            return string.Join("+", parts);
        }
    }

    /// <summary>Default shortcut to toggle the sidebar: Ctrl+Shift+A.</summary>
    public static KeyboardShortcut DefaultToggleSidebar { get; } = new()
    {
        Key       = Key.A,
        Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
    };

    /// <summary>
    /// Parses a shortcut string like "Ctrl+Shift+A" into a <see cref="KeyboardShortcut"/>.
    /// Returns <c>null</c> if the string is empty or no valid key is found.
    /// </summary>
    public static KeyboardShortcut? Parse(string shortcutStr)
    {
        if (string.IsNullOrEmpty(shortcutStr)) return null;

        var parts = shortcutStr.Split('+');
        var modifiers = ModifierKeys.None;
        Key key = Key.None;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            switch (trimmed.ToLowerInvariant())
            {
                case "ctrl":  modifiers |= ModifierKeys.Control; break;
                case "alt":   modifiers |= ModifierKeys.Alt;     break;
                case "shift": modifiers |= ModifierKeys.Shift;   break;
                case "win":   modifiers |= ModifierKeys.Windows; break;
                default:
                    if (Enum.TryParse<Key>(trimmed, true, out var k))
                        key = k;
                    break;
            }
        }

        return key == Key.None ? null : new KeyboardShortcut { Key = key, Modifiers = modifiers };
    }
}
