using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ZenStatesDebugTool
{
    /// <summary>
    /// 简体中文界面文本。保留设计器中的英文原文，便于后续合并上游更新。
    /// </summary>
    internal static class UiLocalization
    {
        private static readonly Dictionary<string, string> Translations =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Apply", "应用" },
                { "Arguments", "参数" },
                { "Argument", "参数" },
                { "Clear", "清空" },
                { "Command", "命令" },
                { "Command ID", "命令 ID" },
                { "Config", "配置" },
                { "Core Control", "核心控制" },
                { "Debug Report", "调试报告" },
                { "Decode", "解析" },
                { "Deselect the cores you want to disable and click Apply", "取消勾选要禁用的核心，然后点击“应用”" },
                { "Dump", "转储" },
                { "Dump Name", "转储文件名" },
                { "End Address", "结束地址" },
                { "End Register", "结束寄存器" },
                { "Firmware", "固件" },
                { "Form1", "结果" },
                { "Frequency", "频率" },
                { "High", "高" },
                { "High T", "高温" },
                { "Info", "信息" },
                { "Load", "加载" },
                { "Low", "低" },
                { "Low T", "低温" },
                { "Mailbox", "邮箱" },
                { "Manual", "手动" },
                { "Max", "最高" },
                { "Med", "中" },
                { "Med T", "中温" },
                { "Min", "最低" },
                { "Model", "型号" },
                { "Monitor", "监视" },
                { "OFF", "关闭" },
                { "ON", "开启" },
                { "Package", "封装" },
                { "PCI Register", "PCI 寄存器" },
                { "PCIRangeMonitor", "PCI 范围监视器" },
                { "PMTable", "功耗管理表" },
                { "PowerTableMonitor", "功耗表监视器" },
                { "Read", "读取" },
                { "Refresh", "刷新" },
                { "Refresh Interval", "刷新间隔" },
                { "Reset", "重置" },
                { "Save", "保存" },
                { "Save As...", "另存为..." },
                { "Scan", "扫描" },
                { "Send", "发送" },
                { "Single Core Frequency", "单核频率" },
                { "All Core Frequency", "全核频率" },
                { "SMU Monitor", "SMU 监视器" },
                { "Start", "开始" },
                { "Start Address", "起始地址" },
                { "Start Register", "起始寄存器" },
                { "Stop", "停止" },
                { "Value", "值" },
                { "Values", "值" },
                { "Write", "写入" },
                { "X3D Turbo Mode", "X3D 加速模式" },
                { "Apply saved profile on startup", "启动时应用已保存的配置文件" },
                { "Curve Shaper", "曲线塑形器" },
                { "MB Vendor", "主板厂商" },
                { "MB Model", "主板型号" },
                { "CMD Address", "CMD 地址" },
                { "RSP Address", "RSP 地址" },
                { "ARG Address", "ARG 地址" }
            };

        internal static void Apply(Control root)
        {
            TranslateControl(root);
        }

        private static void TranslateControl(Control control)
        {
            string translated;
            if (!string.IsNullOrEmpty(control.Text) &&
                Translations.TryGetValue(control.Text, out translated))
            {
                control.Text = translated;
            }

            DataGridView grid = control as DataGridView;
            if (grid != null)
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    if (Translations.TryGetValue(column.HeaderText, out translated))
                    {
                        column.HeaderText = translated;
                    }
                    else
                    {
                        switch (column.HeaderText)
                        {
                            case "Index": column.HeaderText = "序号"; break;
                            case "Offset": column.HeaderText = "偏移"; break;
                            case "Address": column.HeaderText = "地址"; break;
                            case "ValueFloat": column.HeaderText = "浮点值"; break;
                            case "ValueBin": column.HeaderText = "二进制值"; break;
                            case "Cmd": column.HeaderText = "命令"; break;
                            case "Arg": column.HeaderText = "参数"; break;
                            case "Rsp": column.HeaderText = "响应"; break;
                        }
                    }
                }
            }

            foreach (Control child in control.Controls)
            {
                TranslateControl(child);
            }
        }
    }
}
