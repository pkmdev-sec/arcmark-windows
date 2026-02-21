using System;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace Arcmark.Services;

/// <summary>
/// Handles automatic updates via Velopack, equivalent to Sparkle on macOS.
/// Checks GitHub Releases for new versions, downloads delta updates in the
/// background, and prompts the user to restart.
/// </summary>
public static class UpdateService
{
    // ── Configuration ──────────────────────────────────────────────────
    // Point this at your GitHub repository. Velopack reads the Releases
    // page automatically — no separate appcast.xml needed.
    private const string GitHubRepoUrl = "https://github.com/Geek-1001/arcmark-windows";

    /// <summary>
    /// Checks for updates in the background. Safe to call on startup —
    /// it's fully async and non-blocking. Silently does nothing if:
    /// - No network connection
    /// - Already on the latest version
    /// - Running in development (not installed via Velopack)
    /// </summary>
    public static async Task CheckForUpdatesAsync()
    {
        try
        {
            var source = new GithubSource(GitHubRepoUrl, accessToken: null, prerelease: false);
            var mgr = new UpdateManager(source);

            // In development builds (not installed via Velopack), this returns false
            if (!mgr.IsInstalled)
                return;

            // Check for a newer version
            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo is null)
                return; // Already up to date

            // Download the update (delta if available — typically tiny)
            await mgr.DownloadUpdatesAsync(updateInfo);

            // Prompt the user to restart
            var result = MessageBox.Show(
                $"Arcmark {updateInfo.TargetFullRelease.Version} is available.\n\n" +
                "Would you like to restart and update now?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                mgr.ApplyUpdatesAndRestart(updateInfo);
            }
            // If No, the update will be applied on next launch automatically
        }
        catch
        {
            // Silently ignore update errors — the app should always work
            // even if the update check fails (no network, rate limited, etc.)
        }
    }

    /// <summary>
    /// Manually triggered update check (e.g., from Settings → "Check for Updates").
    /// Shows UI feedback even when no update is available.
    /// </summary>
    public static async Task CheckForUpdatesManualAsync()
    {
        try
        {
            var source = new GithubSource(GitHubRepoUrl, accessToken: null, prerelease: false);
            var mgr = new UpdateManager(source);

            if (!mgr.IsInstalled)
            {
                MessageBox.Show(
                    "Update checking is only available in installed builds.\n\n" +
                    "You're running a development build.",
                    "Updates",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo is null)
            {
                MessageBox.Show(
                    "You're running the latest version of Arcmark.",
                    "No Updates Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            await mgr.DownloadUpdatesAsync(updateInfo);

            var result = MessageBox.Show(
                $"Arcmark {updateInfo.TargetFullRelease.Version} is available.\n\n" +
                "Would you like to restart and update now?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                mgr.ApplyUpdatesAndRestart(updateInfo);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to check for updates:\n\n{ex.Message}",
                "Update Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
