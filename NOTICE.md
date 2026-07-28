# 修改、版权与第三方组件说明

SMUDebugTool zh-CN 是基于 `irusanov/SMUDebugTool` 的开源修改版本。

## 项目信息

- 汉化版本：v1.41.2
- 上游基线：irusanov/SMUDebugTool v1.41
- 原项目：https://github.com/irusanov/SMUDebugTool
- 汉化项目：https://github.com/Terry577/SMUDebugTool-zh-CN
- 原作者：irusanov 及原项目贡献者
- 汉化维护：Terry577
- 修改开始日期：2026-07-28

## 主要修改

- 简体中文界面、运行状态、对话框、工具提示和数据表文本；
- 使用 AntdUI 重做固定尺寸现代界面；
- 增加 Per-Monitor V2 DPI 感知与多显示器缩放适配；
- 升级到 .NET Framework 4.8.1，并在发布包内附微软官方 Redistributable；
- 发布包内附经过 SHA-256 校验的 PawnIO 2.2.0 官方安装程序；
- 重做 Curve Optimizer 的 CCD 自适应核心布局；
- 增加启动时同时应用已保存 FMax 的功能；
- 增加构建、打包、运行、驱动与配置说明；
- 补充修改版署名和第三方许可证。

## 开源许可

本项目继续依照 GNU General Public License v3.0 发布，完整条款见 `LICENSE.md`。本说明用于标记修改内容、作者关系和第三方组件，不替代或缩减 GPL-3.0 的任何条款。

分发本项目的源码或二进制版本时，应同时保留：

- 原作者及原项目贡献者的版权声明；
- GNU GPL v3.0 许可证全文；
- 本项目为修改版本的明确说明；
- 对应源码的获取方式；
- 发布包内附带的第三方许可证与署名。

本汉化项目与 AMD、原作者及下列第三方组件作者不存在官方隶属或背书关系。

## 第三方组件与发布文件

- AntdUI 2.4.3：Copyright © Tom，依据 Apache License 2.0 发布；许可证见 `ThirdParty/AntdUI.LICENSE.txt`。
- PawnIO 2.2.0：用于提供底层 I/O 驱动支持；发布包内附来自官方 Release、未经修改且经过 SHA-256 校验的 `PawnIO_setup.exe`、官方 GPL-2.0 许可证、对应标签源码以及 PawnPP 子模块源码。源码见 https://github.com/namazso/PawnIO/tree/2.2.0，官方发布见 https://github.com/namazso/PawnIO.Setup/releases/tag/2.2.0。
- InpOut、WinIo 兼容运行文件及相关许可证：来自原作者 v1.41 Release，并在打包时校验上游 ZIP 的 SHA-256。
- Newtonsoft.Json、TaskScheduler、ZenStates-Core 及上游引用的其他项目：版权与许可归各自作者所有，具体信息请参考对应项目与发行文件。

原项目使用或引用的其他开源项目列表见 `README.md`。
