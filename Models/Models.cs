using System;
using System.Windows.Media;

namespace DesktopLyrics.Models
{
    public class LrcLine
    {
        public TimeSpan StartTime { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;

        public override string ToString() => $"[{StartTime:mm\\:ss\\.ff}] {Text}";
    }

    public class MediaTrackInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string AlbumTitle { get; set; } = string.Empty;
        public bool IsPlaying { get; set; }
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
        public string SourceAppId { get; set; } = string.Empty;
        public ImageSource? Thumbnail { get; set; }
        public DateTime LastUpdatedTime { get; set; } = DateTime.UtcNow;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Title);

        public override string ToString() => $"{Title} - {Artist} ({(IsPlaying ? "Playing" : "Paused")})";
    }

    public class AppSettings
    {
        public double Left { get; set; } = -1;
        public double Top { get; set; } = -1;
        public double Width { get; set; } = 560;
        public double Height { get; set; } = 42;
        public bool IsLocked { get; set; } = true;
        public bool IsDualLine { get; set; } = true;
        public double FontSize { get; set; } = 14;
        public string TextColor { get; set; } = "#FFFFFF";
        public string AccentColor { get; set; } = "#FF4081";
        public bool ShowProgressIndicator { get; set; } = true;
        public bool AutoSearchLyrics { get; set; } = true;
    }
}
