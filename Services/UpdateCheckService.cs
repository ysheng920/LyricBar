using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DesktopLyrics.Services
{
    public class UpdateCheckService
    {
        public const string CurrentVersion = "v1.1.0";
        private const string GitHubApiLatestUrl = "https://api.github.com/repos/ysheng920/LyricBar/releases/latest";

        private readonly HttpClient _httpClient;
        private readonly DispatcherTimer _periodicTimer;

        public event Action<string, string, string>? UpdateAvailable;

        public string? LatestVersion { get; private set; }
        public string? LatestReleaseUrl { get; private set; }
        public bool HasUpdate { get; private set; }

        public UpdateCheckService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LyricBar-UpdateChecker");

            // Periodic check every 6 hours
            _periodicTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(6)
            };
            _periodicTimer.Tick += async (s, e) => await CheckForUpdatesAsync(isManual: false);
            _periodicTimer.Start();
        }

        public void StartStartupCheck()
        {
            // Delayed check 3 seconds after startup
            Task.Delay(3000).ContinueWith(async _ =>
            {
                await CheckForUpdatesAsync(isManual: false);
            });
        }

        public async Task<bool> CheckForUpdatesAsync(bool isManual = false)
        {
            try
            {
                var release = await _httpClient.GetFromJsonAsync<GitHubReleaseInfo>(GitHubApiLatestUrl);
                if (release != null && !string.IsNullOrWhiteSpace(release.TagName))
                {
                    LatestVersion = release.TagName.Trim();
                    LatestReleaseUrl = string.IsNullOrWhiteSpace(release.HtmlUrl)
                        ? "https://github.com/ysheng920/LyricBar/releases/latest"
                        : release.HtmlUrl;

                    if (IsNewerVersion(LatestVersion, CurrentVersion))
                    {
                        HasUpdate = true;
                        UpdateAvailable?.Invoke(LatestVersion, LatestReleaseUrl, release.Body ?? string.Empty);
                        return true;
                    }
                    else
                    {
                        HasUpdate = false;
                    }
                }
            }
            catch
            {
                // Network failure or rate limit
            }

            return false;
        }

        public static bool IsNewerVersion(string latestTag, string currentTag)
        {
            try
            {
                var v1Str = latestTag.TrimStart('v', 'V').Split('-')[0];
                var v2Str = currentTag.TrimStart('v', 'V').Split('-')[0];

                if (Version.TryParse(v1Str, out var v1) && Version.TryParse(v2Str, out var v2))
                {
                    return v1 > v2;
                }
            }
            catch { }

            return false;
        }

        public static void OpenReleasePage(string? url = null)
        {
            try
            {
                var targetUrl = string.IsNullOrWhiteSpace(url)
                    ? "https://github.com/ysheng920/LyricBar/releases/latest"
                    : url;

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetUrl,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private class GitHubReleaseInfo
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }
        }
    }
}
