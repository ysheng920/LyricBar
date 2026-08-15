using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DesktopLyrics.Models;

namespace DesktopLyrics.Services
{
    public class LyricsService
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, List<LrcLine>> _cache = new();

        public LyricsService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public async Task<List<LrcLine>?> FetchLyricsAsync(string rawTitle, string rawArtist, TimeSpan duration, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawTitle))
                return null;

            var (cleanTitle, cleanArtist) = ParseAndCleanTrack(rawTitle, rawArtist);
            var cacheKey = $"{cleanTitle}___{cleanArtist}".ToLowerInvariant();

            if (_cache.TryGetValue(cacheKey, out var cachedLyrics))
                return cachedLyrics;

            var titleVariants = GenerateTitleVariants(cleanTitle);

            foreach (var title in titleVariants)
            {
                // 1. Try Kugou Music (Massive synced library)
                try
                {
                    var kugouResult = await FetchFromKugouAsync(title, cleanArtist, duration, ct);
                    if (kugouResult != null && kugouResult.Count > 0)
                    {
                        _cache[cacheKey] = kugouResult;
                        return kugouResult;
                    }
                }
                catch { }

                // 2. Try NetEase Cloud Music
                try
                {
                    var neteaseResult = await FetchFromNetEaseAsync(title, cleanArtist, ct);
                    if (neteaseResult != null && neteaseResult.Count > 0)
                    {
                        _cache[cacheKey] = neteaseResult;
                        return neteaseResult;
                    }
                }
                catch { }

                // 3. Try QQ Music
                try
                {
                    var qqResult = await FetchFromQQMusicAsync(title, cleanArtist, ct);
                    if (qqResult != null && qqResult.Count > 0)
                    {
                        _cache[cacheKey] = qqResult;
                        return qqResult;
                    }
                }
                catch { }

                // 4. Try LRCLIB Exact & Search
                try
                {
                    var lrcLibResult = await FetchFromLrcLibExactAsync(title, cleanArtist, duration, ct) 
                                       ?? await FetchFromLrcLibSearchAsync(title, cleanArtist, ct);
                    if (lrcLibResult != null && lrcLibResult.Count > 0)
                    {
                        _cache[cacheKey] = lrcLibResult;
                        return lrcLibResult;
                    }
                }
                catch { }
            }

            return null;
        }

        private static List<string> GenerateTitleVariants(string title)
        {
            var list = new List<string> { title };

            // Strip parentheses: "甲乙丙丁 (你我怎么两清)" -> "甲乙丙丁"
            var withoutParentheses = Regex.Replace(title, @"\s*[\(\[\{（【].*?[\)\]\}）】]", "").Trim();
            if (!string.IsNullOrWhiteSpace(withoutParentheses) && !list.Contains(withoutParentheses))
            {
                list.Add(withoutParentheses);
            }

            // Strip after hyphen: "Song - Subtitle" -> "Song"
            var hyphenIdx = title.IndexOf(" - ", StringComparison.Ordinal);
            if (hyphenIdx > 0)
            {
                var beforeHyphen = title.Substring(0, hyphenIdx).Trim();
                if (!string.IsNullOrWhiteSpace(beforeHyphen) && !list.Contains(beforeHyphen))
                {
                    list.Add(beforeHyphen);
                }
            }

            return list;
        }

        public static (string Title, string Artist) ParseAndCleanTrack(string title, string artist)
        {
            var cleanTitle = title.Trim();
            var cleanArtist = artist.Trim();

            // Replace full-width brackets
            cleanTitle = cleanTitle.Replace('（', '(').Replace('）', ')')
                                   .Replace('【', '[').Replace('】', ']')
                                   .Replace('「', '[').Replace('」', ']');

            // Check if title is formatted as "Artist - Title" (common on YouTube)
            if (string.IsNullOrWhiteSpace(cleanArtist) || cleanArtist.EndsWith("- Topic", StringComparison.OrdinalIgnoreCase))
            {
                var hyphenIdx = cleanTitle.IndexOf(" - ", StringComparison.Ordinal);
                if (hyphenIdx > 0)
                {
                    var possibleArtist = cleanTitle.Substring(0, hyphenIdx).Trim();
                    var possibleTitle = cleanTitle.Substring(hyphenIdx + 3).Trim();
                    if (!string.IsNullOrWhiteSpace(possibleArtist) && !string.IsNullOrWhiteSpace(possibleTitle))
                    {
                        cleanArtist = possibleArtist;
                        cleanTitle = possibleTitle;
                    }
                }
            }

            // Remove YouTube noisy tags
            cleanTitle = Regex.Replace(cleanTitle, @"\s*[\(\[\{](?:official|music\s*video|mv|hd|hq|audio|lyrics|lyric\s*video|visualizer|remastered|live|4k|video|feat\..*?|ft\..*?|prod\..*?|performance\s*video)[\)\]\}]", "", RegexOptions.IgnoreCase);
            cleanTitle = Regex.Replace(cleanTitle, @"\s*[-–—]\s*(?:official|music\s*video|mv|audio|lyrics|visualizer).*$", "", RegexOptions.IgnoreCase);
            cleanTitle = Regex.Replace(cleanTitle, @"\s*\|\s*.*$", "");

            // Clean artist channel tags
            cleanArtist = Regex.Replace(cleanArtist, @"\s*-\s*Topic$", "", RegexOptions.IgnoreCase);
            cleanArtist = Regex.Replace(cleanArtist, @"\s*VEVO$", "", RegexOptions.IgnoreCase);

            return (cleanTitle.Trim(), cleanArtist.Trim());
        }

        private async Task<List<LrcLine>?> FetchFromKugouAsync(string title, string artist, TimeSpan duration, CancellationToken ct)
        {
            var keyword = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var durationMs = (int)duration.TotalMilliseconds;

            var searchUrl = $"http://mobilecdn.kugou.com/api/v3/search/song?format=json&keyword={Uri.EscapeDataString(keyword)}&page=1&pagesize=5";
            var searchResp = await _httpClient.GetAsync(searchUrl, ct);
            if (!searchResp.IsSuccessStatusCode) return null;

            using var searchDoc = await JsonDocument.ParseAsync(await searchResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!searchDoc.RootElement.TryGetProperty("data", out var dataElem) ||
                !dataElem.TryGetProperty("info", out var infoElem) ||
                infoElem.GetArrayLength() == 0)
                return null;

            var firstSong = infoElem[0];
            var hash = firstSong.GetProperty("hash").GetString();
            if (string.IsNullOrWhiteSpace(hash)) return null;

            var lyricSearchUrl = $"http://krcs.kugou.com/search?ver=1&man=yes&client=mobi&keyword={Uri.EscapeDataString(keyword)}&duration={durationMs}&hash={hash}";
            var lyricSearchResp = await _httpClient.GetAsync(lyricSearchUrl, ct);
            if (!lyricSearchResp.IsSuccessStatusCode) return null;

            using var lyricSearchDoc = await JsonDocument.ParseAsync(await lyricSearchResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!lyricSearchDoc.RootElement.TryGetProperty("candidates", out var candElem) || candElem.GetArrayLength() == 0)
                return null;

            var candidate = candElem[0];
            var id = candidate.GetProperty("id").GetString();
            var accessKey = candidate.GetProperty("accesskey").GetString();

            var downloadUrl = $"http://krcs.kugou.com/download?ver=1&client=mobi&id={id}&accesskey={accessKey}&fmt=lrc&charset=utf8";
            var downloadResp = await _httpClient.GetAsync(downloadUrl, ct);
            if (!downloadResp.IsSuccessStatusCode) return null;

            using var downloadDoc = await JsonDocument.ParseAsync(await downloadResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!downloadDoc.RootElement.TryGetProperty("content", out var contentElem)) return null;

            var base64Lrc = contentElem.GetString();
            if (string.IsNullOrWhiteSpace(base64Lrc)) return null;

            var lrcBytes = Convert.FromBase64String(base64Lrc);
            var lrcText = Encoding.UTF8.GetString(lrcBytes);

            return LrcParser.Parse(lrcText);
        }

        private async Task<List<LrcLine>?> FetchFromNetEaseAsync(string title, string artist, CancellationToken ct)
        {
            var keyword = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var searchUrl = $"https://music.163.com/api/search/get/web?s={Uri.EscapeDataString(keyword)}&type=1&offset=0&total=true&limit=1";

            var response = await _httpClient.GetAsync(searchUrl, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("result", out var resultElem))
                return null;
            if (!resultElem.TryGetProperty("songs", out var songsElem) || songsElem.GetArrayLength() == 0)
                return null;

            var songId = songsElem[0].GetProperty("id").GetInt64();
            var lyricUrl = $"https://music.163.com/api/song/lyric?os=pc&id={songId}&lv=-1&kv=-1&tv=-1";

            var lyricResponse = await _httpClient.GetAsync(lyricUrl, ct);
            if (!lyricResponse.IsSuccessStatusCode)
                return null;

            using var lyricDoc = await JsonDocument.ParseAsync(await lyricResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (lyricDoc.RootElement.TryGetProperty("lrc", out var lrcElem) &&
                lrcElem.TryGetProperty("lyric", out var lyricTextElem))
            {
                var lrcString = lyricTextElem.GetString();
                if (!string.IsNullOrWhiteSpace(lrcString))
                {
                    return LrcParser.Parse(lrcString);
                }
            }

            return null;
        }

        private async Task<List<LrcLine>?> FetchFromQQMusicAsync(string title, string artist, CancellationToken ct)
        {
            var keyword = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var searchUrl = $"https://c.y.qq.com/soso/fcgi-bin/client_search_cp?p=1&n=5&w={Uri.EscapeDataString(keyword)}&format=json";

            var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            request.Headers.Add("Referer", "https://y.qq.com/");
            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("data", out var dataElem) ||
                !dataElem.TryGetProperty("song", out var songElem) ||
                !songElem.TryGetProperty("list", out var listElem) ||
                listElem.GetArrayLength() == 0)
                return null;

            var songMid = listElem[0].GetProperty("songmid").GetString();
            if (string.IsNullOrWhiteSpace(songMid)) return null;

            var lyricUrl = $"https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?songmid={songMid}&format=json&nobase64=0";
            var lyricReq = new HttpRequestMessage(HttpMethod.Get, lyricUrl);
            lyricReq.Headers.Add("Referer", "https://y.qq.com/");
            var lyricResp = await _httpClient.SendAsync(lyricReq, ct);
            if (!lyricResp.IsSuccessStatusCode) return null;

            using var lyricDoc = await JsonDocument.ParseAsync(await lyricResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (lyricDoc.RootElement.TryGetProperty("lyric", out var lyricElem))
            {
                var base64Lrc = lyricElem.GetString();
                if (!string.IsNullOrWhiteSpace(base64Lrc))
                {
                    var lrcBytes = Convert.FromBase64String(base64Lrc);
                    var lrcText = Encoding.UTF8.GetString(lrcBytes);
                    lrcText = System.Net.WebUtility.HtmlDecode(lrcText);
                    return LrcParser.Parse(lrcText);
                }
            }

            return null;
        }

        private async Task<List<LrcLine>?> FetchFromLrcLibExactAsync(string title, string artist, TimeSpan duration, CancellationToken ct)
        {
            var url = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(title)}&artist_name={Uri.EscapeDataString(artist)}";
            if (duration > TimeSpan.Zero)
            {
                url += $"&duration={(int)duration.TotalSeconds}";
            }

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadFromJsonAsync<LrcLibResponse>(cancellationToken: ct);
            if (!string.IsNullOrWhiteSpace(content?.SyncedLyrics))
            {
                return LrcParser.Parse(content.SyncedLyrics);
            }

            return null;
        }

        private async Task<List<LrcLine>?> FetchFromLrcLibSearchAsync(string title, string artist, CancellationToken ct)
        {
            var query = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var url = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(query)}";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var items = await response.Content.ReadFromJsonAsync<List<LrcLibResponse>>(cancellationToken: ct);
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.SyncedLyrics))
                    {
                        return LrcParser.Parse(item.SyncedLyrics);
                    }
                }
            }

            return null;
        }

        private class LrcLibResponse
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("trackName")]
            public string? TrackName { get; set; }

            [JsonPropertyName("artistName")]
            public string? ArtistName { get; set; }

            [JsonPropertyName("albumName")]
            public string? AlbumName { get; set; }

            [JsonPropertyName("duration")]
            public double Duration { get; set; }

            [JsonPropertyName("plainLyrics")]
            public string? PlainLyrics { get; set; }

            [JsonPropertyName("syncedLyrics")]
            public string? SyncedLyrics { get; set; }
        }
    }
}
