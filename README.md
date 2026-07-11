# SC Launch - Survivalcraft 启动器

[![语言](https://img.shields.io/badge/语言-中文-blue)](README.md)
[![语言](https://img.shields.io/badge/Language-English-blue)](README.en.md)
[![语言](https://img.shields.io/badge/Язык-Русский-blue)](README.ru.md)

---

一个基于 C#/WPF 的 Survivalcraft 游戏启动器，支持多版本管理、多线程下载、自动解压、多语言等功能。

## 关于
在PC（只限Windows）游玩Survivalcraft安装有多个版本，凌乱不堪 。桌面有很多图标，不知版本，看的眼睛“眼花缭乱”。对此，制作了SC Launch，方便玩多版本的烦恼。同时，加入了"多线程（Beta)"下载游戏功能，让你无需花费精力寻找可下载游戏的网站！（本人制作这个目的其实就是练习，闲暇制作）确实有点傻😂

## 功能特性

### 核心功能
- **多版本管理** - 支持添加、编辑、删除多个 Survivalcraft 游戏版本（Net/联机版、API/模组版）
- **多线程下载**支持断点续传、暂停/恢复、自动回退单线程
- **内置解压** - 支持 ZIP/7z 格式自动解压，集成 7z.dll 组件
- **版本检测** - 7 级 fallback 机制自动获取最新版本信息

### 界面与体验
- **暗黑主题**
- **右键菜单** - 游戏列表项支持编辑、删除、打开文件夹
- **下载任务面板**显示进度、速度、百分比、暂停/停止/打开文件夹
- **语言选择** - 首次启动内联覆盖层选择，标题栏 🌐 按钮切换，支持中/英/俄三语
- **欢迎提示** - 未选择游戏时显示添加游戏引导

### 额外功能
- **友情链接** - 官网、社区、模组站、版本合集、外部资源站链接
- **关于对话框** - 版本信息、作者、MIT License、QQ 群号（仅中文显示，点击复制、按钮跳转加群）
- **彩蛋** - 关于界面【笑】
- **自定义图标** - 木头方块图标（EXE 图标 + 窗口图标）

## 支持的语言
- 🇨🇳 简体中文 (zh)
- 🇺🇸 English (en)
- 🇷🇺 Русский (ru)

## 下载源
| 类型 | GitHub | Gitee |
|------|--------|-------|
| Survivalcraft Net (联机版) | survivalcraft-net/survivalcraft-net | SC-SPM/SurvivalcraftNet |
| Survivalcraft API (模组版) | X | SC-SPM/SurvivalcraftAPI |

## 配置文件
- `apps.json` - 游戏列表 + 下载保存路径
- `lang.txt` - 当前语言偏好 (zh/en/ru)
- `skip_lang.txt` - 是否跳过首次语言选择

## 依赖
- .NET 8.0 (Windows Desktop / WPF)
- [SevenZipExtractor](https://www.nuget.org/packages/SevenZipExtractor/) v1.0.19

## 构建
```bash
dotnet build -c Release
```
输出路径：`bin\Release\net8.0-windows\SC Launch.exe`

### 生成安装包
```powershell
# 生成 ZIP 便携包
.\build.bat

# 或使用 PowerShell（需要安装 Inno Setup 6）
.\build.ps1
```

### 发布版本
- **SC_Launch_windows.zip** - 便携版，解压即用
- **SC_Launch_windows_install.exe** - 安装版，需要 [Inno Setup 6](https://jrsoftware.org/isinfo.php)

**注意**：重新构建前请关闭正在运行的程序，否则会因文件被占用而失败。

## 许可证
MIT License

## 作者
WE

## QQ 群
1群：188309534

## 仓库
- GitHub: https://github.com/WE666IPS/WE-SC_Launch
- Gitee: https://gitee.com/we666ips/we-sc_-launch

## README由AI生成😂😂😂
