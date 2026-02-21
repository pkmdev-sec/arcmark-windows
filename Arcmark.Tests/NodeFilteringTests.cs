using Xunit;
using Arcmark.Models;
using Arcmark.Services;

namespace Arcmark.Tests;

/// <summary>
/// Tests for the node search and filter logic used in the sidebar query bar.
/// </summary>
public class NodeFilteringTests
{
    private static List<INode> BuildTree()
    {
        return
        [
            new FolderNode
            {
                Id = Guid.NewGuid(),
                Name = "Work",
                IsExpanded = true,
                Children =
                [
                    new LinkNode { Id = Guid.NewGuid(), Title = "GitHub", Url = "https://github.com" },
                    new LinkNode { Id = Guid.NewGuid(), Title = "Jira Board", Url = "https://mycompany.atlassian.net/jira" }
                ]
            },
            new FolderNode
            {
                Id = Guid.NewGuid(),
                Name = "Personal",
                IsExpanded = false,
                Children =
                [
                    new LinkNode { Id = Guid.NewGuid(), Title = "Reddit", Url = "https://reddit.com" }
                ]
            },
            new LinkNode { Id = Guid.NewGuid(), Title = "Hacker News", Url = "https://news.ycombinator.com" }
        ];
    }

    [Fact]
    public void EmptyQuery_ReturnsAllNodes()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, string.Empty);

        // Should return the original tree unchanged (all root-level items)
        Assert.Equal(tree.Count, results.Count);
    }

    [Fact]
    public void NullQuery_ReturnsAllNodes()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, null);

        Assert.Equal(tree.Count, results.Count);
    }

    [Fact]
    public void MatchingLinkTitle_ReturnsTheLink()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, "GitHub");

        // Should return 1 result — either the link directly or a folder containing only it
        var allLinks = FlattenLinks(results);
        Assert.Single(allLinks);
        Assert.Equal("GitHub", allLinks[0].Title);
    }

    [Fact]
    public void MatchingUrl_ReturnsTheLink()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, "ycombinator");

        var allLinks = FlattenLinks(results);
        Assert.Single(allLinks);
        Assert.Equal("Hacker News", allLinks[0].Title);
    }

    [Fact]
    public void ParentFolderPreserved_WhenChildMatches()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, "Jira");

        // The "Work" folder should be in results, containing only Jira
        var folder = results.OfType<FolderNode>().FirstOrDefault(f => f.Name == "Work");
        Assert.NotNull(folder);
        Assert.Single(folder!.Children);
        Assert.IsType<LinkNode>(folder.Children[0]);
        Assert.Equal("Jira Board", ((LinkNode)folder.Children[0]).Title);
    }

    [Fact]
    public void NonMatchingNodes_AreExcluded()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, "GitHub");

        // "Personal" folder and "Hacker News" should not appear
        Assert.DoesNotContain(results, n => n is FolderNode f && f.Name == "Personal");
        Assert.DoesNotContain(results, n => n is LinkNode l && l.Title == "Hacker News");
    }

    [Fact]
    public void CaseInsensitive_MatchesRegardlessOfCase()
    {
        var tree = BuildTree();

        var upperResults = NodeFilter.Filter(tree, "GITHUB");
        var lowerResults = NodeFilter.Filter(tree, "github");
        var mixedResults = NodeFilter.Filter(tree, "GitHub");

        var upperLinks = FlattenLinks(upperResults);
        var lowerLinks = FlattenLinks(lowerResults);
        var mixedLinks = FlattenLinks(mixedResults);

        Assert.Single(upperLinks);
        Assert.Single(lowerLinks);
        Assert.Single(mixedLinks);
        Assert.Equal("GitHub", upperLinks[0].Title);
    }

    [Fact]
    public void PartialMatch_ReturnsNodes()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, "git");

        var allLinks = FlattenLinks(results);
        Assert.Single(allLinks);
        Assert.Equal("GitHub", allLinks[0].Title);
    }

    [Fact]
    public void NoMatch_ReturnsEmptyList()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, "zzz_no_match_zzz");

        Assert.Empty(results);
    }

    [Fact]
    public void FolderNameMatch_ReturnsFolderWithAllChildren()
    {
        var tree = BuildTree();
        var results = NodeFilter.Filter(tree, "Personal");

        var folder = results.OfType<FolderNode>().FirstOrDefault(f => f.Name == "Personal");
        Assert.NotNull(folder);
        // When the folder itself matches, all its children should be included
        Assert.Single(folder!.Children);
    }

    [Fact]
    public void MultipleMatches_ReturnsAllMatchingNodes()
    {
        var tree = BuildTree();
        // Both "GitHub" and "Jira Board" are in the Work folder; searching "a" matches several
        var results = NodeFilter.Filter(tree, "a");

        var allLinks = FlattenLinks(results);
        // GitHub, Jira Board, Hacker News all contain "a"
        Assert.True(allLinks.Count >= 3);
    }

    [Fact]
    public void FilteredTree_IsImmutable_OriginalUnchanged()
    {
        var tree = BuildTree();
        var originalCount = tree.Count;
        var workFolder = tree.OfType<FolderNode>().First(f => f.Name == "Work");
        var originalChildCount = workFolder.Children.Count;

        NodeFilter.Filter(tree, "GitHub");

        // Original tree should be unchanged
        Assert.Equal(originalCount, tree.Count);
        Assert.Equal(originalChildCount, workFolder.Children.Count);
    }

    // Helper: recursively collect all LinkNodes from a result tree
    private static List<LinkNode> FlattenLinks(List<INode> nodes)
    {
        var links = new List<LinkNode>();
        foreach (var node in nodes)
        {
            if (node is LinkNode link)
                links.Add(link);
            else if (node is FolderNode folder)
                links.AddRange(FlattenLinks(folder.Children));
        }
        return links;
    }
}
