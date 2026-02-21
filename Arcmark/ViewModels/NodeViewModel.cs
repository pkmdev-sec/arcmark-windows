using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arcmark.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcmark.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private bool _isFolder;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private int _depth;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private ImageSource? _icon;
    [ObservableProperty] private bool _isHovered;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isRenaming;

    /// <summary>Left margin based on depth: depth * 16 + 8.</summary>
    [ObservableProperty] private Thickness _indentation;

    partial void OnDepthChanged(int value)
    {
        Indentation = new Thickness(value * 16 + 8, 0, 0, 0);
    }

    public static NodeViewModel FromNode(Node node, int depth)
    {
        return node switch
        {
            FolderNode fn => new NodeViewModel
            {
                Id          = fn.Folder.Id,
                DisplayName = fn.Folder.Name,
                IsFolder    = true,
                IsExpanded  = fn.Folder.IsExpanded,
                Depth       = depth,
                Indentation = new Thickness(depth * 16 + 8, 0, 0, 0)
            },
            LinkNode ln => new NodeViewModel
            {
                Id          = ln.Link.Id,
                DisplayName = ln.Link.Title,
                IsFolder    = false,
                Depth       = depth,
                Url         = ln.Link.Url,
                Indentation = new Thickness(depth * 16 + 8, 0, 0, 0),
                Icon        = LoadFavicon(ln.Link.FaviconPath)
            },
            _ => throw new ArgumentException($"Unknown node type: {node.GetType().Name}")
        };
    }

    public static ImageSource? LoadFaviconStatic(string? path) => LoadFavicon(path);

    private static ImageSource? LoadFavicon(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource    = new Uri(path, UriKind.Absolute);
            bmp.CacheOption  = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 18;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
