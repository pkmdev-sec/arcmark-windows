using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using Arcmark.Utilities.Win32;

namespace Arcmark.Services;

/// <summary>
/// Browser detection and URL launching for Windows.
/// Uses the Windows registry to discover installed and default browsers.
/// </summary>
public static class BrowserManager
{
    private static readonly string[] BrowserRegistryRoots =
    {
        @"SOFTWARE\Clients\StartMenuInternet",
        @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet"
    };

    /// <summary>
    /// Returns a list of installed browsers discovered from the Windows registry.
    /// Each entry is (Name, ExePath).
    /// </summary>
    public static IReadOnlyList<(string Name, string ExePath)> GetInstalledBrowsers()
    {
        var browsers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in BrowserRegistryRoots)
        {
            using var hklm = Registry.LocalMachine.OpenSubKey(root);
            if (hklm == null) continue;

            foreach (var browserKey in hklm.GetSubKeyNames())
            {
                using var browserSubKey = hklm.OpenSubKey(browserKey);
                if (browserSubKey == null) continue;

                var name = browserSubKey.GetValue(null) as string ?? browserKey;
                using var commandKey = browserSubKey.OpenSubKey(@"shell\open\command");
                var command = commandKey?.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(command)) continue;

                var exePath = ParseExePath(command);
                if (!string.IsNullOrEmpty(exePath) && !browsers.ContainsKey(name))
                    browsers[name] = exePath;
            }
        }

        return browsers
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    /// <summary>
    /// Returns the executable path of the system default browser, or null if it cannot be determined.
    /// </summary>
    public static string? GetDefaultBrowserPath()
    {
        try
        {
            // Read the ProgId for http in HKCU
            using var userChoice = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
            var progId = userChoice?.GetValue("ProgId") as string;
            if (string.IsNullOrEmpty(progId)) return null;

            // Resolve ProgId → command
            using var commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
            var command = commandKey?.GetValue(null) as string;
            return string.IsNullOrWhiteSpace(command) ? null : ParseExePath(command);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Opens <paramref name="url"/> in the default (or user-configured) browser.
    /// Falls back to <see cref="Process.Start"/> with ShellExecute if the path is not found.
    /// </summary>
    public static void OpenUrl(string url)
    {
        var browserPath = UserSettings.Current.DefaultBrowserPath ?? GetDefaultBrowserPath();

        if (!string.IsNullOrEmpty(browserPath) && File.Exists(browserPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = browserPath,
                    Arguments = url,
                    UseShellExecute = false
                });
                return;
            }
            catch { /* fall through */ }
        }

        // Fallback: let Windows pick the handler
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BrowserManager.OpenUrl failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns true if a process with the given name is currently running.
    /// </summary>
    public static bool IsRunning(string processName) =>
        Process.GetProcessesByName(processName).Length > 0;

    /// <summary>
    /// Returns the process name of the foreground window, or null if it can't be determined.
    /// Uses Win32 GetForegroundWindow + GetWindowThreadProcessId.
    /// </summary>
    public static string? GetFrontmostApp()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return null;

            return Process.GetProcessById((int)pid)?.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the executable path from a shell command string.
    /// Handles both quoted ("C:\path\app.exe" %1) and unquoted (C:\path\app.exe %1) forms.
    /// </summary>
    private static string ParseExePath(string command)
    {
        command = command.Trim();
        if (command.StartsWith('"'))
        {
            var end = command.IndexOf('"', 1);
            return end > 0 ? command[1..end] : command.Trim('"');
        }

        var spaceIdx = command.IndexOf(' ');
        return spaceIdx > 0 ? command[..spaceIdx] : command;
    }
}
