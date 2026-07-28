# 更新日志

本文件记录 SMUDebugTool zh-CN 相对于原项目的重要修改。上游自身的完整历史请查看 [`irusanov/SMUDebugTool`](https://github.com/irusanov/SMUDebugTool)。

## [1.41.2] - 2026-07-28

### 变更

- 将目标框架从 .NET Framework 4.5 升级到 .NET Framework 4.8.1。
- 使用 `app.config` 启用 WinForms 官方 Per-Monitor V2 动态 DPI 支持。
- 从 Manifest 移除会覆盖 WinForms 配置的旧 DPI 声明。
- 删除手工 `PerformAutoScale()`、DPI 倍率和分隔栏缩放补丁。
- 删除约 680 行未调用的旧版中文布局代码。
- 将共享导航和按钮升级逻辑迁移到新版 UI 文件。
- 将 `ChineseUiLayout` 替换为不执行手工缩放的 `UiRuntimeStyle`。
- 发布 ZIP 内附经过 SHA-256 校验的微软官方 .NET Framework 4.8.1 离线安装程序。
- 发布 ZIP 内附未经修改、经过 SHA-256 校验的 PawnIO 2.2.0 官方安装程序、GPL-2.0 许可证及对应源码。
- 增加 `RUN_SMUDEBUGTOOL.cmd`，用于检测运行时、调用安装程序并启动软件。
- 统一主要界面的圆角卡片、输入框、按钮与间距。
- 压缩 PBO、Curve Shaper、CPU、PCI、MSR 等页面布局，减少无效留白。
- 为 Curve Optimizer、FMax 与 Curve Shaper 的应用操作增加明确的成功或失败运行日志。

### 修复

- 修复现代化下拉框无法展开的问题。
- 修复数值输入框残留原生微调按钮和异常绘制的问题。
- 修复边缘、阴影、滚动条和高 DPI 下的若干布局异常。

## [1.41.1] - 2026-07-28

基于原作者 v1.41。

### 新增

- 使用 AntdUI 构建现代化简体中文界面。
- 增加运行日志与开源信息分区。
- 增加 Per-Monitor V2 DPI 感知。
- 增加“启动时同时应用 FMax”选项。
- 增加 AntdUI 许可证、PawnIO 安装说明和本地构建脚本。

### 变更

- 将主窗口固定为统一尺寸，移除用户可拖动的分隔区域。
- 将导航固定为单行按钮。
- 重做全部主要功能页的布局、间距、按钮样式和中文文本。
- Curve Optimizer 按实际 CCD 数量显示核心：
  - 单 CCD 纵向显示 8 个核心；
  - 双 CCD 时显示第二组核心；
  - 每个 CCD 只显示一组批量加减按钮。
- 配置文件保存时同时写入 FMax。
- 登录计划任务可通过 `--applyprofile --applyfmax` 同时应用 Curve Optimizer 和 FMax。
- 发布包增加 AntdUI 运行库和许可证。

### 修复

- 改善不同分辨率和缩放比例显示器之间移动窗口时的字体模糊问题。
- 修复中文界面中控件错位、间距不足和标签切换导致的布局移动。
- 修复发布目录缺少 I/O 兼容文件时无法正常初始化的问题。

## [1.41] - 2026-07-28

- 首次发布基于原作者 v1.41 的简体中文版本。
- 汉化主要界面、状态提示、确认对话框、错误信息和数据表文本。
- 增加修改版署名、GPL-3.0 说明和第三方运行文件许可。

[1.41.2]: https://github.com/Terry577/SMUDebugTool-zh-CN/compare/v1.41.1...master
[1.41.1]: https://github.com/Terry577/SMUDebugTool-zh-CN/releases/tag/v1.41.1
[1.41]: https://github.com/Terry577/SMUDebugTool-zh-CN/releases/tag/v1.41
