using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DesktopLyrics.Models;

namespace DesktopLyrics.Services
{
    public class LrcParser
    {
        private static readonly Regex TimeTagRegex = new(@"\[(?<time>\d{1,2}:\d{1,2}(?:\.\d{1,3})?)\]", RegexOptions.Compiled);
        private static readonly Regex OffsetRegex = new(@"\[offset:(?<offset>[+-]?\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<LrcLine> Parse(string lrcContent)
        {
            var lines = new List<LrcLine>();
            if (string.IsNullOrWhiteSpace(lrcContent))
                return lines;

            int offsetMs = 0;
            var offsetMatch = OffsetRegex.Match(lrcContent);
            if (offsetMatch.Success && int.TryParse(offsetMatch.Groups["offset"].Value, out int parsedOffset))
            {
                offsetMs = parsedOffset;
            }

            var rawLines = lrcContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in rawLines)
            {
                var matches = TimeTagRegex.Matches(rawLine);
                if (matches.Count == 0)
                    continue;

                // Extract lyric text after all time tags
                var text = TimeTagRegex.Replace(rawLine, string.Empty).Trim();

                // If text is metadata like "[ar:...]", skip it
                if (text.StartsWith('[') && text.EndsWith(']'))
                    continue;

                foreach (Match match in matches)
                {
                    var timeStr = match.Groups["time"].Value;
                    if (TryParseTime(timeStr, out var timeSpan))
                    {
                        var adjustedTime = timeSpan.Add(TimeSpan.FromMilliseconds(offsetMs));
                        if (adjustedTime < TimeSpan.Zero)
                            adjustedTime = TimeSpan.Zero;

                        lines.Add(new LrcLine
                        {
                            StartTime = adjustedTime,
                            Text = text
                        });
                    }
                }
            }

            return lines.OrderBy(l => l.StartTime).ToList();
        }

        private static bool TryParseTime(string timeStr, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            var parts = timeStr.Split(':');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out int minutes))
                return false;

            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                return false;

            timeSpan = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
            return true;
        }

        public static int FindActiveIndex(IReadOnlyList<LrcLine> lyrics, TimeSpan currentTime)
        {
            if (lyrics == null || lyrics.Count == 0)
                return -1;

            if (currentTime < lyrics[0].StartTime)
                return -1;

            int low = 0;
            int high = lyrics.Count - 1;
            int result = -1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (lyrics[mid].StartTime <= currentTime)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return result;
        }
    }
}
