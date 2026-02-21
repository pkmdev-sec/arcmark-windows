namespace Arcmark.Services;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Arcmark.Utilities.Win32;
using Arcmark.Models;

// ---------------------------------------------------------------------------
// Service
// ---------------------------------------------------------------------------

/// <summary>
/// Registers and unregisters a system-wide hotkey using the Win32
/// <c>RegisterHotKey</c> / <c>UnregisterHotKey</c> APIs, hooking into WPF's
/// <see cref="HwndSource"/> to receive <c>WM_HOTKEY</c> messages.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    // -----------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------

    public static GlobalHotkeyService Instance { get; } = new();

    private GlobalHotkeyService() { }

    // -----------------------------------------------------------------------
    // Events
    // -----------------------------------------------------------------------

    /// <summary>Raised on the UI thread when the registered hotkey is pressed.</summary>
    public event EventHandler? HotkeyTriggered;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------

    private HwndSource?       _source;
    private KeyboardShortcut? _currentShortcut;
    private bool              _registered;
    private bool              _disposed;

    // Arbitrary unique ID for our hotkey registration.
    private const int HotkeyId = 0xBEEF;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Registers <paramref name="shortcut"/> as a system-wide hotkey.
    /// Any previously registered hotkey is unregistered first.
    /// Must be called from the UI thread after the main window is loaded.
    /// </summary>
    public void Register(KeyboardShortcut shortcut)
    {
        Unregister();

        EnsureHwndSource();

        if (_source == null)
            throw new InvalidOperationException(
                "GlobalHotkeyService.Register must be called after the main window is initialised.");

        uint vk   = KeyToVirtualKey(shortcut.Key);
        uint mods = ModifierKeysToWin32(shortcut.Modifiers) | NativeMethods.MOD_NOREPEAT;

        bool ok = NativeMethods.RegisterHotKey(_source.Handle, HotkeyId, mods, vk);
        if (!ok)
            throw new InvalidOperationException(
                $"RegisterHotKey failed for {shortcut.Modifiers}+{shortcut.Key}. " +
                "Another application may have claimed the same combination.");

        _currentShortcut = shortcut;
        _registered      = true;
    }

    /// <summary>Unregisters the current hotkey (if any).</summary>
    public void Unregister()
    {
        if (!_registered || _source == null) return;

        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _registered      = false;
        _currentShortcut = null;
    }

    // -----------------------------------------------------------------------
    // HwndSource / message hook
    // -----------------------------------------------------------------------

    private void EnsureHwndSource()
    {
        if (_source != null) return;

        var mainWindow = Application.Current?.MainWindow
            ?? throw new InvalidOperationException("No main window available.");

        _source = PresentationSource.FromVisual(mainWindow) as HwndSource
            ?? throw new InvalidOperationException(
                "Could not obtain HwndSource from the main window.");

        _source.AddHook(WndProc);
        _source.Disposed += OnHwndSourceDisposed;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            HotkeyTriggered?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void OnHwndSourceDisposed(object? sender, EventArgs e)
    {
        _registered = false;
        _source     = null;
    }

    // -----------------------------------------------------------------------
    // Key / modifier conversion helpers
    // -----------------------------------------------------------------------

    private static uint ModifierKeysToWin32(ModifierKeys mods)
    {
        uint result = 0;

        if ((mods & ModifierKeys.Alt)     != 0) result |= NativeMethods.MOD_ALT;
        if ((mods & ModifierKeys.Control) != 0) result |= NativeMethods.MOD_CONTROL;
        if ((mods & ModifierKeys.Shift)   != 0) result |= NativeMethods.MOD_SHIFT;
        if ((mods & ModifierKeys.Windows) != 0) result |= NativeMethods.MOD_WIN;

        return result;
    }

    /// <summary>
    /// Maps WPF <see cref="Key"/> values to Win32 virtual-key codes.
    /// Covers letter keys, digits, function keys, and common special keys.
    /// </summary>
    private static uint KeyToVirtualKey(Key key)
    {
        // WPF exposes KeyInterop for exactly this purpose.
        int vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk > 0) return (uint)vk;

        // Fallback lookup for edge cases not handled by KeyInterop.
        return FallbackVkMap.TryGetValue(key, out uint fallback) ? fallback : 0u;
    }

    /// <summary>
    /// Additional VK mappings for keys that <see cref="KeyInterop"/> might
    /// not translate perfectly (e.g. OEM keys on non-US layouts).
    /// </summary>
    private static readonly Dictionary<Key, uint> FallbackVkMap = new()
    {
        [Key.Space]        = 0x20,
        [Key.Enter]        = 0x0D,
        [Key.Escape]       = 0x1B,
        [Key.Tab]          = 0x09,
        [Key.Back]         = 0x08,
        [Key.Delete]       = 0x2E,
        [Key.Insert]       = 0x2D,
        [Key.Home]         = 0x24,
        [Key.End]          = 0x23,
        [Key.PageUp]       = 0x21,
        [Key.PageDown]     = 0x22,
        [Key.Left]         = 0x25,
        [Key.Up]           = 0x26,
        [Key.Right]        = 0x27,
        [Key.Down]         = 0x28,
        [Key.OemSemicolon] = 0xBA,
        [Key.OemPlus]      = 0xBB,
        [Key.OemComma]     = 0xBC,
        [Key.OemMinus]     = 0xBD,
        [Key.OemPeriod]    = 0xBE,
        [Key.OemQuestion]  = 0xBF,
        [Key.OemTilde]     = 0xC0,
        [Key.OemOpenBrackets] = 0xDB,
        [Key.OemPipe]      = 0xDC,
        [Key.OemCloseBrackets] = 0xDD,
        [Key.OemQuotes]    = 0xDE,
    };

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Unregister();

        if (_source != null)
        {
            _source.RemoveHook(WndProc);
            _source.Disposed -= OnHwndSourceDisposed;
            _source = null;
        }
    }
}
