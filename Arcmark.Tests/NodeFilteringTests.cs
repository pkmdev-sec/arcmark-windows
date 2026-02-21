using Xunit;
using Arcmark.Models;
using Arcmark.Services;

namespace Arcmark.Tests;

/// <summary>
/// Tests for the node search and filter logic used in the sidebar query bar.
/// </summary>
public class NodeFilteringTests
{
    private static List<Node> BuildTree()
    {
        return
        [
            new FolderNode
            {
                Folder = new Folder
                {
                    Id = Guid.NewGuid(),
                    Name = "Work",
                    IsExpanded = true,
                    Children = new List<Node>
                    {
                        new LinkNode { Link = new Link { Id = Guid.NewGuid(), Title = "GitHub", Url = "https://github.com" } },
                        new LinkNode { Link = new Link { Id = Guid.NewGuid(), Title = "Jira Board", Url = "https://mycompany.atlassian.net/jira" } }
                    }
                }
            },
            new FolderNode
            {
                Folder = new Folder
                {
                    Id = Guid.NewGuid(),
                    Name = "Personal",
                    IsExpanded = false,
                    Children = new List<Node>
                    {
                        new LinkNode { Link = new Link { Id = Guid.NewGuid(), Title = "Reddit", Url = "https://reddit.com" } }
                    }
                }
            },
            new LinkNode { Link = new Link { Id = Guid.NewGuid(), Title = "Hacker News", Url = "https://news.ycombinator.com" } }
        ];
    }

    [Fact]
    public void EmptyQuery_ReturnsAllNodes()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, string.Empty);

        // Should return the original tree unchanged (all root-level items)
        Assert.Equal(tree.Count, results.Count);
    }

    [Fact]
    public void NullQuery_ReturnsAllNodes()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, null);

        Assert.Equal(tree.Count, results.Count);
    }

    [Fact]
    public void MatchingLinkTitle_ReturnsTheLink()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, "GitHub");

        // Should return 1 result — either the link directly or a folder containing only it
        var allLinks = FlattenLinks(results);
        Assert.Single(allLinks);
        Assert.Equal("GitHub", allLinks[0].Link.Title);
    }

    [Fact]
    public void MatchingUrl_ReturnsTheLink()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, "ycombinator");

        var allLinks = FlattenLinks(results);
        Assert.Single(allLinks);
        Assert.Equal("Hacker News", allLinks[0].Link.Title);
    }

    [Fact]
    public void ParentFolderPreserved_WhenChildMatches()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, "Jira");

        // The "Work" folder should be in results, containing only Jira
        var folder = results.OfType<FolderNode>().FirstOrDefault(f => f.Folder.Name == "Work");
        Assert.NotNull(folder);
        Assert.Single(folder!.Folder.Children);
        Assert.IsType<LinkNode>(folder.Folder.Children[0]);
        Assert.Equal("Jira Board", ((LinkNode)folder.Folder.Children[0]).Link.Title);
    }

    [Fact]
    public void NonMatchingNodes_AreExcluded()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, "GitHub");

        // "Personal" folder and "Hacker News" should not appear
        Assert.DoesNotContain(results, n => n is FolderNode f && f.Folder.Name == "Personal");
        Assert.DoesNotContain(results, n => n is LinkNode l && l.Link.Title == "Hacker News");
    }

    [Fact]
    public void CaseInsensitive_MatchesRegardlessOfCase()
    {
        var tree = BuildTree();

        var upperResults = NodeFiltering.FilterNodes(tree, "GITHUB");
        var lowerResults = NodeFiltering.FilterNodes(tree, "github");
        var mixedResults = NodeFiltering.FilterNodes(tree, "GitHub");

        var upperLinks = FlattenLinks(upperResults);
        var lowerLinks = FlattenLinks(lowerResults);
        var mixedLinks = FlattenLinks(mixedResults);

        Assert.Single(upperLinks);
        Assert.Single(lowerLinks);
        Assert.Single(mixedLinks);
        Assert.Equal("GitHub", upperLinks[0].Link.Title);
    }

    [Fact]
    public void PartialMatch_ReturnsNodes()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, "git");

        var allLinks = FlattenLinks(results);
        Assert.Single(allLinks);
        Assert.Equal("GitHub", allLinks[0].Link.Title);
    }

    [Fact]
    public void NoMatch_ReturnsEmptyList()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, "zzz_no_match_zzz");

        Assert.Empty(results);
    }

    [Fact]
    public void FolderNameMatch_ReturnsFolderWithAllChildren()
    {
        var tree = BuildTree();
        var results = NodeFiltering.FilterNodes(tree, "Personal");

        var folder = results.OfType<FolderNode>().FirstOrDefault(f => f.Folder.Name == "Personal");
        Assert.NotNull(folder);
        // When the folder itself matches, all its children should be included
        Assert.Single(folder!.Folder.Children);
    }

    [Fact]
    public void MultipleMatches_ReturnsAllMatchingNodes()
    {
        var tree = BuildTree();
        // Both "GitHub" and "Jira Board" are in the Work folder; searching "a" matches several
        var results = NodeFiltering.FilterNodes(tree, "a");

        var allLinks = FlattenLinks(results);
        // GitHub, Jira Board, Hacker News all contain "a"
        Assert.True(allLinks.Count >= 3);
    }

    [Fact]
    public void FilteredTree_IsImmutable_OriginalUnchanged()
    {
        var tree = BuildTree();
        var originalCount = tree.Count;
        var workFolder = tree.OfType<FolderNode>().First(f => f.Folder.Name == "Work");
        var originalChildCount = workFolder.Folder.Children.Count;

        NodeFiltering.FilterNodes(tree, "GitHub");

        // Original tree should be unchanged
        Assert.Equal(originalCount, tree.Count);
        Assert.Equal(originalChildCount, workFolder.Folder.Children.Count);
    }

    // Helper: recursively collect all LinkNodes from a result tree
    private static List<LinkNode> FlattenLinks(List<Node> nodes)
    {
        var links = new List<LinkNode>();
        foreach (var node in nodes)
        {
            if (node is LinkNode link)
                links.Add(link);
            else if (node is FolderNode folder)
                links.AddRange(FlattenLinks(folder.Folder.Children));
        }
        return links;
    }
}
