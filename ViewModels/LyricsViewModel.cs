using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopLyrics.Models;
using DesktopLyrics.Services;

namespace DesktopLyrics.ViewModels
{
    public class LyricsViewModel : INotifyPropertyChanged
    {
        private readonly MediaSessionService _mediaService;
        private readonly LyricsService _lyricsService;
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _syncTimer;

        private List<LrcLine> _currentLyrics = new();
        private CancellationTokenSource? _lyricsFetchCts;
        private int _currentLineIndex = -1;

        private string _primaryLyric = "DesktopLyrics 准备就绪";
        private string _secondaryLyric = "在 YouTube Music 中播放音乐开始同步";
        private string _trackTitle = "等待播放";
        private string _trackArtist = "YouTube Music";
        private ImageSource? _coverArt;
        private bool _isPlaying = false;
        private bool _isLocked = true;
        private double _progressRatio = 0.0;
        private string _sourceStatus = "就绪";
        private bool _isDualLine = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string PrimaryLyric
        {
            get => _primaryLyric;
            set => SetField(ref _primaryLyric, value);
        }

        public string SecondaryLyric
        {
            get => _secondaryLyric;
            set => SetField(ref _secondaryLyric, value);
        }

        public string TrackTitle
        {
            get => _trackTitle;
            set => SetField(ref _trackTitle, value);
        }

        public string TrackArtist
        {
            get => _trackArtist;
            set => SetField(ref _trackArtist, value);
        }

        public ImageSource? CoverArt
        {
            get => _coverArt;
            set => SetField(ref _coverArt, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetField(ref _isPlaying, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => SetField(ref _isLocked, value);
        }

        public double ProgressRatio
        {
            get => _progressRatio;
            set => SetField(ref _progressRatio, value);
        }

        public string SourceStatus
        {
            get => _sourceStatus;
            set => SetField(ref _sourceStatus, value);
        }

        public bool IsDualLine
        {
            get => _isDualLine;
            set => SetField(ref _isDualLine, value);
        }

        public AppSettings Settings => _settingsService.Settings;

        public LyricsViewModel(MediaSessionService mediaService, LyricsService lyricsService, SettingsService settingsService)
        {
            _mediaService = mediaService;
            _lyricsService = lyricsService;
            _settingsService = settingsService;

            _isLocked = _settingsService.Settings.IsLocked;
            _isDualLine = _settingsService.Settings.IsDualLine;

            _mediaService.TrackChanged += OnTrackChanged;
            _mediaService.PlaybackStateChanged += OnPlaybackStateChanged;

            _syncTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(25) // ~40 FPS smooth sync
            };
            _syncTimer.Tick += OnSyncTick;
            _syncTimer.Start();
        }

        public async Task TogglePlayPauseAsync()
        {
            await _mediaService.TogglePlayPauseAsync();
        }

        public async Task PreviousTrackAsync()
        {
            await _mediaService.PreviousTrackAsync();
        }

        public async Task NextTrackAsync()
        {
            await _mediaService.NextTrackAsync();
        }

        private void OnPlaybackStateChanged(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }

        private async void OnTrackChanged(MediaTrackInfo track)
        {
            _lyricsFetchCts?.Cancel();
            _lyricsFetchCts = new CancellationTokenSource();
            var ct = _lyricsFetchCts.Token;

            TrackTitle = string.IsNullOrWhiteSpace(track.Title) ? "未在播放" : track.Title;
            TrackArtist = string.IsNullOrWhiteSpace(track.Artist) ? "YouTube Music" : track.Artist;
            CoverArt = track.Thumbnail;
            IsPlaying = track.IsPlaying;
            _currentLineIndex = -1;

            if (track.IsEmpty)
            {
                PrimaryLyric = "等待媒体播放...";
                SecondaryLyric = "支持 YouTube Music / 浏览器标签页";
                SourceStatus = "空闲";
                ProgressRatio = 0;
                _currentLyrics.Clear();
                return;
            }

            PrimaryLyric = track.Title;
            SecondaryLyric = string.IsNullOrWhiteSpace(track.Artist) ? "正在获取歌词..." : track.Artist;
            SourceStatus = "正在检索歌词...";

            try
            {
                var lyrics = await _lyricsService.FetchLyricsAsync(track.Title, track.Artist, track.Duration, ct);
                if (ct.IsCancellationRequested) return;

                if (lyrics != null && lyrics.Count > 0)
                {
                    _currentLyrics = lyrics;
                    SourceStatus = $"已同步 {lyrics.Count} 行歌词";
                }
                else
                {
                    _currentLyrics.Clear();
                    PrimaryLyric = track.Title;
                    SecondaryLyric = string.IsNullOrWhiteSpace(track.Artist) ? "未找到同步歌词" : $"{track.Artist} (无歌词)";
                    SourceStatus = "无歌词";
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SourceStatus = "歌词获取失败";
                SecondaryLyric = ex.Message;
            }
        }

        private void OnSyncTick(object? sender, EventArgs e)
        {
            if (!_isPlaying && _currentLyrics.Count == 0)
                return;

            var currentTime = _mediaService.GetCurrentAccuratePosition();
            var totalDuration = _mediaService.CurrentTrack.Duration;

            if (totalDuration > TimeSpan.Zero)
            {
                ProgressRatio = Math.Clamp(currentTime.TotalSeconds / totalDuration.TotalSeconds, 0.0, 1.0);
            }

            if (_currentLyrics.Count == 0)
                return;

            int activeIdx = LrcParser.FindActiveIndex(_currentLyrics, currentTime);

            if (activeIdx != _currentLineIndex)
            {
                _currentLineIndex = activeIdx;
                if (_currentLineIndex >= 0 && _currentLineIndex < _currentLyrics.Count)
                {
                    PrimaryLyric = _currentLyrics[_currentLineIndex].Text;
                    
                    // Next line preview
                    if (_currentLineIndex + 1 < _currentLyrics.Count)
                    {
                        SecondaryLyric = _currentLyrics[_currentLineIndex + 1].Text;
                    }
                    else
                    {
                        SecondaryLyric = "♪ 伴奏 / 尾奏 ♪";
                    }
                }
                else
                {
                    PrimaryLyric = _mediaService.CurrentTrack.Title;
                    SecondaryLyric = _mediaService.CurrentTrack.Artist;
                }
            }
        }

        public void ToggleLock()
        {
            IsLocked = !IsLocked;
            _settingsService.Settings.IsLocked = IsLocked;
            _settingsService.SaveSettings();
        }

        public void ToggleDualLine()
        {
            IsDualLine = !IsDualLine;
            _settingsService.Settings.IsDualLine = IsDualLine;
            _settingsService.SaveSettings();
        }

        public void SavePosition(double left, double top, double width, double height)
        {
            _settingsService.Settings.Left = left;
            _settingsService.Settings.Top = top;
            _settingsService.Settings.Width = width;
            _settingsService.Settings.Height = height;
            _settingsService.SaveSettings();
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
