namespace Arcmark.Models;

/// <summary>
/// Identifies a position within the node tree.
/// <para>
/// <see cref="ParentId"/> is <c>null</c> when the node lives at the workspace
/// root level; otherwise it is the <see cref="Folder.Id"/> of the containing
/// folder.
/// </para>
/// </summary>
public record NodeLocation(Guid? ParentId, int Index);
