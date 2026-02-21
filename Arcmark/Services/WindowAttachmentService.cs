namespace Arcmark.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Arcmark.Utilities.Win32;
using Arcmark.Models;

// ---------------------------------------------------------------------------
// Supporting types
// ---------------------------------------------------------------------------

/// <summary>
/// Delegate used by <see cref="WindowAttachmentService"/> to ask the host
/// application what it should do when the tracked browser window changes state.
/// </summary>
public interface ISidebarAttachmentDelegate
{
    /// <summary>Called when Arcmark should reposition itself.</summary>
    void ShouldPositionWindow(Rect frame);

    /// <summary>Called when Arcmark should hide itself (e.g. browser minimised).</summary>
    void ShouldHideWindow();

    /// <summary>Called when Arcmark should become visible again.</summary>
    void ShouldShowWindow();
}

// ---------------------------------------------------------------------------
// Service
// ---------------------------------------------------------------------------

/// <summary>
/// Attaches the Arcmark sidebar to a browser window, mirroring the logic of
/// the macOS <c>WindowAttachmentService.swift</c> using Win32 WinEvent hooks.
/// </summary>
public sealed class WindowAttachmentService : IDisposable
{
    // -----------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------

    public static WindowAttachmentService Instance { get; } = new();

    private WindowAttachmentService() { }

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------

    private string?            _browserProcessName;
    private SidebarPosition    _position = SidebarPosition.Right;
    private IntPtr             _browserHwnd;
    private bool               _enabled;
    private bool               _disposed;

    // WinEvent hook handles – keep alive so GC doesn't collect the delegates
    private readonly List<IntPtr>                     _hookHandles  = new();
    private          NativeMethods.WinEventDelegate?  _winEventProc; // prevent GC

    // Debounce timer
    private DispatcherTimer? _debounceTimer;

    /// <summary>Minimum browser window width (pixels) before we hide.</summary>
    private const int MinBrowserWidth = 600;

    public ISidebarAttachmentDelegate? Delegate { get; set; }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Start tracking <paramref name="browserProcessName"/>.</summary>
    public void Enable(string browserProcessName, SidebarPosition position)
    {
        Disable();

        _browserProcessName = browserProcessName;
        _position           = position;
        _enabled            = true;

        InstallHooks();
        ForceUpdate();
    }

    /// <summary>Stop tracking and unhook all WinEvent hooks.</summary>
    public void Disable()
    {
        _enabled            = false;
        _browserProcessName = null;
        _browserHwnd        = IntPtr.Zero;

        RemoveHooks();
        _debounceTimer?.Stop();
        _debounceTimer = null;
    }

    /// <summary>Immediately recompute the sidebar frame.</summary>
    public void ForceUpdate()
    {
        if (!_enabled) return;

        _browserHwnd = FindBrowserWindow();
        if (_browserHwnd == IntPtr.Zero)
        {
            Delegate?.ShouldHideWindow();
            return;
        }

        UpdateSidebarPosition();
    }

    // -----------------------------------------------------------------------
    // WinEvent hooks
    // -----------------------------------------------------------------------

    private void InstallHooks()
    {
        // Single delegate instance so every hook shares the same GC root.
        _winEventProc = OnWinEvent;

        uint flags = NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS;

        // Position / size changes
        AddHook(NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
                NativeMethods.EVENT_OBJECT_LOCATIONCHANGE, flags);

        // Move-size end (coarser but cheap)
        AddHook(NativeMethods.EVENT_SYSTEM_MOVESIZEEND,
                NativeMethods.EVENT_SYSTEM_MOVESIZEEND, flags);

        // Window destroyed
        AddHook(NativeMethods.EVENT_OBJECT_DESTROY,
                NativeMethods.EVENT_OBJECT_DESTROY, flags);

        // Foreground change (app activation)
        AddHook(NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND, flags);

        // Minimise / restore
        AddHook(NativeMethods.EVENT_SYSTEM_MINIMIZESTART,
                NativeMethods.EVENT_SYSTEM_MINIMIZEEND, flags);
    }

    private void AddHook(uint eventMin, uint eventMax, uint flags)
    {
        var handle = NativeMethods.SetWinEventHook(
            eventMin, eventMax,
            IntPtr.Zero,
            _winEventProc!,
            0, 0,
            flags);

        if (handle != IntPtr.Zero)
            _hookHandles.Add(handle);
    }

    private void RemoveHooks()
    {
        foreach (var h in _hookHandles)
            NativeMethods.UnhookWinEvent(h);
        _hookHandles.Clear();
        _winEventProc = null;
    }

    // -----------------------------------------------------------------------
    // WinEvent callback
    // -----------------------------------------------------------------------

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint   eventType,
        IntPtr hwnd,
        int    idObject,
        int    idChild,
        uint   dwEventThread,
        uint   dwmsEventTime)
    {
        if (!_enabled) return;

        // Only act on events that originate from the browser window we're
        // tracking (or a foreground change that might reveal a new one).
        bool isBrowserEvent = hwnd == _browserHwnd;
        bool isForeground   = eventType == NativeMethods.EVENT_SYSTEM_FOREGROUND;

        if (!isBrowserEvent && !isForeground) return;

        switch (eventType)
        {
            case NativeMethods.EVENT_OBJECT_DESTROY:
                // Browser window was closed
                _browserHwnd = IntPtr.Zero;
                Dispatcher.CurrentDispatcher.BeginInvoke(() => Delegate?.ShouldHideWindow());
                break;

            case NativeMethods.EVENT_SYSTEM_MINIMIZESTART:
                Dispatcher.CurrentDispatcher.BeginInvoke(() => Delegate?.ShouldHideWindow());
                break;

            case NativeMethods.EVENT_SYSTEM_MINIMIZEEND:
                SchedulePositionUpdate();
                break;

            case NativeMethods.EVENT_SYSTEM_FOREGROUND:
                // Foreground window changed – re-evaluate which browser window
                // is currently active (handles browser tab changes etc.).
                SchedulePositionUpdate();
                break;

            default:
                // LOCATIONCHANGE / MOVESIZEEND
                SchedulePositionUpdate();
                break;
        }
    }

    // -----------------------------------------------------------------------
    // Debounced update
    // -----------------------------------------------------------------------

    private void SchedulePositionUpdate()
    {
        // Dispatch to UI thread; the DispatcherTimer must be created there.
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (_debounceTimer == null)
            {
                _debounceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16), // ~1 frame @ 60 Hz
                };
                _debounceTimer.Tick += (_, _) =>
                {
                    _debounceTimer.Stop();
                    UpdateSidebarPosition();
                };
            }

            // Reset so we only fire once after the last event in a burst.
            _debounceTimer.Stop();
            _debounceTimer.Start();
        });
    }

    // -----------------------------------------------------------------------
    // Core positioning logic
    // -----------------------------------------------------------------------

    private void UpdateSidebarPosition()
    {
        if (!_enabled) return;

        // Re-find the window on every update; the process might have opened a
        // new top-level window since we last checked.
        _browserHwnd = FindBrowserWindow();

        if (_browserHwnd == IntPtr.Zero)
        {
            Delegate?.ShouldHideWindow();
            return;
        }

        if (NativeMethods.IsIconic(_browserHwnd))
        {
            Delegate?.ShouldHideWindow();
            return;
        }

        var browserFrame = GetWindowFrame(_browserHwnd);
        if (browserFrame == Rect.Empty)
        {
            Delegate?.ShouldHideWindow();
            return;
        }

        if (browserFrame.Width < MinBrowserWidth)
        {
            Delegate?.ShouldHideWindow();
            return;
        }

        var sidebarFrame = CalculateArcmarkFrame(browserFrame);
        if (sidebarFrame == Rect.Empty) return;

        Delegate?.ShouldPositionWindow(sidebarFrame);
        Delegate?.ShouldShowWindow();
    }

    // -----------------------------------------------------------------------
    // Browser window discovery
    // -----------------------------------------------------------------------

    /// <summary>
    /// Enumerate all visible top-level windows and return the first one whose
    /// owning process matches <see cref="_browserProcessName"/>.
    /// </summary>
    private IntPtr FindBrowserWindow()
    {
        if (string.IsNullOrEmpty(_browserProcessName))
            return IntPtr.Zero;

        IntPtr found = IntPtr.Zero;

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);

            string exeName = GetProcessExecutableName(pid);
            if (string.IsNullOrEmpty(exeName)) return true;

            if (exeName.StartsWith(_browserProcessName, StringComparison.OrdinalIgnoreCase))
            {
                // Prefer the foreground window if it belongs to this process.
                if (NativeMethods.GetForegroundWindow() == hWnd)
                {
                    found = hWnd;
                    return false; // stop enumeration
                }

                // Otherwise keep the first visible match.
                if (found == IntPtr.Zero)
                    found = hWnd;
            }

            return true; // continue
        }, IntPtr.Zero);

        return found;
    }

    private static string GetProcessExecutableName(uint pid)
    {
        var hProcess = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false, pid);

        if (hProcess == IntPtr.Zero) return string.Empty;

        try
        {
            var sb = new StringBuilder(1024);
            NativeMethods.GetModuleFileNameEx(hProcess, IntPtr.Zero, sb, sb.Capacity);
            return System.IO.Path.GetFileNameWithoutExtension(sb.ToString());
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }

    // -----------------------------------------------------------------------
    // Frame calculation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the window rect in device-independent pixels (DIPs), taking
    /// per-monitor DPI scaling into account.
    /// </summary>
    private static Rect GetWindowFrame(IntPtr hWnd)
    {
        if (!NativeMethods.GetWindowRect(hWnd, out var rect))
            return Rect.Empty;

        double dpi = GetDpiForWindow(hWnd);
        double scale = dpi / 96.0;

        return new Rect(
            rect.Left   / scale,
            rect.Top    / scale,
            rect.Width  / scale,
            rect.Height / scale);
    }

    private static double GetDpiForWindow(IntPtr hWnd)
    {
        uint dpi = NativeMethods.GetDpiForWindow(hWnd);
        return dpi > 0 ? dpi : 96;
    }

    /// <summary>
    /// Computes the Arcmark sidebar frame relative to <paramref name="browserFrame"/>.
    /// Mirrors the macOS calculation: the sidebar sits flush against the left
    /// or right edge of the browser window (outside, not overlapping).
    /// </summary>
    private Rect CalculateArcmarkFrame(Rect browserFrame)
    {
        const double sidebarWidth = 340; // Match default window width

        // Position sidebar flush outside the browser window edge
        double x = _position switch
        {
            SidebarPosition.Right => browserFrame.Right,
            SidebarPosition.Left  => browserFrame.Left - sidebarWidth,
            _ => browserFrame.Right
        };

        // Match browser height exactly (same as macOS)
        double y = browserFrame.Top;
        double height = browserFrame.Height;

        // Check screen bounds — get the working area of the nearest monitor
        // For now use SystemParameters; a more robust version would use
        // MonitorFromWindow + GetMonitorInfo.
        var screenWidth = SystemParameters.WorkArea.Width;
        var screenLeft  = SystemParameters.WorkArea.Left;

        // If sidebar would go off-screen, hide instead
        if (x < screenLeft || x + sidebarWidth > screenLeft + screenWidth)
        {
            Delegate?.ShouldHideWindow();
            return Rect.Empty;
        }

        return new Rect(x, y, sidebarWidth, height);
    }

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Disable();
    }
}
