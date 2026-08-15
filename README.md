# 🎵 LyricBar

<p align="center">
  <strong>A sleek, native-feel taskbar lyrics and dynamic music island for Windows 11.</strong>
  <br />
  <em>专为 Windows 11 打造的原生任务栏音乐灵动岛与实时同步歌词组件。</em>
</p>

<p align="center">
  <a href="#-features">English</a> •
  <a href="#-中文说明">中文说明</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-purple.svg?style=flat-square" alt=".NET 9" />
  <img src="https://img.shields.io/badge/Platform-Windows%2011-0078D4.svg?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License" />
  <img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square" alt="PRs Welcome" />
</p>

---

## 🌟 Features (English)

* 🏝️ **Native Windows 11 Taskbar Integration**:
  * Seamlessly floats in the empty space of your taskbar with **100% transparent frameless blending**.
  * Features **non-activating click-through & top-most z-order lock**, never interrupting your clicks or window switching.
* 🎵 **Universal Media Listener (WinRT SMTC)**:
  * Out-of-the-box support for **YouTube Music (Chrome / Edge / Firefox / Desktop clients)**, **Spotify**, **Apple Music**, **NetEase Music**, etc.
  * Automatically extracts **high-res album cover art**, track title, artist name, playback status, and millisecond-accurate timeline progress.
* 🌐 **4-in-1 Multi-Source Synced Lyrics Engine**:
  * Integrated cascade search across **KuGou + NetEase Cloud Music + QQ Music + LRCLIB**.
  * Intelligent multi-tier title noise cleaning (strips `(Official MV)`, `[Live]`, ` - Topic`, etc.) for near-100% lyric match rate worldwide.
* 🎛️ **Dynamic Island Interaction & Hover Animations**:
  * **Continuous Dancing Red Waveform**: Bounces dynamically with the rhythm; stops gracefully into a quiet resting state when paused.
  * **Smooth Hover Slide-out Controls**: Moving the mouse over the widget smoothly slides lyrics to the right, revealing **【⏮️ Prev】【⏯️ Play/Pause】【⏭️ Next】** minimalist vector playback controls.
  * **Single / Dual Line Mode**: Switch between bold single-line focus mode and dual-line mode (active line + next line preview) at any time.
* 🖱️ **Free Drag & Auto-Save Position**:
  * Double-click the system tray icon to unlock drag mode, reposition anywhere on your taskbar/screen, and lock to permanently remember coordinates.

---

## 🚀 Quick Start & Build

### Prerequisites
* Windows 10 (2004+) or Windows 11 (Recommended)
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Clone & Run
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
| **🔒 Lock / 🔓 Unlock Position** | Toggle between click-through taskbar mode and free-drag repositioning (or simply **double-click tray icon**) |
| **📑 Dual-line Lyrics Mode** | Toggle between single-line large text and dual-line (active + preview) lyrics |
| **🔄 Refresh Media & Lyrics** | Force re-sync current playing track and query lyrics engine |
| **🎯 Reset to Default Position** | Quick snap back to default Windows 11 left taskbar slot |
| **🚪 Exit** | Fully close the application |

---

## 🛠️ Architecture & Tech Stack

* **Runtime & Framework**: C# / .NET 9.0 (`net9.0-windows10.0.22621.0`), WPF (Windows Presentation Foundation)
* **Rendering & Typography**: DirectWrite + ClearType Subpixel Font Rasterization (vector sharp text, zero blur)
* **Media Protocol**: WinRT `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager`
* **Window Management**: Win32 `Shell_TrayWnd` Parent Ownership & Non-Activating ToolWindow hooks
* **Lyrics Sync**: High-resolution timeline interpolation with binary search LRC parsing

---
---

## 🇨🇳 中文说明

## ✨ 核心特性

* 🏝️ **无感融入 Windows 11 任务栏**：
  * 精准吸附于任务栏空白区域，100% 透明无边框原生视觉效果；
  * 具备 **无焦点交互与鼠标穿透**，完全不影响正常点击任务栏或切换窗口。
* 🎵 **系统级媒体会话监听 (WinRT SMTC)**：
  * 原生支持 **YouTube Music (Chrome / Edge / 各类客户端)**、**Spotify**、**Apple Music**、**网易云音乐** 等；
  * 自动捕获：高清专辑封面、歌曲名、歌手、播放/暂停状态及毫秒级播放进度。
* 🌐 **四合一多源歌词引擎**：
  * 聚合 **酷狗音乐 (KuGou) + 网易云音乐 (NetEase) + QQ 音乐 + LRCLIB** 4 大歌词库；
  * 智能多级清洗 YouTube 标题后缀（`(Official MV)`、`[Live]`、` - Topic` 等），秒级对齐时间轴。
* 🎛️ **灵动交互与动态声波 (Dynamic Interaction)**：
  * **常驻跳动红色声波**：节拍随音乐活跃起伏，歌曲暂停时自动静止休眠；
  * **鼠标悬停丝滑展开**：鼠标移入时，歌词平滑向右滑开，无缝展开 **【⏮️ 上一首】【⏯️ 播放/暂停】【⏭️ 下一首】** 极简矢量控制按钮；
  * **单行 / 双行模式一键切换**：支持大气沉浸的单行大字，或双行（当前句 + 下一句预告）模式。
* 🖱️ **自由拖拽与持久化记忆**：
  * 双击托盘图标解锁自由拖拽，调整到任意满意位置后锁定，自动保存坐标。

---

## 🚀 快速启动与构建

### 环境要求
* Windows 10 (2004+) / Windows 11 (推荐)
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### 编译与运行
```bash
# 克隆仓库
git clone https://github.com/ysheng920/LyricBar.git
cd LyricBar

# 编译并运行
dotnet run
```

---

## 📄 开源协议 (License)

本项目采用 [MIT License](LICENSE) 开源。欢迎提交 Issue 与 Pull Request！
