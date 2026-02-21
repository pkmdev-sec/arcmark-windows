using System.Windows;
using Arcmark.Services;
using Microsoft.Extensions.Logging;

namespace Arcmark;

public partial class App : Application
{
    /// <summary>Global access to the AppModel singleton.</summary>
    public static AppModel AppModel { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handling
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        // Logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger<App>();

        // Initialize core services
        var dataStore = new DataStore();
        AppModel = new AppModel(dataStore);

        logger.LogInformation("Arcmark started (v{Version})",
            GetType().Assembly.GetName().Version);

        // Create and show the main window
        var mainWindow = new Views.MainWindow();
        mainWindow.Show();

        // Check for updates in the background (non-blocking)
        _ = UpdateService.CheckForUpdatesAsync();
    }

    private void OnDispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}",
            "Arcmark — Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        MessageBox.Show(
            $"A fatal error occurred:\n\n{ex?.Message ?? e.ExceptionObject?.ToString()}",
            "Arcmark — Fatal Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
