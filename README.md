# 🎵 LyricBar

<p align="center">
  <strong>A sleek, native-feel taskbar lyrics and dynamic music island for Windows 11.</strong>
  <br />
  <em>专为 Windows 11 打造的原生任务栏音乐灵动岛与实时同步歌词组件。</em>
</p>

<p align="center">
  <a href="https://github.com/ysheng920/LyricBar/releases/latest">
    <img src="https://img.shields.io/github/v/release/ysheng920/LyricBar?color=0078D4&label=Download%20Release&logo=windows&style=for-the-badge" alt="Download Release" />
  </a>
</p>

<p align="center">
  <a href="#-features">English</a> •
  <a href="#-中文说明">中文说明</a> •
  <a href="https://github.com/ysheng920/LyricBar/releases/latest">Releases (下载)</a>
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
  * Features **non-activating click-through & top-most z-order lock**, never stealing focus or interrupting your work.
* 🎵 **Universal Media Listener (WinRT SMTC)**:
  * Out-of-the-box support for **YouTube Music (Chrome / Edge / Firefox / Desktop clients)**, **Spotify**, **Apple Music**, **NetEase Music**, etc.
  * Automatically extracts **high-res album cover art**, track title, artist name, playback status, and millisecond-accurate timeline progress.
* 🌐 **4-in-1 Multi-Source Synced Lyrics Engine**:
  * Integrated cascade search across **KuGou + NetEase Cloud Music + QQ Music + LRCLIB**.
  * Intelligent multi-tier title noise cleaning (strips `(Official MV)`, `[Live]`, ` - Topic`, etc.) for near-100% lyric match rate worldwide.
* 🎛️ **Dynamic Island Interaction & Minimalist Controls**:
  * **Continuous Dancing Red Waveform**: Bounces dynamically with the rhythm; stops gracefully into a quiet resting state when paused.
  * **Smooth Hover Slide-out Controls**: Moving the mouse over the widget smoothly slides lyrics to the right, revealing **【⏮️ Prev】【⏯️ Play/Pause】【⏭️ Next】** sleek vector playback controls.
  * **Single / Dual Line Mode**: Switch between bold single-line focus mode and dual-line mode (active line + next line preview) at any time.
* 🌊 **Smart Smooth Auto-Scrolling**:
  * For long lyrics exceeding the taskbar slot, it automatically glides smoothly from start to finish at a natural reading speed (`22 px/s`) and parks cleanly at the end.
* ↔️ **Free Drag & Length Adjustment (Drag-to-Resize)**:
  * Double-click the system tray icon to unlock edit mode. Drag the widget anywhere on your taskbar, or **drag the right edge handle to customize the visible length**. Double-click again to lock and save permanently.
* 🥷 **100% Silent Background Execution**:
  * Completely silent startup with zero console black boxes.

---

## 📥 Download & Installation

### Option 1: Direct Download (Pre-built)
1. Go to **[Releases](https://github.com/ysheng920/LyricBar/releases/latest)**.
2. Download **`LyricBar.exe`** (or `LyricBar-v1.0.0-win-x64.zip`).
3. Double-click `LyricBar.exe` to run immediately — no installation needed!

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
  * 具备 **无焦点交互与鼠标穿透**，完全不抢焦点，不影响正常点击任务栏或切换窗口。
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
* 🌊 **超长歌词平滑慢速滑动**：
  * 遇到超长歌词时，以舒适的阅读速度平滑滑出后半句，并在末尾稳稳贴边停住。
* ↔️ **自由拖拽与长度调节 (Drag-to-Resize)**：
  * 双击托盘图标解锁编辑模式，可随意拖动到任意位置，**拖拽右侧边缘手柄可自由拉伸长度**，锁定后自动永久保存配置。
* 🥷 **100% 无黑框静默运行**：
  * 告别繁琐黑框，双击即开，纯后台静默原生体验。

---

## 📥 下载与使用

### 方式 1：直接下载免安装版（推荐）
1. 前往 **[Releases 页面](https://github.com/ysheng920/LyricBar/releases/latest)**；
2. 下载 **`LyricBar.exe`** 或 `LyricBar-v1.0.0-win-x64.zip`；
3. 双击即可运行！

### 方式 2：源码编译
```bash
git clone https://github.com/ysheng920/LyricBar.git
cd LyricBar
dotnet run
```

---

## 📄 开源协议 (License)

本项目采用 [MIT License](LICENSE) 开源。欢迎提交 Issue 与 Pull Request！
