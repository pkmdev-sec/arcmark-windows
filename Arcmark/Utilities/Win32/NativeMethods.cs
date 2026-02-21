namespace Arcmark.Utilities.Win32;

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class NativeMethods
{
    // -------------------------------------------------------------------------
    // Window management
    // -------------------------------------------------------------------------

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>Returns true when the window is minimised (iconic).</summary>
    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    /// <summary>Returns true when the window is maximised (zoomed).</summary>
    [DllImport("user32.dll")]
    public static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // -------------------------------------------------------------------------
    // WinEvent hooks – track window movement / destruction
    // -------------------------------------------------------------------------

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    // -------------------------------------------------------------------------
    // Global hotkeys
    // -------------------------------------------------------------------------

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // -------------------------------------------------------------------------
    // DPI awareness
    // -------------------------------------------------------------------------

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    // -------------------------------------------------------------------------
    // Process info
    // -------------------------------------------------------------------------

    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("psapi.dll", CharSet = CharSet.Auto)]
    public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, int nSize);

    // -------------------------------------------------------------------------
    // Delegates
    // -------------------------------------------------------------------------

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    // -------------------------------------------------------------------------
    // SetWindowPos constants
    // -------------------------------------------------------------------------

    public static readonly IntPtr HWND_TOPMOST   = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);

    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_NOZORDER   = 0x0004;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_NOMOVE     = 0x0002;
    public const uint SWP_NOSIZE     = 0x0001;

    // -------------------------------------------------------------------------
    // WinEvent constants
    // -------------------------------------------------------------------------

    public const uint EVENT_OBJECT_LOCATIONCHANGE  = 0x800B;
    public const uint EVENT_SYSTEM_MOVESIZEEND     = 0x000B;
    public const uint EVENT_OBJECT_DESTROY         = 0x8001;
    public const uint EVENT_SYSTEM_FOREGROUND      = 0x0003;
    public const uint EVENT_SYSTEM_MINIMIZESTART   = 0x0016;
    public const uint EVENT_SYSTEM_MINIMIZEEND     = 0x0017;

    public const uint WINEVENT_OUTOFCONTEXT  = 0x0000;
    public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    // -------------------------------------------------------------------------
    // ShowWindow commands
    // -------------------------------------------------------------------------

    public const int SW_HIDE    = 0;
    public const int SW_SHOW    = 5;
    public const int SW_RESTORE = 9;

    // -------------------------------------------------------------------------
    // Monitor constants
    // -------------------------------------------------------------------------

    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    // -------------------------------------------------------------------------
    // Process access rights
    // -------------------------------------------------------------------------

    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_VM_READ           = 0x0010;

    // -------------------------------------------------------------------------
    // Hotkey modifier flags
    // -------------------------------------------------------------------------

    public const uint MOD_ALT      = 0x0001;
    public const uint MOD_CONTROL  = 0x0002;
    public const uint MOD_SHIFT    = 0x0004;
    public const uint MOD_WIN      = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public const int WM_HOTKEY = 0x0312;

    // -------------------------------------------------------------------------
    // Structures
    // -------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width  => Right - Left;
        public readonly int Height => Bottom - Top;
    }
}
