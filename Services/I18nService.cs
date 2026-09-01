using System;
using DesktopLyrics.Models;

namespace DesktopLyrics.Services
{
    public static class I18n
    {
        public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.English;

        public static string WaitingForPlayback => CurrentLanguage == AppLanguage.Chinese
            ? "等待媒体播放..."
            : "Waiting for media playback...";

        public static string SupportedMediaSources => CurrentLanguage == AppLanguage.Chinese
            ? "支持 YouTube Music / Spotify / 浏览器媒体"
            : "Supports YouTube Music, Spotify & Browser Media";

        public static string FetchingLyrics => CurrentLanguage == AppLanguage.Chinese
            ? "正在检索歌词..."
            : "Fetching lyrics...";

        public static string NoLyricsFound => CurrentLanguage == AppLanguage.Chinese
            ? "未找到同步歌词"
            : "No synced lyrics found";

        public static string NotPlaying => CurrentLanguage == AppLanguage.Chinese
            ? "未在播放"
            : "Not playing";

        public static string Idle => CurrentLanguage == AppLanguage.Chinese
            ? "空闲"
            : "Idle";

        public static string LyricsFetchFailed => CurrentLanguage == AppLanguage.Chinese
            ? "歌词获取失败"
            : "Failed to fetch lyrics";

        public static string SyncedLines(int count) => CurrentLanguage == AppLanguage.Chinese
            ? $"已同步 {count} 行歌词"
            : $"{count} lyric lines synced";

        public static string LockPosition => CurrentLanguage == AppLanguage.Chinese
            ? "🔒 锁定位置 (鼠标穿透)"
            : "🔒 Lock Position (Click-through)";

        public static string UnlockPosition => CurrentLanguage == AppLanguage.Chinese
            ? "🔓 解锁位置 (自由拖拽与调整长度)"
            : "🔓 Unlock Position (Drag & Resize)";

        public static string DualLineLyrics => CurrentLanguage == AppLanguage.Chinese
            ? "📑 双行歌词显示"
            : "📑 Dual-Line Lyrics";

        public static string ThemeMenu => CurrentLanguage == AppLanguage.Chinese
            ? "🎨 主题配色 (Theme)"
            : "🎨 Theme";

        public static string ThemeAuto => CurrentLanguage == AppLanguage.Chinese
            ? "🌓 自动跟随系统 (Auto)"
            : "🌓 Auto (Follow System)";

        public static string ThemeDark => CurrentLanguage == AppLanguage.Chinese
            ? "🌙 纯白文字 (适合深色壁纸)"
            : "🌙 Dark (Pure White Text)";

        public static string ThemeLight => CurrentLanguage == AppLanguage.Chinese
            ? "☀️ 深黑文字 (适合浅色壁纸)"
            : "☀️ Light (Jet Black Text)";

        public static string LanguageMenu => CurrentLanguage == AppLanguage.Chinese
            ? "🌐 语言 (Language)"
            : "🌐 Language";

        public static string DisplayMenu => CurrentLanguage == AppLanguage.Chinese
            ? "🖥️ 放置到显示器 (Display)"
            : "🖥️ Display Monitor";

        public static string DisplayItem(int index, bool isPrimary, int width, int height) => CurrentLanguage == AppLanguage.Chinese
            ? $"🖥️ 显示器 {index} {(isPrimary ? "(主屏)" : "")} [{width}x{height}]"
            : $"🖥️ Display {index} {(isPrimary ? "(Primary)" : "")} [{width}x{height}]";

        public static string RefreshPlayback => CurrentLanguage == AppLanguage.Chinese
            ? "🔄 刷新播放状态与歌词"
            : "🔄 Refresh Playback & Lyrics";

        public static string ResetPosition => CurrentLanguage == AppLanguage.Chinese
            ? "🎯 重置回任务栏默认位置"
            : "🎯 Reset to Default Taskbar Position";

        public static string CheckForUpdates => CurrentLanguage == AppLanguage.Chinese
            ? "🔍 检查更新..."
            : "🔍 Check for Updates...";

        public static string CheckingUpdates => CurrentLanguage == AppLanguage.Chinese
            ? "🔍 正在检查更新..."
            : "🔍 Checking for updates...";

        public static string AlreadyLatestVersion(string ver) => CurrentLanguage == AppLanguage.Chinese
            ? $"当前已是最新版本 ({ver})！\n暂无可用更新。"
            : $"You're already on the latest version ({ver})!\nNo updates available.";

        public static string NewVersionFound(string tag) => CurrentLanguage == AppLanguage.Chinese
            ? $"🆕 发现新版本 {tag}！(点击下载)"
            : $"🆕 New Version {tag} Available! (Click to Download)";

        public static string NewVersionNotificationTitle(string tag) => CurrentLanguage == AppLanguage.Chinese
            ? $"🎉 LyricBar 发现新版本 {tag}！"
            : $"🎉 LyricBar New Version {tag} Available!";

        public static string NewVersionNotificationBody => CurrentLanguage == AppLanguage.Chinese
            ? "点击前往 GitHub 下载最新版本体验全新功能与优化修复。"
            : "Click here to visit GitHub Releases and download the latest update.";

        public static string AboutMenu => CurrentLanguage == AppLanguage.Chinese
            ? "ℹ️ 关于 LyricBar"
            : "ℹ️ About LyricBar";

        public static string AboutDialog(string ver) => CurrentLanguage == AppLanguage.Chinese
            ? $"LyricBar {ver}\n" +
              "专为 Windows 11 打造的原生任务栏音乐灵动岛与歌词组件\n\n" +
              "支持 YouTube Music / Spotify / 浏览器媒体实时同步\n" +
              "支持多显示器跨屏放置与智能歌词多源聚合\n\n" +
              "提示：双击托盘图标可解锁并在任务栏上自由拖动位置和长度！"
            : $"LyricBar {ver}\n" +
              "Native Windows 11 Taskbar Music Island & Real-Time Dynamic Lyrics\n\n" +
              "Supports YouTube Music, Spotify & Browser Media\n" +
              "Multi-monitor auto-docking & AI multi-engine lyric aggregation\n\n" +
              "Tip: Double-click tray icon to unlock and freely drag/resize anywhere on the taskbar!";

        public static string ExitMenu => CurrentLanguage == AppLanguage.Chinese
            ? "🚪 退出"
            : "🚪 Exit";
    }
}
