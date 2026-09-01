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
using Microsoft.Win32;
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

            EnsureWindowVisibleOnScreen();

            // Multi-monitor disconnect / resolution change protection
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            SetupTrayIcon();

            _updateService.UpdateAvailable += OnUpdateAvailable;
            _updateService.StartStartupCheck();

            await _mediaService.InitializeAsync();
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(EnsureWindowVisibleOnScreen);
        }

        private void EnsureWindowVisibleOnScreen()
        {
            if (_lyricsWindow == null) return;

            var windowRect = new Rectangle(
                (int)_lyricsWindow.Left,
                (int)_lyricsWindow.Top,
                (int)Math.Max(50, _lyricsWindow.Width),
                (int)Math.Max(20, _lyricsWindow.Height));

            bool isVisibleOnAnyScreen = false;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.IntersectsWith(windowRect))
                {
                    isVisibleOnAnyScreen = true;
                    break;
                }
            }

            // If disconnected secondary monitor caused window to be off-screen, snap to primary screen
            if (!isVisibleOnAnyScreen)
            {
                var primaryScreen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);
                if (primaryScreen != null)
                {
                    _lyricsWindow.Left = primaryScreen.Bounds.Left + 200;
                    _lyricsWindow.Top = primaryScreen.Bounds.Bottom - 44;
                    _lyricsWindow.Width = 660;
                    _lyricsWindow.Height = 40;
                    _viewModel?.SavePosition(_lyricsWindow.Left, _lyricsWindow.Top, _lyricsWindow.Width, _lyricsWindow.Height);
                }
            }
        }

        private void OnUpdateAvailable(string latestTag, string releaseUrl, string notes)
        {
            Dispatcher.Invoke(() =>
            {
                if (_updateMenuItem != null)
                {
                    _updateMenuItem.Text = I18n.NewVersionFound(latestTag);
                    _updateMenuItem.Visible = true;
                }

                _notifyIcon?.ShowBalloonTip(
                    6000,
                    I18n.NewVersionNotificationTitle(latestTag),
                    I18n.NewVersionNotificationBody,
                    ToolTipIcon.Info);
            });
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateAppIcon(),
                Text = "LyricBar - Windows 11 Taskbar Lyrics",
                Visible = true
            };

            RebuildContextMenu();

            _notifyIcon.BalloonTipClicked += (s, e) =>
            {
                UpdateCheckService.OpenReleasePage(_updateService?.LatestReleaseUrl);
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                _viewModel?.ToggleLock();
                RebuildContextMenu();
            };
        }

        private void RebuildContextMenu()
        {
            if (_notifyIcon == null) return;

            var contextMenu = new ContextMenuStrip();

            // Dynamic Update Banner (Only visible when update found)
            _updateMenuItem = new ToolStripMenuItem(
                _updateService?.HasUpdate == true ? I18n.NewVersionFound(_updateService.LatestVersion ?? "") : "🆕 Update Available",
                null,
                (s, e) => UpdateCheckService.OpenReleasePage(_updateService?.LatestReleaseUrl))
            {
                Visible = _updateService?.HasUpdate == true,
                Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 212)
            };

            var isLocked = _viewModel?.IsLocked ?? true;
            var lockMenuItem = new ToolStripMenuItem(isLocked ? I18n.LockPosition : I18n.UnlockPosition)
            {
                Checked = isLocked
            };
            lockMenuItem.Click += (s, e) =>
            {
                _viewModel?.ToggleLock();
                var lockedNow = _viewModel?.IsLocked ?? true;
                lockMenuItem.Checked = lockedNow;
                lockMenuItem.Text = lockedNow ? I18n.LockPosition : I18n.UnlockPosition;
            };

            var dualLineMenuItem = new ToolStripMenuItem(I18n.DualLineLyrics)
            {
                Checked = _viewModel?.IsDualLine ?? false
            };
            dualLineMenuItem.Click += (s, e) =>
            {
                _viewModel?.ToggleDualLine();
                dualLineMenuItem.Checked = _viewModel?.IsDualLine ?? false;
            };

            // Theme Selection Submenu
            var themeMenu = new ToolStripMenuItem(I18n.ThemeMenu);
            var autoThemeItem = new ToolStripMenuItem(I18n.ThemeAuto)
            {
                Checked = (_viewModel?.Theme == AppTheme.Auto)
            };
            var darkThemeItem = new ToolStripMenuItem(I18n.ThemeDark)
            {
                Checked = (_viewModel?.Theme == AppTheme.Dark)
            };
            var lightThemeItem = new ToolStripMenuItem(I18n.ThemeLight)
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

            // Language Selection Submenu (Bilingual: English & Simplified Chinese)
            var langMenu = new ToolStripMenuItem(I18n.LanguageMenu);
            var engLangItem = new ToolStripMenuItem("English (Default)")
            {
                Checked = (_viewModel?.Language == AppLanguage.English)
            };
            var chnLangItem = new ToolStripMenuItem("简体中文 (Simplified Chinese)")
            {
                Checked = (_viewModel?.Language == AppLanguage.Chinese)
            };

            engLangItem.Click += (s, e) =>
            {
                _viewModel?.SetLanguage(AppLanguage.English);
                RebuildContextMenu();
            };
            chnLangItem.Click += (s, e) =>
            {
                _viewModel?.SetLanguage(AppLanguage.Chinese);
                RebuildContextMenu();
            };

            langMenu.DropDownItems.Add(engLangItem);
            langMenu.DropDownItems.Add(chnLangItem);

            // Multi-Monitor Selection Submenu
            var monitorMenu = new ToolStripMenuItem(I18n.DisplayMenu);
            contextMenu.Opening += (s, e) => PopulateMonitorMenu(monitorMenu);

            var refreshMenuItem = new ToolStripMenuItem(I18n.RefreshPlayback, null, async (s, e) =>
            {
                _lyricsService?.ClearCache();
                if (_mediaService != null)
                {
                    await _mediaService.RefreshMediaInfoAsync();
                }
            });

            var resetPosMenuItem = new ToolStripMenuItem(I18n.ResetPosition, null, (s, e) =>
            {
                if (_lyricsWindow != null)
                {
                    var primaryScreen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);
                    if (primaryScreen != null)
                    {
                        _lyricsWindow.Left = primaryScreen.Bounds.Left + 200;
                        _lyricsWindow.Top = primaryScreen.Bounds.Bottom - 44;
                        _lyricsWindow.Width = 660;
                        _lyricsWindow.Height = 40;
                        _viewModel?.SavePosition(_lyricsWindow.Left, _lyricsWindow.Top, _lyricsWindow.Width, _lyricsWindow.Height);
                    }
                }
            });

            ToolStripMenuItem checkUpdateMenuItem = null!;
            checkUpdateMenuItem = new ToolStripMenuItem(I18n.CheckForUpdates, null, async (s, e) =>
            {
                if (_updateService != null)
                {
                    checkUpdateMenuItem.Text = I18n.CheckingUpdates;
                    var hasUpdate = await _updateService.CheckForUpdatesAsync(isManual: true);
                    checkUpdateMenuItem.Text = I18n.CheckForUpdates;
                    if (!hasUpdate)
                    {
                        MessageBox.Show(
                            I18n.AlreadyLatestVersion(UpdateCheckService.CurrentVersion),
                            "LyricBar",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            });

            var aboutMenuItem = new ToolStripMenuItem(I18n.AboutMenu, null, (s, e) =>
            {
                MessageBox.Show(
                    I18n.AboutDialog(UpdateCheckService.CurrentVersion),
                    "LyricBar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });

            var exitMenuItem = new ToolStripMenuItem(I18n.ExitMenu, null, (s, e) =>
            {
                ExitApplication();
            });

            contextMenu.Items.Add(_updateMenuItem);
            contextMenu.Items.Add(lockMenuItem);
            contextMenu.Items.Add(dualLineMenuItem);
            contextMenu.Items.Add(themeMenu);
            contextMenu.Items.Add(langMenu);
            contextMenu.Items.Add(monitorMenu);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(refreshMenuItem);
            contextMenu.Items.Add(resetPosMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(checkUpdateMenuItem);
            contextMenu.Items.Add(aboutMenuItem);
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void PopulateMonitorMenu(ToolStripMenuItem monitorMenu)
        {
            monitorMenu.DropDownItems.Clear();
            var allScreens = Screen.AllScreens;

            for (int i = 0; i < allScreens.Length; i++)
            {
                var screen = allScreens[i];
                var screenIndex = i + 1;
                var label = I18n.DisplayItem(screenIndex, screen.Primary, screen.Bounds.Width, screen.Bounds.Height);

                var item = new ToolStripMenuItem(label);

                // Check if current window is on this screen
                if (_lyricsWindow != null)
                {
                    var windowCenter = new System.Drawing.Point(
                        (int)(_lyricsWindow.Left + _lyricsWindow.Width / 2),
                        (int)(_lyricsWindow.Top + _lyricsWindow.Height / 2));
                    item.Checked = screen.Bounds.Contains(windowCenter);
                }

                var targetScreen = screen;
                item.Click += (s, e) =>
                {
                    if (_lyricsWindow != null)
                    {
                        _lyricsWindow.Left = targetScreen.Bounds.Left + 200;
                        _lyricsWindow.Top = targetScreen.Bounds.Bottom - 44;
                        _viewModel?.SavePosition(_lyricsWindow.Left, _lyricsWindow.Top, _lyricsWindow.Width, _lyricsWindow.Height);
                    }
                };

                monitorMenu.DropDownItems.Add(item);
            }
        }

        private Icon CreateAppIcon()
        {
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, 32, 32),
                    Color.FromArgb(0, 229, 255),
                    Color.FromArgb(124, 77, 255),
                    45f);
                g.FillEllipse(brush, 1, 1, 29, 29);

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
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _notifyIcon?.Dispose();
            _lyricsWindow?.Close();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
