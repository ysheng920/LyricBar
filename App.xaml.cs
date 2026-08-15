using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using DesktopLyrics.Services;
using DesktopLyrics.Utils;
using DesktopLyrics.ViewModels;
using DesktopLyrics.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace DesktopLyrics
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private TaskbarLyricsWindow? _lyricsWindow;
        private LyricsViewModel? _viewModel;
        private MediaSessionService? _mediaService;
        private LyricsService? _lyricsService;
        private SettingsService? _settingsService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _settingsService = new SettingsService();
            _lyricsService = new LyricsService();
            _mediaService = new MediaSessionService();
            _viewModel = new LyricsViewModel(_mediaService, _lyricsService, _settingsService);

            _lyricsWindow = new TaskbarLyricsWindow(_viewModel);
            _lyricsWindow.Show();

            SetupTrayIcon();

            await _mediaService.InitializeAsync();
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateAppIcon(),
                Text = "DesktopLyrics (Windows 11 任务栏歌词)",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();

            var lockMenuItem = new ToolStripMenuItem("🔒 锁定位置 (鼠标穿透)")
            {
                Checked = _viewModel?.IsLocked ?? true
            };
            lockMenuItem.Click += (s, e) =>
            {
                _viewModel?.ToggleLock();
                lockMenuItem.Checked = _viewModel?.IsLocked ?? true;
                lockMenuItem.Text = lockMenuItem.Checked ? "🔒 锁定位置 (鼠标穿透)" : "🔓 解锁位置 (自由拖拽)";
            };

            var dualLineMenuItem = new ToolStripMenuItem("📑 双行歌词显示")
            {
                Checked = _viewModel?.IsDualLine ?? false
            };
            dualLineMenuItem.Click += (s, e) =>
            {
                _viewModel?.ToggleDualLine();
                dualLineMenuItem.Checked = _viewModel?.IsDualLine ?? false;
            };

            var refreshMenuItem = new ToolStripMenuItem("🔄 刷新播放状态与歌词", null, async (s, e) =>
            {
                if (_mediaService != null)
                {
                    await _mediaService.RefreshMediaInfoAsync();
                }
            });

            var resetPosMenuItem = new ToolStripMenuItem("🎯 重置回任务栏默认位置", null, (s, e) =>
            {
                if (_lyricsWindow != null)
                {
                    var taskbarRect = Win32Helper.GetTaskbarRect();
                    _lyricsWindow.Left = 200;
                    _lyricsWindow.Top = taskbarRect.Top + 4;
                    _lyricsWindow.Width = 440;
                    _lyricsWindow.Height = Math.Min(42, taskbarRect.Height - 8);
                    _viewModel?.SavePosition(_lyricsWindow.Left, _lyricsWindow.Top, _lyricsWindow.Width, _lyricsWindow.Height);
                }
            });

            var aboutMenuItem = new ToolStripMenuItem("ℹ️ 关于 DesktopLyrics", null, (s, e) =>
            {
                MessageBox.Show(
                    "DesktopLyrics v1.0\n" +
                    "支持 YouTube Music / Spotify / 浏览器播放实时歌词\n\n" +
                    "提示：右键托盘图标可解锁并在任务栏上自由拖动位置！",
                    "DesktopLyrics",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });

            var exitMenuItem = new ToolStripMenuItem("🚪 退出", null, (s, e) =>
            {
                ExitApplication();
            });

            contextMenu.Items.Add(lockMenuItem);
            contextMenu.Items.Add(dualLineMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(refreshMenuItem);
            contextMenu.Items.Add(resetPosMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(aboutMenuItem);
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) =>
            {
                _viewModel?.ToggleLock();
                lockMenuItem.Checked = _viewModel?.IsLocked ?? true;
                lockMenuItem.Text = lockMenuItem.Checked ? "🔒 锁定位置 (鼠标穿透)" : "🔓 解锁位置 (自由拖拽)";
            };
        }

        private Icon CreateAppIcon()
        {
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Draw gradient circle background
                using var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, 32, 32),
                    Color.FromArgb(0, 229, 255),
                    Color.FromArgb(124, 77, 255),
                    45f);
                g.FillEllipse(brush, 1, 1, 29, 29);

                // Draw musical note symbol
                using var textBrush = new SolidBrush(Color.White);
                using var font = new Font("Segoe UI Symbol", 14, System.Drawing.FontStyle.Bold);
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("♫", font, textBrush, new RectangleF(0, 1, 32, 32), format);
            }

            var hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private void ExitApplication()
        {
            _notifyIcon?.Dispose();
            _lyricsWindow?.Close();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
