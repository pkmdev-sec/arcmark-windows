using System;
using System.Windows;
using Velopack;

namespace Arcmark;

/// <summary>
/// Application entry point. Velopack must run before WPF initializes
/// to handle install/uninstall/update lifecycle hooks.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // ── Velopack bootstrap ──────────────────────────────────────────
        // This MUST be the very first thing that runs.
        // When the app is launched by the Velopack installer (install,
        // uninstall, or update), it handles those lifecycle events and
        // exits immediately. During normal launches it's a no-op.
        VelopackApp.Build().Run();

        // ── Normal WPF startup ──────────────────────────────────────────
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
