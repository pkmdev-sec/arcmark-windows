using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace Arcmark.Services;

/// <summary>
/// Async favicon fetching with disk caching.
/// Thread-safe: cache reads/writes are marshalled to the UI dispatcher.
/// </summary>
public class FaviconService
{
    public static readonly FaviconService Shared = new();

    private readonly DataStore _store = new();
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _failureCooldown = TimeSpan.FromSeconds(300);

    // In-memory image cache (keyed by lowercased host)
    private readonly Dictionary<string, BitmapImage> _imageCache = new();
    // Disk-path cache: host -> absolute file path
    private readonly Dictionary<string, string> _cachedPaths = new();
    // Tracks in-flight host requests
    private readonly HashSet<string> _inFlight = new();
    // Pending callbacks waiting on an in-flight request
    private readonly Dictionary<string, List<Action<BitmapImage?, string?>>> _pendingCallbacks = new();
    // Failure timestamps for cooldown logic
    private readonly Dictionary<string, DateTime> _failureTimestamps = new();

    private FaviconService()
    {
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(5)
        });
        _httpClient.Timeout = TimeSpan.FromSeconds(8);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// Fetches or returns a cached favicon for the given URL.
    /// <paramref name="cachedPath"/> is the previously stored icon path (may be null).
    /// <paramref name="completion"/> is always called on the UI thread.
    /// </summary>
    public void FetchFavicon(Uri url, string? cachedPath, Action<BitmapImage?, string?> completion)
    {
        var host = url.Host?.ToLowerInvariant();
        if (string.IsNullOrEmpty(host))
        {
            CompleteOnUI(completion, null, null);
            return;
        }

        if (host == "localhost" || host == "127.0.0.1")
        {
            CompleteOnUI(completion, null, null);
            return;
        }

        // Failure cooldown
        if (_failureTimestamps.TryGetValue(host, out var failTime) &&
            DateTime.UtcNow - failTime < _failureCooldown)
        {
            CompleteOnUI(completion, null, null);
            return;
        }

        // Memory cache hit
        if (_imageCache.TryGetValue(host, out var cached))
        {
            _cachedPaths.TryGetValue(host, out var p);
            CompleteOnUI(completion, cached, p);
            return;
        }

        var iconsDir = _store.IconsDirectory();
        var fileName = host.Replace(":", "_") + ".ico";
        var filePath = Path.Combine(iconsDir, fileName);

        // Previously stored path
        if (cachedPath != null && File.Exists(cachedPath))
        {
            var img = TryLoadBitmap(cachedPath);
            if (img != null)
            {
                _imageCache[host] = img;
                _cachedPaths[host] = cachedPath;
                CompleteOnUI(completion, img, cachedPath);
                return;
            }
        }

        // Disk cache hit
        if (File.Exists(filePath))
        {
            var img = TryLoadBitmap(filePath);
            if (img != null)
            {
                _imageCache[host] = img;
                _cachedPaths[host] = filePath;
                CompleteOnUI(completion, img, filePath);
                return;
            }
        }

        // Deduplicate in-flight
        if (_inFlight.Contains(host))
        {
            if (!_pendingCallbacks.TryGetValue(host, out var list))
                _pendingCallbacks[host] = list = new List<Action<BitmapImage?, string?>>();
            list.Add(completion);
            return;
        }

        _inFlight.Add(host);

        Task.Run(async () => await FetchAndSave(url, host, filePath, completion));
    }

    private async Task FetchAndSave(Uri url, string host, string filePath,
        Action<BitmapImage?, string?> primaryCompletion)
    {
        var scheme = url.Scheme ?? "https";
        var primaryUrl  = $"{scheme}://{host}/favicon.ico";
        var fallbackUrl = $"https://www.google.com/s2/favicons?sz=64&domain_url={scheme}://{host}";

        byte[]? data = await FetchData(primaryUrl) ?? await FetchData(fallbackUrl);

        void Deliver()
        {
            _inFlight.Remove(host);
            _pendingCallbacks.TryGetValue(host, out var pending);
            _pendingCallbacks.Remove(host);

            if (data == null)
            {
                _failureTimestamps[host] = DateTime.UtcNow;
                CompleteOnUI(primaryCompletion, null, null);
                if (pending != null)
                    foreach (var cb in pending) CompleteOnUI(cb, null, null);
                return;
            }

            try { File.WriteAllBytes(filePath, data); } catch { /* best-effort */ }

            var bitmap = TryLoadBitmapFromBytes(data);
            if (bitmap != null)
            {
                _imageCache[host] = bitmap;
                _cachedPaths[host] = filePath;
            }

            CompleteOnUI(primaryCompletion, bitmap, bitmap != null ? filePath : null);
            if (pending != null)
                foreach (var cb in pending) CompleteOnUI(cb, bitmap, bitmap != null ? filePath : null);
        }

        var uiDispatcher = System.Windows.Application.Current?.Dispatcher;
        if (uiDispatcher != null)
            uiDispatcher.BeginInvoke(Deliver);
        else
            Deliver();
    }

    private async Task<byte[]?> FetchData(string url)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await _httpClient.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode) return null;
            var data = await response.Content.ReadAsByteArrayAsync(cts.Token);
            return data.Length > 0 ? data : null;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapImage? TryLoadBitmap(string path)
    {
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(path, UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch { return null; }
    }

    private static BitmapImage? TryLoadBitmapFromBytes(byte[] data)
    {
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = new System.IO.MemoryStream(data);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch { return null; }
    }

    private static void CompleteOnUI(Action<BitmapImage?, string?> completion,
        BitmapImage? image, string? path)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            dispatcher.BeginInvoke(() => completion(image, path));
        else
            completion(image, path);
    }
}
