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

        public void ClearCache()
        {
            _cache.Clear();
        }

        public async Task<List<LrcLine>?> FetchLyricsAsync(string rawTitle, string rawArtist, TimeSpan duration, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawTitle))
                return null;

            var cacheKey = $"{rawTitle}___{rawArtist}".ToLowerInvariant();
            if (_cache.TryGetValue(cacheKey, out var cachedLyrics))
                return cachedLyrics;

            var searchPairs = GenerateSearchCandidates(rawTitle, rawArtist);

            // Cross-Language Bridge: If title is Latin/English, resolve original Asian song title
            if (IsLatinOnly(rawTitle))
            {
                var discoveredAliases = await ResolveAliasesFromLrcLibAsync(rawTitle, rawArtist, ct);
                foreach (var alias in discoveredAliases)
                {
                    if (!searchPairs.Exists(p => p.Title.Equals(alias, StringComparison.OrdinalIgnoreCase)))
                    {
                        searchPairs.Add(new SearchPair(alias, rawArtist));
                        searchPairs.Add(new SearchPair(ToSimplifiedChinese(alias), rawArtist));
                    }
                }
            }

            foreach (var pair in searchPairs)
            {
                if (ct.IsCancellationRequested) return null;

                bool isLatin = IsLatinOnly(pair.Title);

                // =============================================================
                // Priority Routing:
                // QQ Music & NetEase Cloud Music provide highest precision and stability
                // =============================================================
                if (isLatin)
                {
                    // 1. QQ Music (Pinyin AI & English Movie Soundtrack matching)
                    try
                    {
                        var qqResult = await FetchFromQQMusicAsync(pair.Title, pair.Artist, duration, ct);
                        if (qqResult != null && qqResult.Count > 0)
                        {
                            _cache[cacheKey] = qqResult;
                            return qqResult;
                        }
                    }
                    catch { }

                    // 2. NetEase Cloud Music (Multi-language tags)
                    try
                    {
                        var neteaseResult = await FetchFromNetEaseAsync(pair.Title, pair.Artist, duration, ct);
                        if (neteaseResult != null && neteaseResult.Count > 0)
                        {
                            _cache[cacheKey] = neteaseResult;
                            return neteaseResult;
                        }
                    }
                    catch { }

                    // 3. LRCLIB (International metadata)
                    try
                    {
                        var lrcLibResult = await FetchFromLrcLibExactAsync(pair.Title, pair.Artist, duration, ct)
                                           ?? await FetchFromLrcLibSearchAsync(pair.Title, pair.Artist, ct);
                        if (lrcLibResult != null && lrcLibResult.Count > 0)
                        {
                            _cache[cacheKey] = lrcLibResult;
                            return lrcLibResult;
                        }
                    }
                    catch { }

                    // 4. Kugou Music (Fallback with duration check)
                    try
                    {
                        var kugouResult = await FetchFromKugouAsync(pair.Title, pair.Artist, duration, ct);
                        if (kugouResult != null && kugouResult.Count > 0)
                        {
                            _cache[cacheKey] = kugouResult;
                            return kugouResult;
                        }
                    }
                    catch { }
                }
                else
                {
                    // For Chinese / CJK Titles:
                    // 1. QQ Music
                    try
                    {
                        var qqResult = await FetchFromQQMusicAsync(pair.Title, pair.Artist, duration, ct);
                        if (qqResult != null && qqResult.Count > 0)
                        {
                            _cache[cacheKey] = qqResult;
                            return qqResult;
                        }
                    }
                    catch { }

                    // 2. NetEase Cloud Music
                    try
                    {
                        var neteaseResult = await FetchFromNetEaseAsync(pair.Title, pair.Artist, duration, ct);
                        if (neteaseResult != null && neteaseResult.Count > 0)
                        {
                            _cache[cacheKey] = neteaseResult;
                            return neteaseResult;
                        }
                    }
                    catch { }

                    // 3. Kugou Music
                    try
                    {
                        var kugouResult = await FetchFromKugouAsync(pair.Title, pair.Artist, duration, ct);
                        if (kugouResult != null && kugouResult.Count > 0)
                        {
                            _cache[cacheKey] = kugouResult;
                            return kugouResult;
                        }
                    }
                    catch { }

                    // 4. LRCLIB
                    try
                    {
                        var lrcLibResult = await FetchFromLrcLibExactAsync(pair.Title, pair.Artist, duration, ct)
                                           ?? await FetchFromLrcLibSearchAsync(pair.Title, pair.Artist, ct);
                        if (lrcLibResult != null && lrcLibResult.Count > 0)
                        {
                            _cache[cacheKey] = lrcLibResult;
                            return lrcLibResult;
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        private async Task<List<string>> ResolveAliasesFromLrcLibAsync(string title, string artist, CancellationToken ct)
        {
            var aliases = new List<string>();
            try
            {
                var query = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
                var url = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(query)}";
                var items = await _httpClient.GetFromJsonAsync<List<LrcLibResponse>>(url, ct);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.TrackName))
                        {
                            // Skip explicit Japanese / Korean language versions
                            if (Regex.IsMatch(item.TrackName, @"(?:Japanese|Korean|Jap\.|Kor\.|日文|韓文|韩文|日語|日语|日本語|한국어)", RegexOptions.IgnoreCase))
                            {
                                continue;
                            }

                            // Strictly only extract alias if the candidate trackName contains Chinese characters
                            // and does NOT contain Japanese Kana or Korean Hangul
                            if (Regex.IsMatch(item.TrackName, @"[\u4e00-\u9fa5]") &&
                                !Regex.IsMatch(item.TrackName, @"[\u3040-\u30ff\uac00-\ud7af]"))
                            {
                                if (item.TrackName.Contains(" - "))
                                {
                                    var parts = item.TrackName.Split(" - ");
                                    foreach (var part in parts)
                                    {
                                        var p = CleanYouTubeTitle(part);
                                        if (Regex.IsMatch(p, @"[\u4e00-\u9fa5]") &&
                                            !Regex.IsMatch(p, @"[\u3040-\u30ff\uac00-\ud7af]") &&
                                            !aliases.Contains(p))
                                        {
                                            aliases.Add(p);
                                        }
                                    }
                                }

                                var chineseOnly = Regex.Replace(item.TrackName, @"[^\u4e00-\u9fa5]", "").Trim();
                                if (chineseOnly.Length >= 2 && !aliases.Contains(chineseOnly))
                                {
                                    aliases.Add(chineseOnly);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return aliases;
        }

        public class SearchPair
        {
            public string Title { get; set; } = string.Empty;
            public string Artist { get; set; } = string.Empty;

            public SearchPair(string title, string artist)
            {
                Title = title;
                Artist = artist;
            }
        }

        public static List<SearchPair> GenerateSearchCandidates(string rawTitle, string rawArtist)
        {
            var pairs = new List<SearchPair>();

            void AddPair(string t, string a)
            {
                t = t.Trim();
                a = a.Trim();
                if (string.IsNullOrWhiteSpace(t)) return;

                if (!pairs.Exists(p => p.Title.Equals(t, StringComparison.OrdinalIgnoreCase) &&
                                       p.Artist.Equals(a, StringComparison.OrdinalIgnoreCase)))
                {
                    pairs.Add(new SearchPair(t, a));
                }

                var simT = ToSimplifiedChinese(t);
                var simA = ToSimplifiedChinese(a);
                if (!pairs.Exists(p => p.Title.Equals(simT, StringComparison.OrdinalIgnoreCase) &&
                                       p.Artist.Equals(simA, StringComparison.OrdinalIgnoreCase)))
                {
                    pairs.Add(new SearchPair(simT, simA));
                }
            }

            // Strategy 1: Book Marks 《...》
            var bookMatches = Regex.Matches(rawTitle, @"《(.*?)》");
            var hashtagMatches = Regex.Matches(rawTitle, @"#([\u4e00-\u9fa5A-Za-z0-9_]+)");

            var extractedSingers = new List<string>();
            foreach (Match m in hashtagMatches)
            {
                var tag = m.Groups[1].Value.Trim();
                if (!Regex.IsMatch(tag, @"(?:天赐|声音|歌手|好声音|乘风|披荆|跨界|纯享|舞台|现场|EP\d+|202\d|官方|MV|HD|4K)", RegexOptions.IgnoreCase))
                {
                    if (tag.Length >= 2 && !extractedSingers.Contains(tag))
                    {
                        extractedSingers.Add(tag);
                    }
                }
            }

            foreach (Match bm in bookMatches)
            {
                var bookSong = bm.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(bookSong))
                {
                    // Filter out channel watermarks in book marks:
                    // e.g. 《動態歌詞》, 《动态歌词》, 《歌詞版》, 《高音质》, 《MV》, 《纯享版》, 《完整版》
                    if (Regex.IsMatch(bookSong, @"^(?:動態歌詞|动态歌词|動態|动态|歌詞|歌词|歌詞版|歌词版|純享版|纯享版|官方|高音质|高清|4K|1080P|无损|MV|Live|现场|伴奏|完整版|合集|试听|单曲)$", RegexOptions.IgnoreCase))
                    {
                        continue;
                    }

                    if (extractedSingers.Count > 0)
                    {
                        var combinedSingers = string.Join(" ", extractedSingers);
                        AddPair(bookSong, combinedSingers);
                        foreach (var singer in extractedSingers)
                        {
                            AddPair(bookSong, singer);
                        }
                    }

                    AddPair(bookSong, CleanArtistName(rawArtist));
                    AddPair(bookSong, "");
                }
            }

            // Strategy 2: Hyphen / Dash Split e.g. "我只在乎你 - 張碧晨" or "喜歡-阿肆" or "周杰伦—晴天"
            var cleanedTitle = CleanYouTubeTitle(rawTitle);
            var cleanedArtist = CleanArtistName(rawArtist);

            var separatorMatch = Regex.Match(cleanedTitle, @"^(.*?)\s*[-–—－~|/]\s*(.*)$");
            if (separatorMatch.Success)
            {
                var partA = CleanYouTubeTitle(separatorMatch.Groups[1].Value.Trim());
                var partB = CleanYouTubeTitle(separatorMatch.Groups[2].Value.Trim());

                if (!string.IsNullOrWhiteSpace(partA) && !string.IsNullOrWhiteSpace(partB))
                {
                    AddPair(partA, partB);
                    AddPair(partB, partA);
                    AddPair($"{partA} {partB}", "");
                    AddPair(partA, "");
                    AddPair(partB, "");
                }
            }

            // Strategy 3: Standard cleaned title + artist
            AddPair(cleanedTitle, cleanedArtist);

            // Multi-artist splitting (e.g. "Disney和Shakira" -> "Disney Shakira", "Disney", "Shakira")
            // Handles Chinese/English conjunctions: 和, 与, 及, 、, &, and, feat., ft., x
            var artistParts = Regex.Split(cleanedArtist, @"\s*(?:和|与|及|、|&|\band\b|feat\.?|ft\.?|\bx\b|,|\/)\s*", RegexOptions.IgnoreCase);
            if (artistParts.Length > 1)
            {
                var joinedArtists = string.Join(" ", artistParts.Where(p => !string.IsNullOrWhiteSpace(p)));
                AddPair(cleanedTitle, joinedArtists);
                foreach (var singleArtist in artistParts)
                {
                    if (!string.IsNullOrWhiteSpace(singleArtist))
                    {
                        AddPair(cleanedTitle, singleArtist.Trim());
                    }
                }
            }

            // Strategy 4: Clean Chinese part of mixed artist (e.g. "en王翊恩" -> "王翊恩")
            var artistChinese = Regex.Replace(cleanedArtist, @"[^\u4e00-\u9fa5]", "").Trim();
            if (artistChinese.Length >= 2)
            {
                AddPair(cleanedTitle, artistChinese);
                if (separatorMatch.Success)
                {
                    var partA = CleanYouTubeTitle(separatorMatch.Groups[1].Value.Trim());
                    AddPair(partA, artistChinese);
                }
            }

            // Strategy 5: Title only fallback
            AddPair(cleanedTitle, "");

            return pairs;
        }

        private static string CleanYouTubeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            var t = title.Trim();
            t = t.Replace('（', '(').Replace('）', ')')
                 .Replace('【', '[').Replace('】', ']')
                 .Replace('「', '[').Replace('」', ']')
                 .Replace('『', '[').Replace('』', ']');

            t = Regex.Replace(t, @"\s*\[.*?\]", "", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"\s*\(.*?\)", "", RegexOptions.IgnoreCase);
            t = t.Replace("《", " ").Replace("》", " ");
            t = Regex.Replace(t, @"#[\u4e00-\u9fa5A-Za-z0-9_]+", " ");
            t = Regex.Replace(t, @"!.*$|！.*$", "");
            t = Regex.Replace(t, @"\s*[-–—]\s*(?:official|music\s*video|mv|audio|lyrics|visualizer).*$", "", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"\s*\|\s*.*$", "");

            // Strip trailing unbracketed noise words in a loop
            string prev;
            do
            {
                prev = t;
                t = Regex.Replace(t, @"\s*(?:動態歌詞|动态歌词|歌詞版|歌词版|歌詞|歌词|lyrics\s*video|lyric\s*video|lyrics|lyric|audio\s*video|audio|video|official|cover|完整版|高音质|高清|无损|纯享版|纯享|4k|1080p)\s*$", "", RegexOptions.IgnoreCase);
            } while (t != prev);

            return Regex.Replace(t, @"\s+", " ").Trim();
        }

        private static string CleanArtistName(string artist)
        {
            if (string.IsNullOrWhiteSpace(artist)) return string.Empty;

            var a = artist.Trim();

            // Handle YouTube Music interpunct album/year separation:
            // e.g. "WeiBird · Red Scarf ("Till We Meet Again" Movie Theme Song)" -> "WeiBird"
            // e.g. "Disney和Shakira · Zoo (From "Zootopia 2") · 2025年" -> "Disney和Shakira"
            if (a.Contains("·") || a.Contains("•"))
            {
                var parts = a.Split(new[] { '·', '•' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    a = parts[0].Trim();
                }
            }

            // Normalize Chinese and formatting conjunctions between artists (e.g. "Disney和Shakira" -> "Disney Shakira")
            a = Regex.Replace(a, @"\s*(?:和|与|及|、)\s*", " ");
            a = Regex.Replace(a, @"\s*-\s*Topic$", "", RegexOptions.IgnoreCase);
            a = Regex.Replace(a, @"\s*VEVO$", "", RegexOptions.IgnoreCase);
            return Regex.Replace(a, @"\s+", " ").Trim();
        }

        private static string ToSimplifiedChinese(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var map = new Dictionary<char, char>
            {
                {'歡', '欢'}, {'愛', '爱'}, {'說', '说'}, {'話', '话'}, {'對', '对'},
                {'為', '为'}, {'與', '与'}, {'聽', '听'}, {'見', '见'}, {'開', '开'},
                {'關', '关'}, {'過', '过'}, {'還', '还'}, {'這', '这'}, {'時', '时'},
                {'後', '后'}, {'動', '动'}, {'態', '态'}, {'詞', '词'}, {'樂', '乐'},
                {'風', '风'}, {'帶', '带'}, {'姓', '姓'}, {'連', '连'}, {'點', '点'},
                {'飛', '飞'}, {'傷', '伤'}, {'夢', '梦'}, {'難', '难'}, {'淚', '泪'},
                {'發', '发'}, {'變', '变'}, {'讓', '让'}, {'誰', '谁'}, {'給', '给'},
                {'從', '从'}, {'來', '来'}, {'個', '个'}, {'們', '们'}, {'麼', '么'},
                {'樣', '样'}, {'頭', '头'}, {'電', '电'}, {'視', '视'}, {'劇', '剧'},
                {'卻', '却'}, {'扛', '扛'}, {'純', '纯'}, {'享', '享'}, {'溫', '温'},
                {'暖', '暖'}, {'治', '治'}, {'癒', '愈'}, {'旋', '旋'}, {'律', '律'},
                {'飆', '飙'}, {'升', '升'}, {'聲', '声'}, {'音', '音'}, {'錄', '录'},
                {'雛', '雏'}, {'孤', '孤'}, {'張', '张'}, {'華', '华'}, {'語', '语'},
                {'國', '国'}, {'鄧', '邓'}, {'麗', '丽'}, {'君', '君'}, {'陳', '陈'},
                {'劉', '刘'}, {'黃', '黄'}, {'鄭', '郑'}, {'謝', '谢'}, {'鍾', '钟'},
                {'韋', '韦'}, {'禮', '礼'}, {'安', '安'}
            };

            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                sb.Append(map.TryGetValue(c, out var sim) ? sim : c);
            }
            return sb.ToString();
        }

        private static bool IsLatinOnly(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            foreach (var c in text)
            {
                if ((c >= 0x4E00 && c <= 0x9FFF) ||
                    (c >= 0x3400 && c <= 0x4DBF) ||
                    (c >= 0x3040 && c <= 0x30FF) ||
                    (c >= 0xAC00 && c <= 0xD7AF))
                {
                    return false;
                }
            }
            return true;
        }

        private async Task<List<LrcLine>?> FetchFromKugouAsync(string title, string artist, TimeSpan duration, CancellationToken ct)
        {
            var keyword = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var durationMs = (int)duration.TotalMilliseconds;

            var searchUrl = $"http://mobilecdn.kugou.com/api/v3/search/song?format=json&keyword={Uri.EscapeDataString(keyword)}&page=1&pagesize=6";
            var searchResp = await _httpClient.GetAsync(searchUrl, ct);
            if (!searchResp.IsSuccessStatusCode) return null;

            using var searchDoc = await JsonDocument.ParseAsync(await searchResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!searchDoc.RootElement.TryGetProperty("data", out var dataElem) ||
                !dataElem.TryGetProperty("info", out var infoElem) ||
                infoElem.GetArrayLength() == 0)
                return null;

            JsonElement bestSong = default;
            int minDurationDiff = int.MaxValue;

            for (int i = 0; i < infoElem.GetArrayLength(); i++)
            {
                var item = infoElem[i];
                if (item.TryGetProperty("duration", out var durElem) && durElem.TryGetInt32(out var durSec))
                {
                    int diff = durationMs > 0 ? Math.Abs(durSec * 1000 - durationMs) : 0;
                    if (diff < minDurationDiff)
                    {
                        minDurationDiff = diff;
                        bestSong = item;
                        if (diff <= 3000) break;
                    }
                }
            }

            if (bestSong.ValueKind == JsonValueKind.Undefined)
                bestSong = infoElem[0];

            var hash = bestSong.GetProperty("hash").GetString();
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

        private async Task<List<LrcLine>?> FetchFromNetEaseAsync(string title, string artist, TimeSpan duration, CancellationToken ct)
        {
            var keyword = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var searchUrl = $"https://music.163.com/api/search/get/web?s={Uri.EscapeDataString(keyword)}&type=1&offset=0&total=true&limit=5";

            var response = await _httpClient.GetAsync(searchUrl, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("result", out var resultElem))
                return null;
            if (!resultElem.TryGetProperty("songs", out var songsElem) || songsElem.GetArrayLength() == 0)
                return null;

            JsonElement bestSong = default;
            int minDurationDiff = int.MaxValue;

            for (int i = 0; i < songsElem.GetArrayLength(); i++)
            {
                var item = songsElem[i];
                if (item.TryGetProperty("duration", out var durElem) && durElem.TryGetInt64(out var durMs))
                {
                    int diff = duration > TimeSpan.Zero ? (int)Math.Abs(durMs - duration.TotalMilliseconds) : 0;
                    if (diff < minDurationDiff)
                    {
                        minDurationDiff = diff;
                        bestSong = item;
                        if (diff <= 3000) break;
                    }
                }
            }

            if (bestSong.ValueKind == JsonValueKind.Undefined)
                bestSong = songsElem[0];

            var songId = bestSong.GetProperty("id").GetInt64();
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

        private async Task<List<LrcLine>?> FetchFromQQMusicAsync(string title, string artist, TimeSpan duration, CancellationToken ct)
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

            JsonElement bestSong = default;
            int minDurationDiff = int.MaxValue;

            for (int i = 0; i < listElem.GetArrayLength(); i++)
            {
                var item = listElem[i];
                if (item.TryGetProperty("interval", out var intElem) && intElem.TryGetInt32(out var intervalSec))
                {
                    int diff = duration > TimeSpan.Zero ? (int)Math.Abs(intervalSec * 1000 - duration.TotalMilliseconds) : 0;
                    if (diff < minDurationDiff)
                    {
                        minDurationDiff = diff;
                        if (diff <= 3000)
                        {
                            bestSong = item;
                            break;
                        }
                    }
                }
            }

            if (bestSong.ValueKind == JsonValueKind.Undefined)
                bestSong = listElem[0];

            var songMid = bestSong.GetProperty("songmid").GetString();
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
            var url = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(title)}";
            if (!string.IsNullOrWhiteSpace(artist))
            {
                url += $"&artist_name={Uri.EscapeDataString(artist)}";
            }
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
                    if (string.IsNullOrWhiteSpace(item.SyncedLyrics)) continue;

                    if (IsRelevantMatch(title, item.TrackName, item.ArtistName))
                    {
                        return LrcParser.Parse(item.SyncedLyrics);
                    }
                }
            }

            return null;
        }

        private static bool IsRelevantMatch(string expectedTitle, string? returnedTrack, string? returnedArtist)
        {
            if (string.IsNullOrWhiteSpace(expectedTitle) || string.IsNullOrWhiteSpace(returnedTrack))
                return false;

            var normExpected = expectedTitle.Replace(" ", "").ToLowerInvariant();
            var normTrack = returnedTrack.Replace(" ", "").ToLowerInvariant();

            if (normTrack.Contains(normExpected) || normExpected.Contains(normTrack))
                return true;

            return false;
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
