using Arcmark.Models;

namespace Arcmark.Services;

/// <summary>
/// Result returned from an import operation.
/// </summary>
public class ImportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<Workspace> Workspaces { get; init; } = new();
    public int WorkspaceCount { get; init; }
    public int LinkCount { get; init; }
    public int FolderCount { get; init; }

    public static ImportResult Failure(string message) =>
        new() { Success = false, ErrorMessage = message };

    public override string ToString() =>
        Success
            ? $"Imported {WorkspaceCount} workspace(s), {LinkCount} link(s), {FolderCount} folder(s)."
            : $"Import failed: {ErrorMessage}";
}
