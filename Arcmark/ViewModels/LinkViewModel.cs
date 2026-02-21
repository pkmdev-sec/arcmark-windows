using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arcmark.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcmark.ViewModels;

public partial class LinkViewModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _url   = string.Empty;
    [ObservableProperty] private ImageSource? _favicon;

    public static LinkViewModel FromLink(Link link)
    {
        return new LinkViewModel
        {
            Id      = link.Id,
            Title   = link.Title,
            Url     = link.Url,
            Favicon = LoadFavicon(link.FaviconPath)
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
            bmp.UriSource        = new Uri(path, UriKind.Absolute);
            bmp.CacheOption      = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 32;
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
