# SMUDebugTool zh-CN v1.41.2

本版本继续基于原作者 SMUDebugTool v1.41，集中改进现代界面、跨屏 DPI 适配、运行反馈和发布体验。

主要更新：

- 升级到 .NET Framework 4.8.1，并启用 WinForms 官方 Per-Monitor V2 DPI 支持。
- 清理已停用的旧版中文 UI 与手工缩放逻辑，统一使用重写后的现代界面。
- 统一圆角卡片、按钮、输入框和导航样式，优化 CPU、PCI、MSR、PBO、Curve Shaper 等页面布局。
- 修复现代化下拉框无法展开、数值框异常绘制和若干边缘布局问题。
- Curve Optimizer、FMax 与 Curve Shaper 的应用操作会在运行日志中明确显示成功或失败。
- 发布包内附微软官方 .NET Framework 4.8.1 离线安装程序。
- 发布包内附未经修改且经过 SHA-256 校验的 PawnIO 2.2.0 官方安装程序、GPL-2.0 许可证及对应源码。

安装：

1. 完整解压 ZIP 到普通英文路径。
2. 首次使用时运行 `PawnIO_setup.exe` 安装底层 I/O 驱动，然后重新启动 Windows。
3. 双击 `RUN_SMUDEBUGTOOL.cmd`；它会检查 .NET Framework 4.8.1 并启动程序。
4. 涉及硬件写入时请使用管理员权限，并确认参数适用于当前硬件。

PawnIO 2.2.0 安装程序 SHA-256：

`1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032`

本项目及 PawnIO 的许可证、对应源码和第三方说明均已包含在发布包内。
