using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arcmark.Models;

[JsonConverter(typeof(NodeJsonConverter))]
public abstract class Node
{
    public abstract Guid Id { get; set; }

    /// <summary>Display name for UI — folder name or link title.</summary>
    [JsonIgnore]
    public string DisplayName => this switch
    {
        FolderNode fn => fn.Folder.Name,
        LinkNode ln => ln.Link.Title,
        _ => ""
    };
}

public class FolderNode : Node
{
    public override Guid Id
    {
        get => Folder.Id;
        set => Folder.Id = value;
    }

    public Folder Folder { get; set; } = new();
}

public class LinkNode : Node
{
    public override Guid Id
    {
        get => Link.Id;
        set => Link.Id = value;
    }

    public Link Link { get; set; } = new();
}

public class NodeJsonConverter : JsonConverter<Node>
{
    public override Node Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("Node JSON is missing required 'type' property.");

        var type = typeProp.GetString();

        return type switch
        {
            "folder" => new FolderNode
            {
                Folder = root.TryGetProperty("folder", out var folderEl)
                    ? JsonSerializer.Deserialize<Folder>(folderEl.GetRawText(), options)
                        ?? throw new JsonException("Failed to deserialize 'folder' property.")
                    : throw new JsonException("Node with type 'folder' is missing 'folder' property.")
            },
            "link" => new LinkNode
            {
                Link = root.TryGetProperty("link", out var linkEl)
                    ? JsonSerializer.Deserialize<Link>(linkEl.GetRawText(), options)
                        ?? throw new JsonException("Failed to deserialize 'link' property.")
                    : throw new JsonException("Node with type 'link' is missing 'link' property.")
            },
            _ => throw new JsonException($"Unknown Node type: '{type}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case FolderNode folderNode:
                writer.WriteString("type", "folder");
                writer.WritePropertyName("folder");
                JsonSerializer.Serialize(writer, folderNode.Folder, options);
                break;

            case LinkNode linkNode:
                writer.WriteString("type", "link");
                writer.WritePropertyName("link");
                JsonSerializer.Serialize(writer, linkNode.Link, options);
                break;

            default:
                throw new JsonException($"Unknown Node subtype: '{value.GetType().Name}'.");
        }

        writer.WriteEndObject();
    }
}
