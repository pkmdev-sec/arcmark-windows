using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arcmark.Services;

/// <summary>
/// Simple settings persistence using a JSON file at %LOCALAPPDATA%\Arcmark\settings.json.
/// Auto-saves on property change.
/// </summary>
public class UserSettings
{
    private static readonly Lazy<UserSettings> _instance =
        new(() => new UserSettings());

    public static UserSettings Current => _instance.Value;

    private readonly string _settingsPath;
    private SettingsData _data;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private UserSettings()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arcmark");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _settingsPath = Path.Combine(dir, "settings.json");
        _data = Load();
    }

    // Constructor for testing
    public UserSettings(string settingsPath)
    {
        _settingsPath = settingsPath;
        _data = Load();
    }

    public string? LastSelectedWorkspaceId
    {
        get => _data.LastSelectedWorkspaceId;
        set { _data.LastSelectedWorkspaceId = value; Save(); }
    }

    public bool AlwaysOnTopEnabled
    {
        get => _data.AlwaysOnTopEnabled;
        set { _data.AlwaysOnTopEnabled = value; Save(); }
    }

    public string? MainWindowSize
    {
        get => _data.MainWindowSize;
        set { _data.MainWindowSize = value; Save(); }
    }

    public bool SidebarAttachmentEnabled
    {
        get => _data.SidebarAttachmentEnabled;
        set { _data.SidebarAttachmentEnabled = value; Save(); }
    }

    public string SidebarPosition
    {
        get => _data.SidebarPosition;
        set { _data.SidebarPosition = value; Save(); }
    }

    public string? DefaultBrowserPath
    {
        get => _data.DefaultBrowserPath;
        set { _data.DefaultBrowserPath = value; Save(); }
    }

    /// <summary>
    /// Toggle sidebar shortcut. Stored as a string representation since the WPF
    /// KeyboardShortcut uses WPF-specific types (Key, ModifierKeys) that don't
    /// serialize trivially to JSON. Use <see cref="ToggleSidebarShortcutString"/>
    /// to persist the raw display string (e.g. "Ctrl+Shift+A").
    /// </summary>
    public string? ToggleSidebarShortcutString
    {
        get => _data.ToggleSidebarShortcutString;
        set { _data.ToggleSidebarShortcutString = value; Save(); }
    }

    private SettingsData Load()
    {
        if (!File.Exists(_settingsPath))
            return new SettingsData();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<SettingsData>(json, JsonOptions) ?? new SettingsData();
        }
        catch
        {
            return new SettingsData();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UserSettings: Failed to save: {ex.Message}");
        }
    }

    private class SettingsData
    {
        [JsonPropertyName("lastSelectedWorkspaceId")]
        public string? LastSelectedWorkspaceId { get; set; }

        [JsonPropertyName("alwaysOnTopEnabled")]
        public bool AlwaysOnTopEnabled { get; set; }

        [JsonPropertyName("mainWindowSize")]
        public string? MainWindowSize { get; set; }

        [JsonPropertyName("sidebarAttachmentEnabled")]
        public bool SidebarAttachmentEnabled { get; set; }

        [JsonPropertyName("sidebarPosition")]
        public string SidebarPosition { get; set; } = "right";

        [JsonPropertyName("defaultBrowserPath")]
        public string? DefaultBrowserPath { get; set; }

        [JsonPropertyName("toggleSidebarShortcut")]
        public string? ToggleSidebarShortcutString { get; set; }
    }
}
