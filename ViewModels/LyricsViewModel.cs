using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using DesktopLyrics.Models;
using DesktopLyrics.Services;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

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

        private string _primaryLyric = "Waiting for media playback...";
        private string _secondaryLyric = "Supports YouTube Music, Spotify & Browser Media";
        private string _trackTitle = "Not playing";
        private string _trackArtist = "YouTube Music";
        private ImageSource? _coverArt;
        private bool _isPlaying = false;
        private bool _isLocked = true;
        private double _progressRatio = 0.0;
        private string _sourceStatus = "Idle";
        private bool _isDualLine = false;
        private AppTheme _theme = AppTheme.Auto;
        private AppLanguage _language = AppLanguage.English;

        // Dynamic theme brushes
        private Brush _primaryTextBrush = new SolidColorBrush(Colors.White);
        private Brush _secondaryTextBrush = new SolidColorBrush(Color.FromRgb(170, 176, 190));
        private Brush _dividerBrush = new SolidColorBrush(Color.FromArgb(37, 255, 255, 255));
        private Brush _controlButtonBgBrush = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255));
        private Brush _controlIconBrush = new SolidColorBrush(Colors.White);

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

        public AppTheme Theme
        {
            get => _theme;
            set
            {
                if (SetField(ref _theme, value))
                {
                    ApplyTheme();
                }
            }
        }

        public AppLanguage Language
        {
            get => _language;
            set
            {
                if (SetField(ref _language, value))
                {
                    I18n.CurrentLanguage = value;
                    UpdateLanguagePlaceholders();
                }
            }
        }

        public Brush PrimaryTextBrush
        {
            get => _primaryTextBrush;
            set => SetField(ref _primaryTextBrush, value);
        }

        public Brush SecondaryTextBrush
        {
            get => _secondaryTextBrush;
            set => SetField(ref _secondaryTextBrush, value);
        }

        public Brush DividerBrush
        {
            get => _dividerBrush;
            set => SetField(ref _dividerBrush, value);
        }

        public Brush ControlButtonBgBrush
        {
            get => _controlButtonBgBrush;
            set => SetField(ref _controlButtonBgBrush, value);
        }

        public Brush ControlIconBrush
        {
            get => _controlIconBrush;
            set => SetField(ref _controlIconBrush, value);
        }

        public AppSettings Settings => _settingsService.Settings;

        public LyricsViewModel(MediaSessionService mediaService, LyricsService lyricsService, SettingsService settingsService)
        {
            _mediaService = mediaService;
            _lyricsService = lyricsService;
            _settingsService = settingsService;

            _isLocked = _settingsService.Settings.IsLocked;
            _isDualLine = _settingsService.Settings.IsDualLine;
            _theme = _settingsService.Settings.Theme;
            _language = _settingsService.Settings.Language;
            I18n.CurrentLanguage = _language;

            _primaryLyric = I18n.WaitingForPlayback;
            _secondaryLyric = I18n.SupportedMediaSources;
            _sourceStatus = I18n.Idle;
            _trackTitle = I18n.NotPlaying;

            ApplyTheme();

            _mediaService.TrackChanged += OnTrackChanged;
            _mediaService.PlaybackStateChanged += OnPlaybackStateChanged;

            _syncTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(25) // ~40 FPS smooth sync
            };
            _syncTimer.Tick += OnSyncTick;
            _syncTimer.Start();
        }

        public void SetTheme(AppTheme theme)
        {
            Theme = theme;
            _settingsService.Settings.Theme = theme;
            _settingsService.SaveSettings();
        }

        public void SetLanguage(AppLanguage lang)
        {
            Language = lang;
            _settingsService.Settings.Language = lang;
            _settingsService.SaveSettings();
        }

        public void UpdateLanguagePlaceholders()
        {
            if (_mediaService.CurrentTrack.IsEmpty)
            {
                PrimaryLyric = I18n.WaitingForPlayback;
                SecondaryLyric = I18n.SupportedMediaSources;
                SourceStatus = I18n.Idle;
                TrackTitle = I18n.NotPlaying;
            }
            else if (_currentLyrics.Count == 0)
            {
                PrimaryLyric = _mediaService.CurrentTrack.Title;
                SecondaryLyric = I18n.NoLyricsFound;
                SourceStatus = I18n.NoLyricsFound;
            }
        }

        public void ApplyTheme()
        {
            bool isLight = false;

            if (_theme == AppTheme.Light)
            {
                isLight = true;
            }
            else if (_theme == AppTheme.Dark)
            {
                isLight = false;
            }
            else // Auto
            {
                isLight = DetectWindowsLightTheme();
            }

            if (isLight)
            {
                PrimaryTextBrush = new SolidColorBrush(Color.FromRgb(24, 25, 28));
                SecondaryTextBrush = new SolidColorBrush(Color.FromRgb(85, 94, 109));
                DividerBrush = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0));
                ControlButtonBgBrush = new SolidColorBrush(Color.FromArgb(24, 0, 0, 0));
                ControlIconBrush = new SolidColorBrush(Color.FromRgb(24, 25, 28));
            }
            else
            {
                PrimaryTextBrush = new SolidColorBrush(Colors.White);
                SecondaryTextBrush = new SolidColorBrush(Color.FromRgb(170, 176, 190));
                DividerBrush = new SolidColorBrush(Color.FromArgb(37, 255, 255, 255));
                ControlButtonBgBrush = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255));
                ControlIconBrush = new SolidColorBrush(Colors.White);
            }
        }

        private static bool DetectWindowsLightTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("SystemUsesLightTheme") is int val)
                {
                    return val == 1;
                }
            }
            catch { }
            return false;
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

            TrackTitle = string.IsNullOrWhiteSpace(track.Title) ? I18n.NotPlaying : track.Title;
            TrackArtist = string.IsNullOrWhiteSpace(track.Artist) ? "YouTube Music" : track.Artist;
            CoverArt = track.Thumbnail;
            IsPlaying = track.IsPlaying;
            _currentLineIndex = -1;

            if (track.IsEmpty)
            {
                PrimaryLyric = I18n.WaitingForPlayback;
                SecondaryLyric = I18n.SupportedMediaSources;
                SourceStatus = I18n.Idle;
                ProgressRatio = 0;
                _currentLyrics.Clear();
                return;
            }

            PrimaryLyric = track.Title;
            SecondaryLyric = string.IsNullOrWhiteSpace(track.Artist) ? I18n.FetchingLyrics : track.Artist;
            SourceStatus = I18n.FetchingLyrics;

            try
            {
                var lyrics = await _lyricsService.FetchLyricsAsync(track.Title, track.Artist, track.Duration, ct);
                if (ct.IsCancellationRequested) return;

                if (lyrics != null && lyrics.Count > 0)
                {
                    _currentLyrics = lyrics;
                    SourceStatus = I18n.SyncedLines(lyrics.Count);
                }
                else
                {
                    _currentLyrics.Clear();
                    PrimaryLyric = track.Title;
                    SecondaryLyric = string.IsNullOrWhiteSpace(track.Artist) ? I18n.NoLyricsFound : $"{track.Artist}";
                    SourceStatus = I18n.NoLyricsFound;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SourceStatus = I18n.LyricsFetchFailed;
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

                    if (_isDualLine)
                    {
                        if (!string.IsNullOrWhiteSpace(_currentLyrics[_currentLineIndex].Translation))
                        {
                            SecondaryLyric = _currentLyrics[_currentLineIndex].Translation;
                        }
                        else if (_currentLineIndex + 1 < _currentLyrics.Count)
                        {
                            SecondaryLyric = _currentLyrics[_currentLineIndex + 1].Text;
                        }
                        else
                        {
                            SecondaryLyric = string.Empty;
                        }
                    }
                    else
                    {
                        SecondaryLyric = string.IsNullOrWhiteSpace(_trackArtist) ? "YouTube Music" : _trackArtist;
                    }
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
