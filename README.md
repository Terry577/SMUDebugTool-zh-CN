# SMUDebugTool zh-CN

这是 [irusanov/SMUDebugTool](https://github.com/irusanov/SMUDebugTool) 的简体中文汉化版本。项目用于读取和写入 AMD Ryzen 平台的多种底层参数，包括手动超频、SMU、PCI、CPUID、MSR、PBO 和功耗管理表等。

![SMUDebugTool 界面截图](screenshot.png "SMUDebugTool")

## 汉化说明

- 界面按钮、标签、状态提示、确认对话框及数据表列名已汉化。
- CPU、SMU、PCI、MSR、PBO、CPUID、CCD、CCX、PROCHOT 等通用技术缩写予以保留。
- 英文设计器文本仍作为上游基线保存在源码中，程序运行时由 `UiLocalization.cs` 应用中文文本，以降低同步上游更新时的冲突。
- 如果发现漏译、错译或中文显示不完整，欢迎提交 Issue 或 Pull Request。

## 风险提示

本工具能够直接修改处理器频率、PState、SMU、PCI、MSR 等底层参数。错误的数值可能造成系统崩溃、数据丢失，甚至硬件损坏。请在充分了解相关参数并自行承担风险的前提下使用。

## 构建

1. 在 Windows 上安装 Visual Studio，并启用“.NET 桌面开发”工作负载。
2. 安装 .NET Framework 4.5 开发组件。
3. 使用 Visual Studio 打开 `ZenStatesDebugTool.sln`。
4. 还原 NuGet 软件包后，选择 `Release` 配置并生成解决方案。

驱动安装及兼容性信息请以[原项目的最新 Release 说明](https://github.com/irusanov/SMUDebugTool/releases)为准。

## 运行要求

1. 下载并完整解压 ZIP，不要只从压缩包预览窗口直接运行程序。
2. 以管理员身份运行 `SMUDebugTool.exe`。
3. 如果提示无法初始化 I/O 模块，请安装 [PawnIO 官方驱动](https://pawnio.eu/) 后重新启动程序。

发布包包含原作者 v1.41 提供的 InpOut/WinIo 兼容运行文件及相应许可证。构建脚本会校验原作者发布包的 SHA-256，避免下载内容被意外替换。

## 同步原项目

首次设置上游仓库：

```bash
git remote add upstream https://github.com/irusanov/SMUDebugTool.git
```

以后同步更新：

```bash
git fetch upstream
git merge upstream/master
```

同步后建议重点检查新增的 `.Text`、`MessageBox.Show`、`SetStatusText` 和 `HandleError` 文本，并补充到汉化中。

## 开源许可与署名

本汉化版是对原项目的修改版本，继续采用 [GNU General Public License v3.0](LICENSE.md) 发布，具体修改与署名见 [NOTICE.md](NOTICE.md)。分发源码或编译版本时，请同时保留：

- 原作者及贡献者的版权声明；
- GPL-3.0 许可证全文；
- 本项目为修改版以及修改日期的说明；
- 获取对应源代码的方式。

- 原项目作者：[irusanov](https://github.com/irusanov)
- 简体中文汉化维护：[Terry577](https://github.com/Terry577)
- 汉化开始日期：2026-07-28

## 原项目使用的相关项目

- [RTCSharp](https://github.com/tomrus88/RTCSharp)
- [ryzen_smu](https://gitlab.com/leogx9r/ryzen_smu/)
- [ryzen_nb_smu](https://github.com/flygoat/ryzen_nb_smu)
- [zenpower](https://github.com/ocerman/zenpower)
- [Linux kernel](https://github.com/torvalds/linux)
- [AMD 公开技术文档](https://www.amd.com/en/support/tech-docs)
