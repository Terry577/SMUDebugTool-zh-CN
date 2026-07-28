SMUDebugTool zh-CN - PBO 配置文件说明
====================================

此目录用于保存 Curve Optimizer 与 FMax 配置。

默认配置文件：

  profiles\co_profile.txt

文件格式示例：

  [0,-26]
  [1,-16]
  [2,-23]
  fmax=5250

说明：

- [核心编号,偏移量] 表示对应物理核心的 Curve Optimizer 偏移量。
- fmax= 表示保存的 FMax 数值。
- 点击“保存”会写入当前可用核心偏移量与 FMax。
- 点击“加载”只把文件内容加载到界面，不会立即写入硬件。
- 点击“应用”后才会把界面中的 Curve Optimizer 或 FMax 写入硬件。
- “启动时应用已保存的配置文件”会在登录 Windows 时读取此文件并应用 Curve Optimizer。
- 红色的“启动时同时应用 FMax”会额外应用此文件中的 fmax= 值。
- 勾选 FMax 启动应用时，程序会自动启用 Curve Optimizer 启动应用。
- 取消主启动选项会删除名为 RyzenSDT 的 Windows 计划任务。

安全提示：

- 配置文件与 CPU 核心编号、BIOS 和平台状态相关，不保证可在其他电脑上通用。
- 更换 CPU、更新 BIOS、启用或关闭核心后，请重新检查并保存配置。
- 建议通过程序界面生成文件；手动编辑时请使用整数并保持上述格式。
- 应用过激的负偏移或 FMax 可能造成崩溃、重启、计算错误或数据丢失。
