using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopLyrics.Utils;
using DesktopLyrics.ViewModels;

namespace DesktopLyrics.Views
{
    public partial class TaskbarLyricsWindow : Window
    {
        public LyricsViewModel ViewModel { get; }
        private HwndSource? _hwndSource;
        private readonly DispatcherTimer _topmostGuardTimer;

        private Storyboard? _singleLineStoryboard;
        private Storyboard? _dualPrimaryStoryboard;
        private Storyboard? _dualSecondaryStoryboard;

        public TaskbarLyricsWindow(LyricsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            SourceInitialized += OnSourceInitialized;
            Loaded += OnWindowLoaded;
            MouseEnter += OnWindowMouseEnter;
            MouseLeave += OnWindowMouseLeave;

            // Heartbeat to keep topmost above Shell_TrayWnd
            _topmostGuardTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _topmostGuardTimer.Tick += (s, e) =>
            {
                Win32Helper.EnsureTopMost(this);
            };
            _topmostGuardTimer.Start();
        }

        private void OnWindowMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HoverControlsPanel.IsHitTestVisible = true;
        }

        private void OnWindowMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HoverControlsPanel.IsHitTestVisible = false;
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.PreviousTrackAsync();
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.TogglePlayPauseAsync();
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.NextTrackAsync();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(WndProc);

            // Attach window to Taskbar (Shell_TrayWnd) as owner
            Win32Helper.AttachToTaskbar(this);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Prevent taking focus when clicked
            if (msg == Win32Helper.WM_MOUSEACTIVATE)
            {
                handled = true;
                return (IntPtr)Win32Helper.MA_NOACTIVATE;
            }

            // Ensure window maintains topmost Z-order when other windows reorder
            if (msg == Win32Helper.WM_WINDOWPOSCHANGING)
            {
                try
                {
                    var pos = Marshal.PtrToStructure<Win32Helper.WINDOWPOS>(lParam);
                    pos.hwndInsertAfter = Win32Helper.HWND_TOPMOST;
                    Marshal.StructureToPtr(pos, lParam, true);
                }
                catch { }
            }

            return IntPtr.Zero;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            PositionWindowOnTaskbar();
            ApplyLockState();
            Dispatcher.InvokeAsync(UpdateAllMarquees, DispatcherPriority.Loaded);
        }

        private void PositionWindowOnTaskbar()
        {
            var settings = ViewModel.Settings;

            if (settings.Left >= 0 && settings.Top >= 0)
            {
                Left = settings.Left;
                Top = settings.Top;
                Width = settings.Width > 200 ? settings.Width : 660;
                Height = settings.Height > 20 ? settings.Height : 44;
            }
            else
            {
                var taskbarRect = Win32Helper.GetTaskbarRect();

                double dpiFactor = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                double taskbarTopDip = taskbarRect.Top / dpiFactor;
                double taskbarHeightDip = taskbarRect.Height / dpiFactor;

                Left = 200;
                Top = taskbarTopDip + Math.Max(2, (taskbarHeightDip - Height) / 2);
                Width = 660;
                Height = Math.Min(44, taskbarHeightDip - 4);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.IsLocked))
            {
                ApplyLockState();
            }
            else if (e.PropertyName == nameof(ViewModel.PrimaryLyric) ||
                     e.PropertyName == nameof(ViewModel.SecondaryLyric) ||
                     e.PropertyName == nameof(ViewModel.IsDualLine))
            {
                Dispatcher.InvokeAsync(UpdateAllMarquees, DispatcherPriority.Loaded);
            }
        }

        private void UpdateAllMarquees()
        {
            if (!IsLoaded) return;

            if (!ViewModel.IsDualLine)
            {
                AnimateMarquee(SingleLineClipBorder, SingleLineTextBlock, SingleLineMarqueeTranslate, ref _singleLineStoryboard);
            }
            else
            {
                AnimateMarquee(DualLinePrimaryClipBorder, DualLinePrimaryTextBlock, DualLinePrimaryMarqueeTranslate, ref _dualPrimaryStoryboard);
                AnimateMarquee(DualLineSecondaryClipBorder, DualLineSecondaryTextBlock, DualLineSecondaryMarqueeTranslate, ref _dualSecondaryStoryboard);
            }
        }

        private void AnimateMarquee(Border clipBorder, TextBlock textBlock, TranslateTransform transform, ref Storyboard? activeStoryboard)
        {
            activeStoryboard?.Stop();
            activeStoryboard = null;
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            transform.X = 0;

            if (string.IsNullOrWhiteSpace(textBlock.Text)) return;

            textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = textBlock.DesiredSize.Width;
            double containerWidth = clipBorder.ActualWidth;

            if (containerWidth <= 0)
            {
                containerWidth = Math.Max(200, ActualWidth - 230);
            }

            // Only scroll if text genuinely overflows past the right border edge
            if (textWidth > containerWidth + 2)
            {
                // Exact pixel distance so the very last character lands flush with the right edge
                double overflow = textWidth - containerWidth;
                double scrollDuration = Math.Max(2.5, overflow / 22.0);

                var sb = new Storyboard
                {
                    FillBehavior = FillBehavior.HoldEnd
                };

                var anim = new DoubleAnimation
                {
                    From = 0,
                    To = -overflow,
                    Duration = TimeSpan.FromSeconds(scrollDuration),
                    BeginTime = TimeSpan.FromSeconds(0.1),
                    DecelerationRatio = 0.2, // soft, precise landing
                    FillBehavior = FillBehavior.HoldEnd
                };

                Storyboard.SetTarget(anim, textBlock);
                Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                sb.Children.Add(anim);
                activeStoryboard = sb;
                sb.Begin();
            }
        }

        private void ApplyLockState()
        {
            Dispatcher.Invoke(() =>
            {
                // We keep NOACTIVATE + TOOLWINDOW + TOPMOST so hover buttons are clickable without stealing focus
                Win32Helper.SetClickThrough(this, false);
                Win32Helper.EnsureTopMost(this);
            });
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!ViewModel.IsLocked && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
                ViewModel.SavePosition(Left, Top, Width, Height);
            }
        }

        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            if (!ViewModel.IsLocked)
            {
                ViewModel.SavePosition(Left, Top, Width, Height);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ViewModel.IsLocked)
            {
                ViewModel.SavePosition(Left, Top, Width, Height);
            }
            Dispatcher.InvokeAsync(UpdateAllMarquees, DispatcherPriority.Loaded);
        }

        protected override void OnClosed(EventArgs e)
        {
            _singleLineStoryboard?.Stop();
            _dualPrimaryStoryboard?.Stop();
            _dualSecondaryStoryboard?.Stop();
            _topmostGuardTimer.Stop();
            _hwndSource?.RemoveHook(WndProc);
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnClosed(e);
        }
    }
}
