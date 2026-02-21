using System.Net.Http;
using System.Text.RegularExpressions;

namespace Arcmark.Services;

/// <summary>
/// Fetches the HTML &lt;title&gt; of a URL on a background thread and delivers
/// the result on the UI thread.
/// </summary>
public class LinkTitleService
{
    public static readonly LinkTitleService Shared = new();

    private readonly HttpClient _httpClient;
    private readonly HashSet<Guid> _inFlight = new();
    private static readonly Regex TitleRegex =
        new(@"<title[^>]*>(.*?)</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(2));

    private LinkTitleService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// Downloads HTML for <paramref name="url"/> and extracts the &lt;title&gt;.
    /// <paramref name="completion"/> is called on the UI thread with the title string,
    /// or <c>null</c> if the title could not be retrieved.
    /// </summary>
    public void FetchTitle(Uri url, Guid linkId, Action<string?> completion)
    {
        if (_inFlight.Contains(linkId))
        {
            CallbackOnUI(completion, null);
            return;
        }
        _inFlight.Add(linkId);

        Task.Run(async () =>
        {
            string? title = null;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    CallbackOnUI(completion, null);
                    return;
                }

                // Limit to first 200KB
                var bytes = await response.Content.ReadAsByteArrayAsync(cts.Token);
                var limited = bytes.AsSpan(0, Math.Min(bytes.Length, 200_000)).ToArray();
                var html = System.Text.Encoding.UTF8.GetString(limited);

                var match = TitleRegex.Match(html);
                if (match.Success)
                {
                    title = match.Groups[1].Value
                        .Replace("\n", " ")
                        .Replace("\t", " ")
                        .Replace("  ", " ")
                        .Trim();
                    if (string.IsNullOrEmpty(title)) title = null;
                }
            }
            catch { /* swallow — title stays null */ }
            finally
            {
                _inFlight.Remove(linkId);
            }

            CallbackOnUI(completion, title);
        });
    }

    private static void CallbackOnUI(Action<string?> completion, string? value)
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
            app.Dispatcher.BeginInvoke(() => completion(value));
        else
            completion(value);
    }
}
