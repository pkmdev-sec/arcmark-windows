using Xunit;
using Arcmark.Models;
using Arcmark.Services;

namespace Arcmark.Tests;

public class ModelTests
{
    private AppModel CreateModel()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ArcmarkTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var store = new DataStore(tempDir);
        return new AppModel(store);
    }

    [Fact]
    public void DefaultState_HasOneWorkspace()
    {
        var model = CreateModel();
        Assert.Single(model.Workspaces);
        Assert.Equal("Inbox", model.CurrentWorkspace.Name);
    }

    [Fact]
    public void CreateWorkspace_AddsAndSelects()
    {
        var model = CreateModel();
        var id = model.CreateWorkspace("Test", WorkspaceColorId.Blush);
        Assert.Equal(2, model.Workspaces.Count);
        Assert.Equal(id, model.CurrentWorkspace.Id);
    }

    [Fact]
    public void AddFolder_CreatesFolder()
    {
        var model = CreateModel();
        var folderId = model.AddFolder("My Folder", null);
        var node = model.NodeById(folderId);
        Assert.NotNull(node);
        Assert.IsType<FolderNode>(node);
        Assert.Equal("My Folder", node!.DisplayName);
    }

    [Fact]
    public void AddLink_CreatesLink()
    {
        var model = CreateModel();
        var linkId = model.AddLink("https://example.com", "Example", null);
        var node = model.NodeById(linkId);
        Assert.NotNull(node);
        Assert.IsType<LinkNode>(node);
    }

    [Fact]
    public void DeleteNode_RemovesNode()
    {
        var model = CreateModel();
        var linkId = model.AddLink("https://example.com", "Example", null);
        model.DeleteNode(linkId);
        Assert.Null(model.NodeById(linkId));
    }

    [Fact]
    public void MoveNode_ReordersCorrectly()
    {
        var model = CreateModel();
        var id1 = model.AddLink("https://a.com", "A", null);
        var id2 = model.AddLink("https://b.com", "B", null);
        model.MoveNode(id2, null, 0);
        Assert.Equal(id2, model.CurrentWorkspace.Items[0].Id);
        Assert.Equal(id1, model.CurrentWorkspace.Items[1].Id);
    }

    [Fact]
    public void MoveNodeIntoFolder_Works()
    {
        var model = CreateModel();
        var folderId = model.AddFolder("Folder", null);
        var linkId = model.AddLink("https://example.com", "Example", null);
        model.MoveNode(linkId, folderId, 0);
        var folder = model.NodeById(folderId) as FolderNode;
        Assert.NotNull(folder);
        Assert.Single(folder!.Folder.Children);
        Assert.Equal(linkId, folder.Folder.Children[0].Id);
    }

    [Fact]
    public void RenameNode_UpdatesName()
    {
        var model = CreateModel();
        var folderId = model.AddFolder("Old Name", null);
        model.RenameNode(folderId, "New Name");
        Assert.Equal("New Name", model.NodeById(folderId)!.DisplayName);
    }

    [Fact]
    public void DeleteWorkspace_PreventsDeletingLast()
    {
        var model = CreateModel();
        var id = model.CurrentWorkspace.Id;
        model.DeleteWorkspace(id);
        Assert.Single(model.Workspaces); // Should still have 1
    }

    [Fact]
    public void MoveWorkspace_ReordersCorrectly()
    {
        var model = CreateModel();
        var id2 = model.CreateWorkspace("Second", WorkspaceColorId.Apricot);
        model.MoveWorkspace(id2, WorkspaceMoveDirection.Left);
        Assert.Equal(id2, model.Workspaces[0].Id);
    }

    [Fact]
    public void NestedFolderOperations_Work()
    {
        var model = CreateModel();
        var parentId = model.AddFolder("Parent", null);
        var childId = model.AddFolder("Child", parentId);
        var linkId = model.AddLink("https://example.com", "Link", childId);

        var parent = model.NodeById(parentId) as FolderNode;
        Assert.NotNull(parent);
        Assert.Single(parent!.Folder.Children);

        var child = model.NodeById(childId) as FolderNode;
        Assert.NotNull(child);
        Assert.Single(child!.Folder.Children);
    }

    [Fact]
    public void PinLink_MovesToPinnedList()
    {
        var model = CreateModel();
        var linkId = model.AddLink("https://example.com", "Example", null);
        model.PinLink(linkId);
        Assert.Single(model.CurrentWorkspace.PinnedLinks);
        Assert.Null(model.NodeById(linkId)); // Removed from tree
    }

    [Fact]
    public void UnpinLink_MovesBackToTree()
    {
        var model = CreateModel();
        var linkId = model.AddLink("https://example.com", "Example", null);
        model.PinLink(linkId);
        model.UnpinLink(linkId);
        Assert.Empty(model.CurrentWorkspace.PinnedLinks);
        Assert.NotNull(model.NodeById(linkId)); // Back in tree
    }

    [Fact]
    public void GroupNodesInNewFolder_CreatesFolder()
    {
        var model = CreateModel();
        var id1 = model.AddLink("https://a.com", "A", null);
        var id2 = model.AddLink("https://b.com", "B", null);
        var folderId = model.GroupNodesInNewFolder(new List<Guid> { id1, id2 }, "Group");
        Assert.NotNull(folderId);
        var folder = model.NodeById(folderId!.Value) as FolderNode;
        Assert.NotNull(folder);
        Assert.Equal(2, folder!.Folder.Children.Count);
    }
}
