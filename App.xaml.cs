using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using DesktopLyrics.Models;
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
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;

        private NotifyIcon? _notifyIcon;
        private TaskbarLyricsWindow? _lyricsWindow;
        private LyricsViewModel? _viewModel;
        private MediaSessionService? _mediaService;
        private LyricsService? _lyricsService;
        private SettingsService? _settingsService;
        private UpdateCheckService? _updateService;

        private ToolStripMenuItem? _updateMenuItem;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Automatically hide any console window immediately
            var consoleHwnd = GetConsoleWindow();
            if (consoleHwnd != IntPtr.Zero)
            {
                ShowWindow(consoleHwnd, SW_HIDE);
            }

            _settingsService = new SettingsService();
            _lyricsService = new LyricsService();
            _mediaService = new MediaSessionService();
            _updateService = new UpdateCheckService();
            _viewModel = new LyricsViewModel(_mediaService, _lyricsService, _settingsService);

            _lyricsWindow = new TaskbarLyricsWindow(_viewModel);
            _lyricsWindow.Show();

            SetupTrayIcon();

            _updateService.UpdateAvailable += OnUpdateAvailable;
            _updateService.StartStartupCheck();

            await _mediaService.InitializeAsync();
        }

        private void OnUpdateAvailable(string latestTag, string releaseUrl, string notes)
        {
            Dispatcher.Invoke(() =>
            {
                if (_updateMenuItem != null)
                {
                    _updateMenuItem.Text = $"🆕 发现新版本 {latestTag}！(点击下载)";
                    _updateMenuItem.Visible = true;
                }

                _notifyIcon?.ShowBalloonTip(
                    6000,
                    $"🎉 LyricBar 发现新版本 {latestTag}！",
                    "点击前往 GitHub 下载最新版本体验全新功能与优化修复。",
                    ToolTipIcon.Info);
            });
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateAppIcon(),
                Text = "LyricBar (Windows 11 任务栏歌词)",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();

            // Dynamic Update Banner (Only visible when update found)
            _updateMenuItem = new ToolStripMenuItem("🆕 发现新版本！点击下载", null, (s, e) =>
            {
                UpdateCheckService.OpenReleasePage(_updateService?.LatestReleaseUrl);
            })
            {
                Visible = false,
                Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 212)
            };

            var lockMenuItem = new ToolStripMenuItem("🔒 锁定位置 (鼠标穿透)")
            {
                Checked = _viewModel?.IsLocked ?? true
            };
            lockMenuItem.Click += (s, e) =>
            {
                _viewModel?.ToggleLock();
                lockMenuItem.Checked = _viewModel?.IsLocked ?? true;
                lockMenuItem.Text = lockMenuItem.Checked ? "🔒 锁定位置 (鼠标穿透)" : "🔓 解锁位置 (自由拖拽与调整长度)";
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

            // Theme Selection Submenu
            var themeMenu = new ToolStripMenuItem("🎨 主题配色 (Theme)");
            var autoThemeItem = new ToolStripMenuItem("🌓 自动跟随系统 (Auto)")
            {
                Checked = (_viewModel?.Theme == AppTheme.Auto)
            };
            var darkThemeItem = new ToolStripMenuItem("🌙 纯白文字 (适合深色壁纸)")
            {
                Checked = (_viewModel?.Theme == AppTheme.Dark)
            };
            var lightThemeItem = new ToolStripMenuItem("☀️ 深黑文字 (适合浅色壁纸)")
            {
                Checked = (_viewModel?.Theme == AppTheme.Light)
            };

            autoThemeItem.Click += (s, e) =>
            {
                _viewModel?.SetTheme(AppTheme.Auto);
                autoThemeItem.Checked = true;
                darkThemeItem.Checked = false;
                lightThemeItem.Checked = false;
            };
            darkThemeItem.Click += (s, e) =>
            {
                _viewModel?.SetTheme(AppTheme.Dark);
                autoThemeItem.Checked = false;
                darkThemeItem.Checked = true;
                lightThemeItem.Checked = false;
            };
            lightThemeItem.Click += (s, e) =>
            {
                _viewModel?.SetTheme(AppTheme.Light);
                autoThemeItem.Checked = false;
                darkThemeItem.Checked = false;
                lightThemeItem.Checked = true;
            };

            themeMenu.DropDownItems.Add(autoThemeItem);
            themeMenu.DropDownItems.Add(darkThemeItem);
            themeMenu.DropDownItems.Add(lightThemeItem);

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
                    _lyricsWindow.Width = 660;
                    _lyricsWindow.Height = Math.Min(44, taskbarRect.Height - 4);
                    _viewModel?.SavePosition(_lyricsWindow.Left, _lyricsWindow.Top, _lyricsWindow.Width, _lyricsWindow.Height);
                }
            });

            var checkUpdateMenuItem = new ToolStripMenuItem("🔍 检查更新...", null, async (s, e) =>
            {
                if (_updateService != null)
                {
                    var hasUpdate = await _updateService.CheckForUpdatesAsync(isManual: true);
                    if (!hasUpdate)
                    {
                        MessageBox.Show(
                            $"当前已是最新版本 ({UpdateCheckService.CurrentVersion})！\n暂无可用更新。",
                            "LyricBar 检查更新",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            });

            var aboutMenuItem = new ToolStripMenuItem("ℹ️ 关于 LyricBar", null, (s, e) =>
            {
                MessageBox.Show(
                    $"LyricBar {UpdateCheckService.CurrentVersion}\n" +
                    "专为 Windows 11 打造的原生任务栏音乐灵动岛与歌词组件\n\n" +
                    "支持 YouTube Music / Spotify / 浏览器媒体实时同步\n" +
                    "提示：双击托盘图标可解锁并在任务栏上自由拖动位置和长度！",
                    "LyricBar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });

            var exitMenuItem = new ToolStripMenuItem("🚪 退出", null, (s, e) =>
            {
                ExitApplication();
            });

            contextMenu.Items.Add(_updateMenuItem);
            contextMenu.Items.Add(lockMenuItem);
            contextMenu.Items.Add(dualLineMenuItem);
            contextMenu.Items.Add(themeMenu);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(refreshMenuItem);
            contextMenu.Items.Add(resetPosMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(checkUpdateMenuItem);
            contextMenu.Items.Add(aboutMenuItem);
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.BalloonTipClicked += (s, e) =>
            {
                UpdateCheckService.OpenReleasePage(_updateService?.LatestReleaseUrl);
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                _viewModel?.ToggleLock();
                lockMenuItem.Checked = _viewModel?.IsLocked ?? true;
                lockMenuItem.Text = lockMenuItem.Checked ? "🔒 锁定位置 (鼠标穿透)" : "🔓 解锁位置 (自由拖拽与调整长度)";
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
