# 🎵 LyricBar

<p align="center">
  <strong>A sleek, native-feel taskbar lyrics and dynamic music island for Windows 11.</strong>
  <br />
  <em>专为 Windows 11 打造的原生任务栏音乐灵动岛与实时同步歌词组件。</em>
</p>

<p align="center">
  <a href="https://github.com/ysheng920/LyricBar/releases/latest">
    <img src="https://img.shields.io/github/v/release/ysheng920/LyricBar?color=0078D4&label=Download%20v1.1.0&logo=windows&style=for-the-badge" alt="Download Release" />
  </a>
</p>

<p align="center">
  <a href="#-features-english">English</a> •
  <a href="#-核心特性-中文">中文说明</a> •
  <a href="https://github.com/ysheng920/LyricBar/releases/latest">Releases (Downloads)</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-purple.svg?style=flat-square" alt=".NET 9" />
  <img src="https://img.shields.io/badge/Platform-Windows%2011-0078D4.svg?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/Size-26.8MB%20(6.7MB%20zip)-success.svg?style=flat-square" alt="Size" />
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License" />
  <img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square" alt="PRs Welcome" />
</p>

---

## 🌟 Features (English)

* 🏝️ **Native Windows 11 Taskbar Integration**:
  * Seamlessly floats in the empty space of your taskbar with **100% transparent frameless blending**.
  * Features **non-activating click-through & top-most z-order lock**, never stealing focus or interrupting your work.
* 🎨 **Dynamic Theme Engine (Dark / Light / Auto)**:
  * **Auto Mode**: Automatically detects Windows taskbar theme from system registry.
  * **🌙 Dark Theme**: Crisp pure-white typography with subtle contrast drops for dark wallpapers.
  * **☀️ Light Theme**: Ultra-sharp jet-black text tailored for light wallpapers and white taskbars.
* 🖥️ **Full Multi-Monitor Management**:
  * **One-Click Monitor Switcher**: Right-click tray menu ➔ **`Display Monitor`** to instantly snap the lyrics bar to any connected monitor (Primary / Secondary / 4K Displays).
  * **🔌 Off-Screen Safe Guard**: Automatically detects unplugged monitors or resolution changes and safely docks the widget back to your primary screen.
* 🌐 **Bilingual Internationalization (Default English + Chinese)**:
  * Full native English UI by default, with instant 1-click language switching to Simplified Chinese via tray menu.
* 🔔 **Silent Auto-Update System**:
  * Silently checks for new versions on startup in the background (0 latency).
  * Notifies you with native Windows desktop notifications when a new release is available on GitHub.
* 🎵 **Universal Media Listener (WinRT SMTC)**:
  * Out-of-the-box support for **YouTube Music (Chrome / Edge / Firefox / Desktop clients)**, **Spotify**, **Apple Music**, **NetEase Music**, etc.
  * Automatically extracts **high-res album cover art**, track title, artist name, playback status, and millisecond-accurate timeline progress.
* 🧠 **AI Multi-Source Lyrics Aggregation & Pinyin NLP**:
  * Multi-engine cascade across **QQ Music + KuGou + NetEase Cloud Music + LRCLIB**.
  * **Pinyin AI NLP Matching**: Automatically converts Romanized/Pinyin YouTube titles (e.g. `Lian Ming Dai Xing` ➔ `连名带姓`) to native Chinese lyrics.
  * **⏱️ Duration Calibration**: Filters out 30s ringtones, snippets, or DJ remixes to ensure exact timeline synchronization.
  * **Cross-Language Soundtrack Bridge**: Automatically maps English track titles (e.g. `Red Scarf` ➔ `如果可以`) to original soundtrack lyrics.
* 🎛️ **Dynamic Island Interaction & Minimalist Controls**:
  * **Continuous Dancing Red Waveform**: Bounces dynamically with the rhythm; stops gracefully into a quiet resting state when paused.
  * **Smooth Hover Slide-out Controls**: Moving the mouse over the widget smoothly reveals **【⏮️ Prev】【⏯️ Play/Pause】【⏭️ Next】** playback controls.
  * **Single / Dual Line Mode**: Switch between bold single-line focus mode and dual-line mode (active line + next line preview).
* ↔️ **Free Drag & Length Adjustment (Drag-to-Resize)**:
  * Double-click the system tray icon to unlock edit mode. Drag anywhere on your taskbar, or **drag the right edge handle to customize the visible length**. Double-click again to lock and save permanently.
* 🥷 **100% Silent Background Execution**:
  * Completely silent startup with zero console black boxes.

---

## 📥 Download & Installation

### Option 1: Direct Download (Pre-built)
1. Go to **[Releases](https://github.com/ysheng920/LyricBar/releases/latest)**.
2. Download **`LyricBar-v1.1.0-win-x64.zip`** (or standalone `LyricBar.exe`).
3. Extract and double-click `LyricBar.exe` to run immediately!

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/ysheng920/LyricBar.git
cd LyricBar

# Build and run
dotnet run
```

---

## 🎮 System Tray & Controls

Find the `♫` icon in the Windows notification tray:

| Menu Option | Description |
| :--- | :--- |
| **🔒 Lock / 🔓 Unlock Position** | Toggle between click-through taskbar mode and free-drag / resize mode (or simply **double-click tray icon**) |
| **📑 Dual-line Lyrics Mode** | Toggle between single-line large text and dual-line (active + preview) lyrics |
| **🎨 Theme** | Switch between `Auto (Follow System)`, `Dark (Pure White Text)`, and `Light (Jet Black Text)` |
| **🌐 Language** | Switch between `English` and `简体中文` |
| **🖥️ Display Monitor** | 1-Click dock to any connected monitor (Primary, Secondary, etc.) |
| **🔄 Refresh Playback & Lyrics** | Clear memory cache and re-sync current playing track |
| **🎯 Reset to Default Position** | Quick snap back to default Windows 11 left taskbar slot |
| **🔍 Check for Updates...** | Check for the latest release on GitHub |
| **🚪 Exit** | Fully close the application |

---
---

## 🇨🇳 核心特性 (中文)

* 🏝️ **无感融入 Windows 11 任务栏**：
  * 精准吸附于任务栏空白区域，100% 透明无边框原生视觉效果；
  * 具备 **无焦点交互与鼠标穿透**，完全不抢焦点，不影响正常点击任务栏或切换窗口。
* 🎨 **自适应主题引擎（深色 / 浅色 / 自动跟随）**：
  * **自动模式**：读取系统注册表，自动跟随 Windows 任务栏深浅色；
  * **🌙 深色主题**：纯白清晰文字，适合深色任务栏与暗色壁纸；
  * **☀️ 浅色主题**：深黑文字高对比度，完美解决浅色壁纸看不清的问题。
* 🖥️ **多显示器管理系统**：
  * **一键跨屏瞬移**：右键托盘 ➔ **`放置到显示器`** 即可一键吸附至副屏任务栏；
  * **🔌 拔插屏幕防丢失保护**：拔掉外接屏幕时自动将歌词条拉回主屏可见区域，防止窗口卡在屏幕外。
* 🌐 **国际化双语支持（默认英文 + 简体中文）**：
  * 默认采用全英文现代界面，可随时在托盘菜单一键无缝切换为简体中文。
* 🔔 **全自动静默版本检测与通知**：
  * 开机启动后台静默比对 GitHub 版本，发现新版本时在桌面右下角弹出原生更新通知。
* 🎵 **系统级媒体会话监听 (WinRT SMTC)**：
  * 原生支持 **YouTube Music (Chrome / Edge / 各类客户端)**、**Spotify**、**Apple Music**、**网易云音乐** 等；
  * 自动捕获：高清专辑封面、歌曲名、歌手、播放/暂停状态及毫秒级播放进度。
* 🧠 **AI 拼音自然语言转写与歌曲时长智能校准**：
  * 聚合 **QQ 音乐 + 酷狗音乐 + 网易云音乐 + LRCLIB** 4 大曲库；
  * **拼音转汉字**：自动将 YouTube 英文拼音歌名（如 `Lian Ming Dai Xing` ➔ `连名带姓`）映射为中文歌词；
  * **⏱️ 时长校准**：精准根据播放时长过滤 48 秒短视频伴奏/铃声，杜绝时间戳提前错位；
  * **英文别名桥接**：自动关联电影主题曲与海外翻译别名（如 `Red Scarf` ➔ 韦礼安《如果可以》）。
* 🎛️ **灵动交互与动态声波 (Dynamic Interaction)**：
  * **常驻跳动红色声波**：节拍随音乐活跃起伏，歌曲暂停时自动静止休眠；
  * **鼠标悬停丝滑展开**：鼠标移入时展开 **【⏮️ 上一首】【⏯️ 播放/暂停】【⏭️ 下一首】** 极简控制按钮；
  * **单行 / 双行模式一键切换**：支持大气沉浸的单行大字，或双行（当前句 + 下一句预告）模式。
* ↔️ **自由拖拽与长度调节 (Drag-to-Resize)**：
  * 双击托盘图标解锁编辑模式，可随意拖动到任意位置，**拖拽右侧边缘手柄可自由拉伸长度**，锁定后自动永久保存配置。
* 🥷 **100% 无黑框静默运行**：
  * 告别繁琐黑框，双击即开，纯后台静默原生体验。

---

## 🛠️ 架构与技术栈 (Tech Stack)

* **Runtime & Framework**: C# / .NET 9.0 (`net9.0-windows10.0.22621.0`), WPF (Windows Presentation Foundation)
* **Rendering & Typography**: DirectWrite + ClearType Subpixel Font Rasterization (vector sharp text, zero blur)
* **Media Protocol**: WinRT `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager`
* **Window Management**: Win32 `Shell_TrayWnd` Parent Ownership & Non-Activating ToolWindow hooks
* **Lyrics Sync**: High-resolution timeline interpolation with binary search LRC parsing

---

## 📄 开源协议 (License)

本项目采用 [MIT License](LICENSE) 开源。欢迎 Star、Fork 与提交 Issue / PR！
