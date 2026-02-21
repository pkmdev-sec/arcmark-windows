using System.Text.Json;
using Arcmark.Models;

namespace Arcmark.Services;

/// <summary>
/// Persistence service that saves/loads AppState as JSON to %LOCALAPPDATA%\Arcmark\data.json.
/// </summary>
public class DataStore
{
    private readonly string _baseDirectory;
    private readonly string _dataPath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new NodeJsonConverter() }
    };

    public DataStore(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Arcmark");

        _dataPath = Path.Combine(_baseDirectory, "data.json");
    }

    /// <summary>
    /// Loads AppState from disk. Returns a default state if the file doesn't exist or can't be parsed.
    /// </summary>
    public AppState Load()
    {
        EnsureDirectories();

        if (!File.Exists(_dataPath))
        {
            var defaultState = DefaultState();
            Save(defaultState);
            return defaultState;
        }

        try
        {
            var json = File.ReadAllText(_dataPath);
            var state = JsonSerializer.Deserialize<AppState>(json, SerializerOptions);
            return state ?? DefaultState();
        }
        catch
        {
            var fallback = DefaultState();
            Save(fallback);
            return fallback;
        }
    }

    /// <summary>
    /// Saves AppState to disk as pretty-printed JSON with sorted keys.
    /// </summary>
    public void Save(AppState state)
    {
        EnsureDirectories();

        try
        {
            // Use sorted keys by serializing with a custom approach:
            // System.Text.Json doesn't have a built-in sorted keys option,
            // so we serialize normally (camelCase) and sort via JsonDocument round-trip.
            var rawJson = JsonSerializer.Serialize(state, SerializerOptions);
            var sortedJson = SortJsonKeys(rawJson);
            File.WriteAllText(_dataPath, sortedJson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DataStore: CRITICAL - Failed to save state: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the path to the Icons subdirectory, creating it if needed.
    /// </summary>
    public string IconsDirectory()
    {
        var iconsPath = Path.Combine(_baseDirectory, "Icons");
        if (!Directory.Exists(iconsPath))
            Directory.CreateDirectory(iconsPath);
        return iconsPath;
    }

    /// <summary>
    /// Creates the initial AppState with one "Inbox" workspace.
    /// </summary>
    public static AppState DefaultState()
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Inbox",
            ColorId = WorkspaceColorId.Sky,
            Items = new List<Node>(),
            PinnedLinks = new List<Link>()
        };

        return new AppState
        {
            SchemaVersion = 2,
            Workspaces = new List<Workspace> { workspace },
            SelectedWorkspaceId = workspace.Id,
            IsSettingsSelected = false
        };
    }

    private void EnsureDirectories()
    {
        if (!Directory.Exists(_baseDirectory))
            Directory.CreateDirectory(_baseDirectory);
    }

    /// <summary>
    /// Round-trips through JsonDocument to produce sorted keys output.
    /// </summary>
    private static string SortJsonKeys(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var options = new JsonWriterOptions { Indented = true };
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);
        WriteSorted(writer, doc.RootElement);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteSorted(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(prop.Name);
                    WriteSorted(writer, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteSorted(writer, item);
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}
