# SMUDebugTool zh-CN

[![Latest release](https://img.shields.io/github/v/release/Terry577/SMUDebugTool-zh-CN?label=release)](https://github.com/Terry577/SMUDebugTool-zh-CN/releases/latest)
[![Build](https://github.com/Terry577/SMUDebugTool-zh-CN/actions/workflows/build.yml/badge.svg)](https://github.com/Terry577/SMUDebugTool-zh-CN/actions/workflows/build.yml)
[![License: GPL-3.0](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE.md)

这是 [irusanov/SMUDebugTool](https://github.com/irusanov/SMUDebugTool) 的简体中文开源修改版，用于读取和写入 AMD Ryzen 平台的频率、PState、SMU、PCI、CPUID、MSR、PBO 和功耗管理表等底层参数。

当前稳定版本为 **v1.41.3**，基于原作者 **v1.41** 开发。

> [下载最新版本](https://github.com/Terry577/SMUDebugTool-zh-CN/releases/latest)

完整版本变更见 [CHANGELOG.md](CHANGELOG.md)。

## v1.41.3 主要改动

- FMax 写入失败时保留并显示 SMU 的真实返回状态。
- 可区分前置条件不满足、SMU 忙碌、邮箱超时、PCI 读写失败和命令不受支持。
- 启动时应用已保存 FMax 失败时同样显示具体状态码。

## v1.41.2 主要改动

- 目标框架由 .NET Framework 4.5 升级到最新的 .NET Framework 4.8.1。
- 改用 WinForms 4.8.1 官方 Per-Monitor V2 动态 DPI 支持。
- 删除手工 `PerformAutoScale()`、DPI 倍率和分隔栏缩放补丁。
- 删除约 680 行已停用的旧版中文布局代码，继续收敛到单一现代 UI 构建入口。
- 发布 ZIP 内附微软官方 .NET Framework 4.8.1 离线安装程序及运行入口。

## v1.41.1 主要改动

- 全面汉化界面、运行状态、提示框、工具提示和数据表列名。
- 使用 AntdUI 重新设计固定尺寸界面，包括单行导航、功能卡片、运行日志和开源信息区。
- 启用 Per-Monitor V2 DPI 感知，改善不同分辨率和缩放比例显示器之间移动窗口时的字体清晰度。
- PBO Curve Optimizer 根据实际 CCD 数量自适应布局：
  - 单 CCD 纵向显示 8 个核心，仅显示一组批量 `+` / `−` 按钮；
  - 双 CCD 时才显示第二组核心控件；
  - 每个 CCD 的核心输入框采用紧凑固定宽度。
- 新增“启动时同时应用 FMax”，可在登录 Windows 时同时恢复已保存的 Curve Optimizer 与 FMax。
- 补充 AntdUI、PawnIO、InpOut、WinIo 等组件的许可证与署名信息。

CPU、SMU、PCI、MSR、PBO、CPUID、CCD、CCX、PROCHOT、Curve Shaper 等通用技术名称予以保留。

## 下载与运行

1. 从 [Releases](https://github.com/Terry577/SMUDebugTool-zh-CN/releases) 下载最新 ZIP。
2. 将 ZIP 完整解压到普通英文路径，不要直接从压缩包预览窗口运行。
3. 双击 `RUN_SMUDEBUGTOOL.cmd`。如果系统缺少 .NET Framework 4.8.1，它会调用发布包内附的微软官方离线安装程序。
4. 右键运行发布包内的 `PawnIO_setup.exe`，安装 PawnIO 官方驱动并重新启动 Windows。
5. 以管理员身份运行 `SMUDebugTool.exe`。

程序需要受支持的 Windows、.NET Framework 4.8.1 和可用的底层 I/O 驱动。.NET Framework 不能作为便携式 self-contained 运行时使用，因此发布包内附的是需要安装一次的微软官方 Redistributable。发布包同时包含原作者 v1.41 提供的 InpOut/WinIo 兼容运行文件及对应许可证，以及未经修改、经过 SHA-256 校验的 PawnIO 2.2.0 官方安装程序、GPL-2.0 许可证和对应源码。

运行时安装说明见 [`ReleaseAssets/INSTALL_DOTNET.txt`](ReleaseAssets/INSTALL_DOTNET.txt)。

## PBO 配置与开机应用

配置文件固定保存为：

```text
profiles/co_profile.txt
```

使用流程：

1. 在 PBO 页面设置各核心 Curve Optimizer 偏移量和 FMax。
2. 点击“保存”，将当前可用核心偏移量与 FMax 写入配置文件。
3. 点击“加载”，只把配置读取到界面；确认数值后再点击“应用”写入硬件。
4. 勾选“启动时应用已保存的配置文件”，程序会创建名为 `RyzenSDT` 的 Windows 登录计划任务，并以最高权限应用已保存的 Curve Optimizer。
5. 如需同时恢复 FMax，再勾选红色的“启动时同时应用 FMax”。勾选它会自动启用上一项，并读取配置文件中的 `fmax=` 值。
6. 取消“启动时应用已保存的配置文件”会同时关闭 FMax 启动应用并删除该计划任务。

计划任务只使用已经保存到 `co_profile.txt` 的值，不会使用尚未保存的界面输入。更换 CPU、更新 BIOS 或调整核心布局后，请重新检查并保存配置，不要盲目复用旧文件。

配置文件格式和注意事项见 [`ReleaseAssets/profiles/README.txt`](ReleaseAssets/profiles/README.txt)。

## 常见问题

### 提示无法初始化 I/O 模块

依次检查：

1. 是否已完整解压发布包；
2. 是否以管理员身份运行；
3. PawnIO 是否已正确安装；
4. 安装驱动后是否已重新启动 Windows；
5. 安全软件是否拦截了底层驱动或相关 DLL。

详细说明见 [`ReleaseAssets/INSTALL_PAWNIO.txt`](ReleaseAssets/INSTALL_PAWNIO.txt)。

### 在不同缩放比例的显示器间移动时字体异常

v1.41.2 已改用 .NET Framework 4.8.1 官方 Per-Monitor V2 动态 DPI 支持，并移除了旧版手工缩放。请确保 Windows 显示设置中的缩放比例已正确应用；如果系统刚修改过缩放设置，退出并重新启动程序。

### 启动应用没有生效

确认 `profiles/co_profile.txt` 已存在，并在 Windows 任务计划程序中检查 `RyzenSDT`。移动或删除程序目录后，计划任务仍会指向旧路径；请在程序中取消并重新勾选启动应用，以重建任务。

## 风险提示

本工具能够直接修改处理器和系统底层参数。错误的频率、电压、Curve Optimizer、FMax、PState、SMU、PCI 或 MSR 设置可能导致系统崩溃、数据丢失，甚至硬件损坏。请在理解相关参数并自行承担风险的前提下使用。

## 从源码构建

### Visual Studio

1. 在 Windows 上安装 Visual Studio 2022，并启用“.NET 桌面开发”工作负载。
2. 安装 .NET Framework 4.8.1 Developer Pack。
3. 打开 `ZenStatesDebugTool.sln`。
4. 还原 NuGet 软件包。
5. 选择 `Release | Any CPU` 并生成解决方案。

### 本地 PowerShell 脚本

先还原 NuGet 软件包，然后运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\build-local.ps1 `
  -Configuration Release
```

本地脚本需要已安装的 .NET SDK，并使用 NuGet 恢复的 .NET Framework 4.8.1 引用程序集。输出位于 `bin\Release`。

### 生成发布包

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\package-release.ps1 `
  -Version 1.41.3 `
  -Configuration Release `
  -OutputDirectory .
```

打包脚本会下载原作者 v1.41 发布包、微软官方 .NET Framework 4.8.1 离线安装程序、PawnIO 2.2.0 官方安装程序及其对应源码，分别校验 SHA-256，再组合程序、依赖库、调试符号、许可证、安装说明和 `profiles` 目录。内附运行时、驱动安装程序和对应源码后，ZIP 体积约为 84 MB。

GitHub Actions 工作流也会在推送到 `master` 后执行 .NET Framework Release 构建并上传 ZIP 构建产物。

## 项目结构

| 路径 | 内容 |
| --- | --- |
| `SettingsForm.cs` | 主功能、PBO 配置、计划任务与硬件操作逻辑 |
| `ModernInterface.cs` | 现代界面结构与功能卡片 |
| `ModernUi.cs` | AntdUI 外观、导航和控件升级 |
| `UiRuntimeStyle.cs` | 统一字体、窗口策略和非手工 DPI 样式 |
| `UiLocalization.cs` | 中文文本本地化 |
| `Properties/AssemblyInfo.cs` | 产品信息与版本号 |
| `CHANGELOG.md` | 汉化版本更新记录 |
| `scripts/build-local.ps1` | 本地 .NET Framework 编译脚本 |
| `scripts/package-release.ps1` | Windows 发布包生成脚本 |
| `ReleaseAssets` | 发布包内附带的说明与默认目录 |
| `ThirdParty` | 第三方许可证 |

## 同步原项目

仓库已使用 `upstream` 指向原项目。手动配置时可运行：

```bash
git remote add upstream https://github.com/irusanov/SMUDebugTool.git
git fetch upstream
git merge upstream/master
```

同步后建议重点检查新增的界面文本、错误信息、控件布局、PBO 核心映射和打包依赖，并重新执行 Release 构建和界面测试。

## 反馈与贡献

- 错误报告：[提交 Bug](https://github.com/Terry577/SMUDebugTool-zh-CN/issues/new?template=bug_report.md)
- 功能建议：[提交功能请求](https://github.com/Terry577/SMUDebugTool-zh-CN/issues/new?template=feature_request.md)
- 代码贡献：欢迎提交 Issue 或 Pull Request。

报告问题时请提供 Windows 版本、CPU 型号、主板与 BIOS、程序版本、显示缩放比例、复现步骤、运行日志和必要截图。请先移除序列号、用户名、路径等隐私信息。

## 开源许可与署名

本汉化版继续采用 [GNU General Public License v3.0](LICENSE.md) 开源发布，具体修改与署名见 [NOTICE.md](NOTICE.md)。

- 原项目作者：[irusanov](https://github.com/irusanov)
- 简体中文汉化维护：[Terry577](https://github.com/Terry577)
- 原项目：[irusanov/SMUDebugTool](https://github.com/irusanov/SMUDebugTool)
- 汉化项目：[Terry577/SMUDebugTool-zh-CN](https://github.com/Terry577/SMUDebugTool-zh-CN)

分发源码或编译版本时，请同时保留 GPL-3.0 许可证、原作者与贡献者版权声明、修改版说明，以及发布包中附带的第三方许可证。

## 相关项目

- [AntdUI](https://gitee.com/AntdUI/AntdUI)（现代化 WinForms 界面）
- [PawnIO](https://pawnio.eu/)（底层 I/O 驱动）
- [RTCSharp](https://github.com/tomrus88/RTCSharp)
- [ryzen_smu](https://gitlab.com/leogx9r/ryzen_smu/)
- [ryzen_nb_smu](https://github.com/flygoat/ryzen_nb_smu)
- [zenpower](https://github.com/ocerman/zenpower)
- [Linux kernel](https://github.com/torvalds/linux)
- [AMD 公开技术文档](https://www.amd.com/en/support/tech-docs)
