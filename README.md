# 🎵 LyricBar

<p align="center">
  <strong>专为 Windows 11 打造的原生任务栏音乐灵动岛与实时同步歌词组件</strong>
  <br />
  <em>A sleek, native-feel taskbar lyrics and dynamic music island for Windows 11.</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-purple.svg?style=flat-square" alt=".NET 9" />
  <img src="https://img.shields.io/badge/Platform-Windows%2011-0078D4.svg?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License" />
</p>

---

## ✨ 核心特性 (Features)

* 🏝️ **无感融入 Windows 11 任务栏**：
  * 精准吸附于任务栏空白区域，100% 透明无边框原生视觉；
  * 支持 **无焦点交互与鼠标穿透**，完全不干扰日常点击和拖拽操作。
* 🎵 **系统级媒体会话监听 (WinRT SMTC)**：
  * 原生支持 **YouTube Music (Chrome / Edge / 各类客户端)**、**Spotify**、**Apple Music**、**网易云音乐** 等；
  * 自动捕获：高清专辑封面、歌曲名、歌手、播放/暂停状态及毫秒级播放进度。
* 🌐 **四合一多源歌词引擎**：
  * 聚合 **酷狗音乐 (KuGou) + 网易云音乐 (NetEase) + QQ 音乐 + LRCLIB** 4 大歌词库；
  * 智能多级清洗 YouTube 标题后缀（`(Official MV)`、`[Live]`、` - Topic` 等），极速秒级对齐时间轴。
* 🎛️ **灵动交互与动态声波 (Dynamic Interaction)**：
  * **常驻跳动红色声波**：节拍随音乐活跃起伏，歌曲暂停时自动静止休眠；
  * **鼠标悬停展开**：鼠标移入时，歌词平滑向右滑开，无缝展开 **【⏮️ 上一首】【⏯️ 播放/暂停】【⏭️ 下一首】** 极简矢量控制按钮；
  * **单行 / 双行模式一键切换**：支持大气沉浸的单行大字，或双行（当前句 + 下一句预告）模式。
* 🖱️ **自由拖拽与持久化记忆**：
  * 双击托盘图标解锁自由拖拽，调整到任意位置后锁定，自动保存坐标。

---

## 🚀 快速启动与构建 (Getting Started)

### 环境要求
* Windows 10 (2004+) / Windows 11 (推荐)
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### 编译与运行
```bash
# 克隆仓库
git clone https://github.com/your-username/LyricBar.git
cd LyricBar

# 编译并运行
dotnet run
```

---

## 🎮 托盘菜单使用指南 (Usage)

在右下角系统托盘找到 `♫` 音乐图标：

| 菜单项 | 说明 |
| :--- | :--- |
| **🔒 锁定 / 🔓 解锁位置** | 切换固定位置与鼠标拖动模式（亦可**双击托盘图标**快速切换） |
| **📑 双行歌词显示** | 在单行大字与双行预告模式之间自由切换 |
| **🔄 刷新播放状态与歌词** | 强制重新同步当前媒体并重新检索歌词 |
| **🎯 重置回任务栏默认位置** | 恢复到任务栏左侧默认区域 |
| **🚪 退出** | 彻底关闭程序 |

---

## 🛠️ 技术架构 (Tech Stack)

* **语言/运行时**：C# / .NET 9.0 (`net9.0-windows10.0.22621.0`)
* **UI 框架**：WPF (Windows Presentation Foundation)
* **字体与渲染**：DirectWrite + ClearType 矢量亚像素增强
* **媒体捕获**：WinRT `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager`
* **歌词协议**：LRC Parser (毫秒级高精度时间插值算法)

---

## 📄 开源协议 (License)

本项目采用 [MIT License](LICENSE) 开源。欢迎提交 Issue 与 Pull Request！
