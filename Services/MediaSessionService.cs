using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using DesktopLyrics.Models;

namespace DesktopLyrics.Services
{
    public class MediaSessionService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        
        // Accurate playback time interpolation
        private TimeSpan _lastReportedPosition = TimeSpan.Zero;
        private DateTime _lastPositionUpdateTime = DateTime.UtcNow;
        private bool _isPlaying = false;
        private double _playbackRate = 1.0;

        public event Action<MediaTrackInfo>? TrackChanged;
        public event Action<bool>? PlaybackStateChanged;

        public MediaTrackInfo CurrentTrack { get; private set; } = new();

        public async Task InitializeAsync()
        {
            try
            {
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_sessionManager != null)
                {
                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                    UpdateCurrentSession(_sessionManager.GetCurrentSession());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaSessionService] Failed to initialize SMTC: {ex.Message}");
            }
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentSession(sender.GetCurrentSession());
        }

        private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSession? session)
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }

            _currentSession = session;

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

                _ = RefreshMediaInfoAsync();
            }
            else
            {
                CurrentTrack = new MediaTrackInfo();
                _isPlaying = false;
                TrackChanged?.Invoke(CurrentTrack);
                PlaybackStateChanged?.Invoke(false);
            }
        }

        private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await RefreshMediaInfoAsync();
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            RefreshPlaybackState();
        }

        private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            RefreshTimeline();
        }

        public async Task RefreshMediaInfoAsync()
        {
            if (_currentSession == null)
                return;

            try
            {
                var props = await _currentSession.TryGetMediaPropertiesAsync();
                var playbackInfo = _currentSession.GetPlaybackInfo();
                var timeline = _currentSession.GetTimelineProperties();

                var isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                _isPlaying = isPlaying;
                _playbackRate = playbackInfo?.PlaybackRate ?? 1.0;
                if (_playbackRate <= 0) _playbackRate = 1.0;

                _lastReportedPosition = timeline?.Position ?? TimeSpan.Zero;
                _lastPositionUpdateTime = timeline?.LastUpdatedTime.UtcDateTime ?? DateTime.UtcNow;

                BitmapImage? coverArt = null;
                if (props?.Thumbnail != null)
                {
                    try
                    {
                        using var stream = await props.Thumbnail.OpenReadAsync();
                        using var netStream = stream.AsStreamForRead();
                        using var ms = new MemoryStream();
                        await netStream.CopyToAsync(ms);
                        ms.Position = 0;

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        coverArt = bitmap;
                    }
                    catch (Exception thumbEx)
                    {
                        Debug.WriteLine($"[MediaSessionService] Thumbnail load error: {thumbEx.Message}");
                    }
                }

                var track = new MediaTrackInfo
                {
                    Title = props?.Title ?? string.Empty,
                    Artist = props?.Artist ?? string.Empty,
                    AlbumTitle = props?.AlbumTitle ?? string.Empty,
                    IsPlaying = isPlaying,
                    Position = _lastReportedPosition,
                    Duration = timeline?.EndTime ?? TimeSpan.Zero,
                    SourceAppId = _currentSession.SourceAppUserModelId ?? string.Empty,
                    Thumbnail = coverArt,
                    LastUpdatedTime = _lastPositionUpdateTime
                };

                CurrentTrack = track;
                TrackChanged?.Invoke(track);
                PlaybackStateChanged?.Invoke(isPlaying);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaSessionService] Error refreshing media info: {ex.Message}");
            }
        }

        public async Task TogglePlayPauseAsync()
        {
            if (_currentSession != null)
            {
                try { await _currentSession.TryTogglePlayPauseAsync(); } catch { }
            }
        }

        public async Task PreviousTrackAsync()
        {
            if (_currentSession != null)
            {
                try { await _currentSession.TrySkipPreviousAsync(); } catch { }
            }
        }

        public async Task NextTrackAsync()
        {
            if (_currentSession != null)
            {
                try { await _currentSession.TrySkipNextAsync(); } catch { }
            }
        }

        private void RefreshPlaybackState()
        {
            if (_currentSession == null) return;
            try
            {
                var playbackInfo = _currentSession.GetPlaybackInfo();
                var isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                
                RefreshTimeline();
                _isPlaying = isPlaying;
                CurrentTrack.IsPlaying = isPlaying;
                PlaybackStateChanged?.Invoke(isPlaying);
            }
            catch { }
        }

        private void RefreshTimeline()
        {
            if (_currentSession == null) return;
            try
            {
                var timeline = _currentSession.GetTimelineProperties();
                if (timeline != null)
                {
                    _lastReportedPosition = timeline.Position;
                    _lastPositionUpdateTime = timeline.LastUpdatedTime.UtcDateTime;
                    CurrentTrack.Duration = timeline.EndTime;
                    CurrentTrack.Position = _lastReportedPosition;
                }
            }
            catch { }
        }

        public TimeSpan GetCurrentAccuratePosition()
        {
            if (!_isPlaying)
            {
                return _lastReportedPosition;
            }

            var elapsed = (DateTime.UtcNow - _lastPositionUpdateTime).TotalSeconds * _playbackRate;
            var estimated = _lastReportedPosition + TimeSpan.FromSeconds(elapsed);

            if (CurrentTrack.Duration > TimeSpan.Zero && estimated > CurrentTrack.Duration)
            {
                estimated = CurrentTrack.Duration;
            }

            if (estimated < TimeSpan.Zero)
            {
                estimated = TimeSpan.Zero;
            }

            return estimated;
        }
    }
}
