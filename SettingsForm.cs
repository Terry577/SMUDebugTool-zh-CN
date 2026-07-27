using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ZenStates.Core;
using ZenStatesDebugTool.Properties;
using Application = System.Windows.Forms.Application;
using static ZenStates.Core.Cpu;
using Microsoft.Win32.TaskScheduler;
using System.Security.Principal;
using System.Diagnostics;
using ZenStates.Core.Drivers;
using AntButton = AntdUI.Button;
using WinButton = System.Windows.Forms.Button;

namespace ZenStatesDebugTool
{
    public partial class SettingsForm : Form
    {
        private const string PawnIoDownloadUrl = "https://pawnio.eu/";
        private static readonly Color ThemeAccentColor =
            Color.FromArgb(22, 119, 255);
        private static readonly Color ThemeAccentHoverColor =
            Color.FromArgb(64, 150, 255);
        private static readonly Color ThemeAccentActiveColor =
            Color.FromArgb(9, 88, 217);
        private static readonly Color ThemeBorderColor =
            Color.FromArgb(214, 220, 229);
        //private static readonly int Threads = Convert.ToInt32(Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS"));
        private BackgroundWorker backgroundWorker1;
        private readonly NUMAUtil _numaUtil;
        private readonly Cpu cpu;
        List<SmuAddressSet> matches = new List<SmuAddressSet>();
        private readonly Mailbox testMailbox = new Mailbox();
        private readonly string wmiAMDACPI = "AMD_ACPI";
        private readonly string wmiScope = "root\\wmi";
        private readonly string profilesPath;
        private readonly string defaultsPath;
        private ManagementObject classInstance;
        private string instanceName;
        private ManagementBaseObject pack;
        private const string profilesFolderName = "profiles";
        private const string filename = "co_profile.txt";
        private readonly string[] args;
        private readonly bool isApplyProfile;
        private readonly bool isApplyFmax;
        private CheckBox checkBoxApplyFmaxStartup;
        private readonly Dictionary<int, NumericUpDown> coControls = new Dictionary<int, NumericUpDown>();
        private readonly Dictionary<TabPage, AntButton> fixedTabButtons =
            new Dictionary<TabPage, AntButton>();

        public SettingsForm()
        {
            InitializeComponent();
            BuildModernInterface();
            toolTip1.SetToolTip(radioButtonManualCoreControl, "手动模式可选择要禁用的特定核心和/或 SMT。");
            toolTip1.SetToolTip(radioButtonX3D, "X3D 模式会禁用 SMT，并在存在第二个 CCD 模块时将其禁用。");
            _numaUtil = new NUMAUtil();
            textBoxResult.Text = $@"检测到 NUMA 节点：{_numaUtil.HighestNumaNode + 1}" + textBoxResult.Text;

            try
            {
                profilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, profilesFolderName);
                defaultsPath =  Path.Combine(profilesPath, filename);
                
                args = Environment.GetCommandLineArgs();
                foreach (string arg in args)
                {
                    if (string.Equals(
                        arg,
                        "--applyprofile",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        isApplyProfile = true;
                    }
                    else if (string.Equals(
                        arg,
                        "--applyfmax",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        isApplyFmax = true;
                        isApplyProfile = true;
                    }
                }

                cpu = new Cpu();

                InitForm();
                ApplyModernButtons();
                UiLocalization.Apply(this);
            }
            catch (Exception ex)
            {
                ShowInitializationError(ex);
                Dispose();
                ExitApplication();
            }
        }

        private static void ShowInitializationError(Exception exception)
        {
            Exception current = exception;
            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.Message) &&
                    (current.Message.IndexOf("initializing IO module", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     current.Message.IndexOf("PawnIO is not installed", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    DialogResult result = MessageBox.Show(
                        "无法初始化硬件 I/O 模块。\n\n" +
                        "请确认已完整解压发布包、以管理员身份运行程序，并安装 PawnIO 驱动。\n\n" +
                        "是否打开 PawnIO 官方下载页面？",
                        "I/O 初始化失败",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(PawnIoDownloadUrl)
                            {
                                UseShellExecute = true
                            });
                        }
                        catch
                        {
                            // The URL is also documented in the release package.
                        }
                    }

                    return;
                }

                current = current.InnerException;
            }

            MessageBox.Show(exception.Message, Resources.Error);
        }

        private void ExitApplication()
        {
            cpu?.Dispose();

            if (Application.MessageLoop)
                Application.Exit();
            else
                Environment.Exit(1);
        }

        private void InitTestMailbox(uint msgAddr, uint rspAddr, uint argAddr)
        {
            testMailbox.SMU_ADDR_MSG = msgAddr;
            testMailbox.SMU_ADDR_RSP = rspAddr;
            testMailbox.SMU_ADDR_ARG = argAddr;
            ResetSmuAddresses();
        }

        private void InitTestMailbox(Mailbox mailbox)
        {
            testMailbox.SMU_ADDR_MSG = mailbox.SMU_ADDR_MSG;
            testMailbox.SMU_ADDR_RSP = mailbox.SMU_ADDR_RSP;
            testMailbox.SMU_ADDR_ARG = mailbox.SMU_ADDR_ARG;
            ResetSmuAddresses();
        }

        private void ResetSmuAddresses()
        {
            textBoxCMDAddress.Text = $"0x{Convert.ToString(testMailbox.SMU_ADDR_MSG, 16).ToUpper()}";
            textBoxRSPAddress.Text = $"0x{Convert.ToString(testMailbox.SMU_ADDR_RSP, 16).ToUpper()}";
            textBoxARGAddress.Text = $"0x{Convert.ToString(testMailbox.SMU_ADDR_ARG, 16).ToUpper()}";
        }

        private void DisplaySystemInfo()
        {
            try
            {
                cpuInfoLabel.Text = cpu.systemInfo.CpuName;
                modelInfoLabel.Text = $"{cpu.systemInfo.Model:X2}";
                packageTypeInfoLabel.Text = cpu.info.packageType.ToString();
                mbVendorInfoLabel.Text = cpu.systemInfo.MbVendor;
                mbModelInfoLabel.Text = cpu.systemInfo.MbName;
                biosInfoLabel.Text = cpu.systemInfo.BiosVersion;
                smuInfoLabel.Text = cpu.systemInfo.SmuVersionString;
                firmwareInfoLabel.Text = $"{cpu.systemInfo.PatchLevel:X8}";
                cpuIdLabel.Text = $"{cpu.systemInfo.CpuIdString} ({cpu.info.codeName})";
                configInfoLabel.Text = $"{cpu.info.topology.ccds} CCD / {cpu.info.topology.ccxs} CCX / {cpu.systemInfo.PhysicalCoreCount} 个物理核心";
            }
            catch { }
        }

        private void InitForm()
        {
            /*if (cpu.Status == Utils.LibStatus.PARTIALLY_OK)
            {
                if (cpu.LastError != null)
                    MessageBox.Show(cpu.LastError.Message, Resources.Error);
            }*/

            if (cpu.smu.Version == 0)
            {
                MessageBox.Show("无法获取 SMU 版本！\n" +
                    "默认 SMU 地址未响应命令。",
                    "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (!Directory.Exists(profilesPath))
            {
                MessageBox.Show("配置文件目录不存在，已为你创建。");
                Directory.CreateDirectory(profilesPath);
            }

            InitTestMailbox(cpu.smu.Rsmu);
            DisplaySystemInfo();

            pstateIdBox.SelectedIndex = 0;

            pstateDid.KeyDown += PstateFidDid_KeyDown;
            pstateDid.KeyPress += PstateFidDid_KeyPress;
            pstateDid.KeyUp += PstateFidDid_KeyUp;
            pstateFid.KeyDown += PstateFidDid_KeyDown;
            pstateFid.KeyPress += PstateFidDid_KeyPress;
            pstateFid.KeyUp += PstateFidDid_KeyUp;

            PopulateFrequencyList(comboBoxACF.Items);
            PopulateFrequencyList(comboBoxSCF.Items);
            PopulateCCDList(comboBoxCore.Items);
            PopulateMailboxesList(comboBoxMailboxSelect.Items);

            comboBoxCore.SelectedIndex = 0;
            double multi = GetCurrentMulti();
            if (multi >= 5.50)
            {
                int index = (int)((multi - 5.50) / 0.25);
                if (index > -1 && index < comboBoxACF.Items.Count && index < comboBoxSCF.Items.Count)
                {
                    comboBoxACF.SelectedIndex = index;
                    comboBoxSCF.SelectedIndex = index;
                }
            }

            InitCoreControl();
            InitPboLayout();
            InitPBO();
            InitCS();
            PopulateWmiFunctions();

            double? currentBclk = cpu.GetBclk();
            labelBCLK.Text = currentBclk + " MHz";
            numericUpDownBclk.Text = $"{currentBclk}";

            var prochotEnabled = cpu.IsProchotEnabled();
            checkBoxPROCHOT.Checked = prochotEnabled ?? false;
            //checkBoxPROCHOT.Enabled = prochotEnabled;
            //buttonApplyPROCHOT.Enabled = prochotEnabled;

            comboBoxMailboxSelect.SelectedIndex = 0;

            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(checkBoxPROCHOT, "禁用温度节流，可用于极限制冷场景。");

            if (isApplyProfile)
            {
                ApplyCOProfile();
                if (isApplyFmax)
                    ApplySavedFmax();
                InitPBO();
                tabControl1.SelectedTab = tabPagePbo;
            }

            SetStatusText($"{cpu.info.codeName}。就绪。");
        }

        private void ConfigureChineseMainLayout()
        {
            SuspendLayout();

            splitContainer1.IsSplitterFixed = true;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.Panel1MinSize = 690;
            splitContainer1.Panel2MinSize = 240;
            splitContainer1.SplitterWidth = 12;
            splitContainer1.SplitterDistance = 704;

            tabControl1.Padding = new Point(12, 5);
            tabPageCS.Text = "Curve Shaper";
            ConfigureFixedTabNavigation();
            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                tabPage.AutoScroll = true;
                tabPage.Padding = new Padding(6);
            }

            ConfigureCpuTabLayout();
            ConfigureSmuTabLayout();
            ConfigureCommandTable(tableLayoutPanel4);
            ConfigureCommandTable(tableLayoutPanel5);
            ConfigurePstateBclkLayout();
            ConfigureCommandTable(tableLayoutPanel9);
            ConfigureCommandTable(tableLayoutPanel10);
            ConfigureCpuidDecodeLayout();
            ConfigureCurveShaperLayout();
            ConfigureWmiLayout();
            ConfigureInfoLayout();
            ConfigureVisualDesign();

            ResumeLayout(true);
        }

        private void ConfigureFixedTabNavigation()
        {
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.Multiline = false;
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.Margin = new Padding(0);

            TableLayoutPanel mainHost = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = new Padding(0),
                Name = "tableLayoutPanelMainHost",
                Padding = new Padding(0),
                RowCount = 2
            };
            mainHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            mainHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel navigation = new TableLayoutPanel
            {
                BackColor = Color.FromArgb(245, 247, 250),
                ColumnCount = 10,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = new Padding(0),
                Name = "tableLayoutPanelFixedTabs",
                Padding = new Padding(0),
                RowCount = 1
            };

            for (int column = 0; column < 10; column++)
                navigation.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 10F));
            navigation.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Remove(splitContainer1);
            Controls.Remove(statusStrip1);
            mainHost.Controls.Add(navigation, 0, 0);
            mainHost.Controls.Add(splitContainer1, 0, 1);
            Controls.Add(mainHost);
            Controls.Add(statusStrip1);
            tabControl1.Dock = DockStyle.Fill;

            fixedTabButtons.Clear();
            AddFixedTabButton(navigation, tabPageCPU, "CPU", 0, 0);
            AddFixedTabButton(navigation, tabPageSmu, "SMU", 1, 0);
            AddFixedTabButton(navigation, tabPagePci, "PCI", 2, 0);
            AddFixedTabButton(navigation, tabPageMsr, "MSR", 3, 0);
            AddFixedTabButton(navigation, tabPageCPUID, "CPUID", 4, 0);
            AddFixedTabButton(navigation, tabPagePbo, "PBO", 5, 0);
            AddFixedTabButton(navigation, tabPageCS, "Curve Shaper", 6, 0);
            AddFixedTabButton(navigation, tabPageWmi, "AMD ACPI", 7, 0);
            AddFixedTabButton(navigation, tabPagePstates, "PStates", 8, 0);
            AddFixedTabButton(navigation, tabPageInfo, "信息", 9, 0);
            UpdateFixedTabButtons();
        }

        private void AddFixedTabButton(
            TableLayoutPanel navigation,
            TabPage tabPage,
            string text,
            int column,
            int row)
        {
            AntButton button = new AntButton
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(
                    column == 0 ? 8 : 3,
                    6,
                    column == 9 ? 8 : 3,
                    5),
                Padding = new Padding(0),
                Radius = 7,
                TabStop = false,
                Tag = tabPage,
                Text = text,
                WaveSize = 0
            };
            button.Font = new Font("Microsoft YaHei UI", 8.5F);
            button.Click += FixedTabButton_Click;
            navigation.Controls.Add(button, column, row);
            fixedTabButtons[tabPage] = button;
        }

        private void FixedTabButton_Click(object sender, EventArgs e)
        {
            AntButton button = sender as AntButton;
            TabPage tabPage = button == null ? null : button.Tag as TabPage;
            if (tabPage != null)
                tabControl1.SelectedTab = tabPage;
        }

        private void UpdateFixedTabButtons()
        {
            foreach (KeyValuePair<TabPage, AntButton> pair in fixedTabButtons)
            {
                bool selected = pair.Key == tabControl1.SelectedTab;
                pair.Value.Type = selected
                    ? AntdUI.TTypeMini.Primary
                    : AntdUI.TTypeMini.Default;
                pair.Value.BackColor = selected
                    ? ThemeAccentColor
                    : Color.White;
                pair.Value.BackHover = selected
                    ? ThemeAccentHoverColor
                    : Color.FromArgb(237, 244, 255);
                pair.Value.BackActive = selected
                    ? ThemeAccentActiveColor
                    : Color.FromArgb(225, 236, 252);
                pair.Value.ForeColor = selected
                    ? Color.White
                    : Color.FromArgb(38, 48, 64);
                pair.Value.ForeHover = selected
                    ? Color.White
                    : ThemeAccentColor;
                pair.Value.ForeActive = selected
                    ? Color.White
                    : ThemeAccentActiveColor;
                pair.Value.DefaultBack = Color.White;
                pair.Value.DefaultBorderColor = selected
                    ? ThemeAccentColor
                    : ThemeBorderColor;
                pair.Value.BorderWidth = 1F;
            }
        }

        private void ConfigureCpuTabLayout()
        {
            tableLayoutPanel8.SuspendLayout();
            tableLayoutPanel8.Controls.Clear();
            tableLayoutPanel8.ColumnStyles.Clear();
            tableLayoutPanel8.RowStyles.Clear();
            tableLayoutPanel8.AutoSize = false;
            tableLayoutPanel8.ColumnCount = 4;
            tableLayoutPanel8.RowCount = 3;
            tableLayoutPanel8.Height = 130;
            tableLayoutPanel8.Padding = new Padding(12, 8, 12, 8);
            tableLayoutPanel8.ColumnStyles.Clear();
            tableLayoutPanel8.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 100F));
            tableLayoutPanel8.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel8.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 105F));
            tableLayoutPanel8.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 88F));
            tableLayoutPanel8.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38F));
            tableLayoutPanel8.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38F));
            tableLayoutPanel8.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38F));

            tableLayoutPanel8.SetColumnSpan(label14, 1);
            tableLayoutPanel8.SetColumnSpan(label16, 1);
            tableLayoutPanel8.SetColumnSpan(comboBoxACF, 2);
            tableLayoutPanel8.SetColumnSpan(checkBoxPROCHOT, 3);
            label14.Text = "全核频率";
            label16.Text = "单核频率";
            label14.TextAlign = ContentAlignment.MiddleLeft;
            label16.TextAlign = ContentAlignment.MiddleLeft;

            tableLayoutPanel8.Controls.Add(label14, 0, 0);
            tableLayoutPanel8.Controls.Add(comboBoxACF, 1, 0);
            tableLayoutPanel8.Controls.Add(buttonApplyAC, 3, 0);
            tableLayoutPanel8.Controls.Add(label16, 0, 1);
            tableLayoutPanel8.Controls.Add(comboBoxSCF, 1, 1);
            tableLayoutPanel8.Controls.Add(comboBoxCore, 2, 1);
            tableLayoutPanel8.Controls.Add(buttonApplySC, 3, 1);
            tableLayoutPanel8.Controls.Add(checkBoxPROCHOT, 0, 2);
            tableLayoutPanel8.Controls.Add(buttonApplyPROCHOT, 3, 2);

            foreach (Control control in tableLayoutPanel8.Controls)
            {
                control.Dock = DockStyle.Fill;
                control.Margin = control is Label
                    ? new Padding(2, 3, 6, 3)
                    : new Padding(3, 5, 3, 5);
            }
            tableLayoutPanel8.Dock = DockStyle.Fill;
            tableLayoutPanel8.Margin = new Padding(0, 0, 0, 6);
            tableLayoutPanel8.ResumeLayout(true);

            groupBoxCoreControl.Dock = DockStyle.Fill;
            groupBoxCoreControl.Margin = new Padding(0, 6, 0, 0);
            groupBoxCoreControl.Padding = new Padding(10, 30, 10, 8);
            groupBoxCoreControl.Text = string.Empty;
            groupBoxCoreControl.Paint -= GroupBoxCoreControl_Paint;
            groupBoxCoreControl.Paint += GroupBoxCoreControl_Paint;

            Label sectionTitle = new Label
            {
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(13, 10),
                Name = "labelCoreControlSectionTitle",
                Text = "核心控制"
            };
            groupBoxCoreControl.Controls.Add(sectionTitle);
            sectionTitle.BringToFront();

            TableLayoutPanel cpuHost = new TableLayoutPanel
            {
                BackColor = Color.FromArgb(245, 247, 250),
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = new Padding(0),
                Name = "tableLayoutPanelCpuHost",
                Padding = new Padding(0),
                RowCount = 2
            };
            cpuHost.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            cpuHost.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 142F));
            cpuHost.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));

            tabPageCPU.Controls.Remove(tableLayoutPanel8);
            tabPageCPU.Controls.Remove(groupBoxCoreControl);
            cpuHost.Controls.Add(tableLayoutPanel8, 0, 0);
            cpuHost.Controls.Add(groupBoxCoreControl, 0, 1);
            tabPageCPU.Controls.Add(cpuHost);

            groupBoxCoreControl.Resize -= GroupBoxCoreControl_Resize;
            groupBoxCoreControl.Resize += GroupBoxCoreControl_Resize;
            LayoutCoreControlGroup();
        }

        private void GroupBoxCoreControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
        }

        private void GroupBoxCoreControl_Resize(object sender, EventArgs e)
        {
            LayoutCoreControlGroup();
        }

        private void LayoutCoreControlGroup()
        {
            int contentWidth = Math.Max(260, groupBoxCoreControl.ClientSize.Width - 20);

            radioButtonX3D.Location = new Point(12, 37);
            panelX3D.Size = new Size(104, 31);
            panelX3D.Location = new Point(
                Math.Max(140, groupBoxCoreControl.ClientSize.Width - panelX3D.Width - 10),
                30);
            button5.Location = new Point(2, 3);
            button5.Size = new Size(48, 25);
            button6.Location = new Point(54, 3);
            button6.Size = new Size(48, 25);

            radioButtonManualCoreControl.Location = new Point(12, 71);
            panelManualCoreControl.Location = new Point(10, 94);
            panelManualCoreControl.Size = new Size(
                contentWidth,
                Math.Max(115, groupBoxCoreControl.ClientSize.Height - 101));
            panelManualCoreControl.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;

            checkBoxSMT.Location = new Point(
                Math.Max(235, panelManualCoreControl.ClientSize.Width - 78),
                4);
            buttonApplyCoreMap.Location = new Point(
                Math.Max(235, panelManualCoreControl.ClientSize.Width - 82),
                59);
            buttonApplyCoreMap.Size = new Size(78, 27);
            label67.Location = new Point(0, panelManualCoreControl.ClientSize.Height - 23);
            label67.MaximumSize = new Size(
                Math.Max(220, panelManualCoreControl.ClientSize.Width - 4),
                0);
        }

        private void ConfigureSmuTabLayout()
        {
            tableLayoutPanel6.Padding = new Padding(6);
            tableLayoutPanel6.ColumnStyles.Clear();
            tableLayoutPanel6.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel6.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 100F));

            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 112F));
            tableLayoutPanel1.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));

            tableLayoutPanel2.AutoSize = false;
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            tableLayoutPanel2.RowStyles.Clear();
            tableLayoutPanel2.RowCount = 6;
            for (int row = 0; row < 5; row++)
            {
                tableLayoutPanel2.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 34F));
            }
            tableLayoutPanel2.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            foreach (Control control in tableLayoutPanel2.Controls)
            {
                Button button = control as Button;
                if (button != null)
                {
                    button.Dock = DockStyle.Fill;
                    button.Margin = new Padding(3, 2, 3, 2);
                }
            }
        }

        private static void ConfigureCommandTable(TableLayoutPanel table)
        {
            table.Padding = new Padding(6);
            table.ColumnStyles.Clear();
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            table.Dock = DockStyle.Top;

            foreach (Control control in table.Controls)
            {
                if (control is Button)
                {
                    control.Dock = DockStyle.Fill;
                    control.Margin = new Padding(3, 2, 3, 2);
                }
                else if (control is TextBox || control is ComboBox ||
                         control is NumericUpDown)
                {
                    control.Dock = DockStyle.Fill;
                    control.Margin = new Padding(3, 4, 3, 3);
                }
            }
        }

        private void ConfigureCpuidDecodeLayout()
        {
            tableLayoutPanel14.Padding = new Padding(6);
            tableLayoutPanel14.ColumnStyles.Clear();
            tableLayoutPanel14.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 108F));
            tableLayoutPanel14.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel14.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 12F));
            tableLayoutPanel14.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 88F));
            tableLayoutPanel14.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel14.AutoSize = false;
            tableLayoutPanel14.Height = 52;

            tabPageCPUID.Resize -= TabPageCpuid_Resize;
            tabPageCPUID.Resize += TabPageCpuid_Resize;
            LayoutCpuidDecodePanel();
        }

        private void TabPageCpuid_Resize(object sender, EventArgs e)
        {
            LayoutCpuidDecodePanel();
        }

        private void LayoutCpuidDecodePanel()
        {
            tableLayoutPanel14.Location = new Point(
                6,
                tableLayoutPanel10.Bottom + 14);
            tableLayoutPanel14.Width = Math.Max(
                300,
                tabPageCPUID.ClientSize.Width - 12);
        }

        private void ConfigureCurveShaperLayout()
        {
            tableLayoutPanel16.Padding = new Padding(6);
            tableLayoutPanel16.ColumnStyles.Clear();
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 54F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.34F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 12F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 88F));
            foreach (Control control in tableLayoutPanel16.Controls)
            {
                NumericUpDown numericControl = control as NumericUpDown;
                if (numericControl != null)
                {
                    numericControl.Dock = DockStyle.Fill;
                    numericControl.Margin = new Padding(4, 4, 4, 3);
                }
            }
        }

        private void ConfigurePstateBclkLayout()
        {
            TableLayoutPanel bclkRow = new TableLayoutPanel
            {
                ColumnCount = 4,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Name = "tableLayoutPanelBclk",
                Padding = new Padding(0),
                RowCount = 1,
                Size = new Size(200, 48)
            };
            bclkRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 108F));
            bclkRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            bclkRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 92F));
            bclkRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 88F));
            bclkRow.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 48F));

            Label bclkLabel = new Label
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 6, 0),
                Text = "BCLK",
                TextAlign = ContentAlignment.MiddleLeft
            };
            numericUpDownBclk.Dock = DockStyle.Fill;
            numericUpDownBclk.Margin = new Padding(3, 7, 3, 5);
            labelBCLK.Dock = DockStyle.Fill;
            labelBCLK.Margin = new Padding(6, 0, 3, 0);
            labelBCLK.TextAlign = ContentAlignment.MiddleLeft;
            buttonBCLKApply.Dock = DockStyle.Fill;
            buttonBCLKApply.Margin = new Padding(3, 5, 3, 5);

            bclkRow.Controls.Add(bclkLabel, 0, 0);
            bclkRow.Controls.Add(numericUpDownBclk, 1, 0);
            bclkRow.Controls.Add(labelBCLK, 2, 0);
            bclkRow.Controls.Add(buttonBCLKApply, 3, 0);

            tableLayoutPanel5.RowCount = 5;
            while (tableLayoutPanel5.RowStyles.Count < 5)
                tableLayoutPanel5.RowStyles.Add(new RowStyle());
            tableLayoutPanel5.RowStyles[4] =
                new RowStyle(SizeType.Absolute, 48F);
            tableLayoutPanel5.Controls.Add(bclkRow, 0, 4);
            tableLayoutPanel5.SetColumnSpan(bclkRow, 4);
        }

        private void ConfigureWmiLayout()
        {
            tableLayoutPanel13.Padding = new Padding(6);
            tableLayoutPanel13.ColumnStyles.Clear();
            tableLayoutPanel13.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 92F));
            tableLayoutPanel13.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            buttonWmiCmdSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonWmiCmdSend.MinimumSize = new Size(88, 27);
        }

        private void ConfigureInfoLayout()
        {
            tableLayoutPanel3.Padding = new Padding(6);
            tableLayoutPanel3.ColumnStyles.Clear();
            tableLayoutPanel3.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 92F));
            tableLayoutPanel3.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            buttonExport.AutoSize = true;
            buttonExport.MinimumSize = new Size(100, 29);
        }

        private void ConfigureVisualDesign()
        {
            Color pageColor = Color.FromArgb(245, 247, 250);
            Color cardColor = Color.White;
            Color borderColor = ThemeBorderColor;

            splitContainer1.BackColor = pageColor;
            splitContainer1.BorderStyle = BorderStyle.None;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.SplitterWidth = 12;
            splitContainer1.Panel1.BackColor = pageColor;
            splitContainer1.Panel2.BackColor = pageColor;
            splitContainer1.Panel1.Padding = new Padding(14, 12, 6, 12);
            splitContainer1.Panel2.Padding = new Padding(0, 12, 14, 12);

            statusStrip1.BackColor = pageColor;
            statusStrip1.SizingGrip = false;
            statusStrip1.Padding = new Padding(9, 0, 9, 0);

            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                tabPage.BackColor = pageColor;
                tabPage.UseVisualStyleBackColor = false;
                tabPage.Padding = new Padding(12);
            }

            TableLayoutPanel[] cards =
            {
                tableLayoutPanel1,
                tableLayoutPanel2,
                tableLayoutPanel3,
                tableLayoutPanel4,
                tableLayoutPanel5,
                tableLayoutPanel8,
                tableLayoutPanel9,
                tableLayoutPanel10,
                tableLayoutPanel12,
                tableLayoutPanel13,
                tableLayoutPanel14,
                tableLayoutPanel16
            };
            foreach (TableLayoutPanel card in cards)
            {
                card.BackColor = cardColor;
                card.BorderStyle = BorderStyle.None;
            }
            groupBoxCoreControl.BackColor = cardColor;

            ConfigureOutputPanel(cardColor, pageColor, borderColor);
            ModernUi.WrapCards(
                cards.Concat(new Control[]
                {
                    groupBoxCoreControl,
                    tableLayoutPanel11
                }),
                cardColor,
                borderColor);
        }

        private void ApplyModernButtons()
        {
            WinButton[] primaryButtons =
            {
                buttonApply,
                buttonApplyAC,
                buttonApplySC,
                buttonApplyPROCHOT,
                buttonApplyCoreMap,
                buttonPciWrite,
                buttonMsrWrite,
                buttonApplyCO,
                buttonApplyFMax,
                buttonApplyCS,
                buttonWmiCmdSend,
                buttonBCLKApply,
                btnPstateWrite,
                button5
            };
            ModernUi.UpgradeButtons(
                this,
                new HashSet<WinButton>(primaryButtons),
                toolTip1,
                ThemeAccentColor,
                ThemeAccentHoverColor,
                ThemeAccentActiveColor,
                ThemeBorderColor);
            ModernUi.UpgradeSelectionControls(
                this,
                toolTip1,
                ThemeAccentColor);
        }

        private void ConfigureOutputPanel(
            Color cardColor,
            Color pageColor,
            Color borderColor)
        {
            tableLayoutPanel11.SuspendLayout();
            tableLayoutPanel11.Controls.Clear();
            tableLayoutPanel11.ColumnStyles.Clear();
            tableLayoutPanel11.RowStyles.Clear();
            tableLayoutPanel11.BackColor = cardColor;
            tableLayoutPanel11.BorderStyle = BorderStyle.None;
            tableLayoutPanel11.ColumnCount = 1;
            tableLayoutPanel11.RowCount = 2;
            tableLayoutPanel11.Padding = new Padding(0);
            tableLayoutPanel11.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel11.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40F));
            tableLayoutPanel11.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));

            Label outputHeader = new Label
            {
                BackColor = cardColor,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(20, 0, 0, 0),
                Text = "输出",
                TextAlign = ContentAlignment.MiddleLeft
            };
            outputHeader.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(borderColor))
                {
                    e.Graphics.DrawLine(
                        pen,
                        0,
                        outputHeader.Height - 1,
                        outputHeader.Width,
                        outputHeader.Height - 1);
                }
                using (Pen pen = new Pen(ThemeAccentColor, 3F))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    e.Graphics.DrawLine(
                        pen,
                        10,
                        13,
                        10,
                        outputHeader.Height - 13);
                }
            };

            textBoxResult.BackColor = cardColor;
            textBoxResult.BorderStyle = BorderStyle.None;
            textBoxResult.Dock = DockStyle.Fill;
            textBoxResult.Margin = new Padding(12, 10, 8, 8);
            tableLayoutPanel11.Controls.Add(outputHeader, 0, 0);
            tableLayoutPanel11.Controls.Add(textBoxResult, 0, 1);
            tableLayoutPanel11.ResumeLayout(true);
        }

        private void ApplyCOProfile ()
        {
            numericUpDownFmax.Tag = null;
            List<Tuple<int, int>> margins = LoadCOProfile();
            if (margins.Count > 0 && cpu.smu.Rsmu.SMU_MSG_SetDldoPsmMargin != 0)
            {
                foreach (var margin in margins)
                {
                    int index = margin.Item1;
                    int value = margin.Item2;
                    int mapIndex = index < 8 ? 0 : 1;
                    if ((~cpu.info.topology.coreDisableMap[mapIndex] >> index % 8 & 1) == 1)
                    {
                        cpu.SetPsmMarginSingleCore(EncodeCoreMarginBitmask(index), Convert.ToInt32(value));
                    }
                }
            }
        }

        private void ApplySavedFmax()
        {
            if (!(numericUpDownFmax.Tag is decimal))
            {
                HandleError(
                    "配置文件中没有可应用的 FMax 值。");
                return;
            }

            decimal savedFmax =
                (decimal)numericUpDownFmax.Tag;
            uint targetFmax = decimal.ToUInt32(savedFmax);
            if (cpu.SetFMax(targetFmax))
            {
                numericUpDownFmax.Value = cpu.GetFMax();
                textBoxResult.Text =
                    string.Format(
                        "启动时已应用 FMax：{0} MHz。",
                        numericUpDownFmax.Value) +
                    Environment.NewLine +
                    textBoxResult.Text;
            }
            else
            {
                HandleError(
                    string.Format(
                        "启动时应用 FMax {0} MHz 失败。",
                        targetFmax));
            }
        }

        // TODO: Detect OC Mode and return PState freq if on auto
        private double GetCurrentMulti()
        {
            double multi = cpu.GetCoreMulti();
            if (multi == 0)
                SetStatusText(@"无法获取当前频率！");

            return multi;
        }

        private void PopulateFrequencyList(ComboBox.ObjectCollection l)
        {
            for (double multi = 5.5; multi <= 70; multi += 0.25)
            {
                l.Add((object)new FrequencyListItem(multi, string.Format("x{0:0.00}", multi)));
            }
        }

        private void PopulateCCDList(ComboBox.ObjectCollection l)
        {
            int ccxInCcd = cpu.info.family == Cpu.Family.FAMILY_19H ? 1 : 2;
            int coresInCcx = 8 / ccxInCcd;
            for (int core = 0; core < cpu.info.topology.cores; ++core)
                l.Add(new CoreListItem(core / 8, core / coresInCcx, core));
        }

        private void PopulateMailboxesList(ComboBox.ObjectCollection l)
        {
            l.Clear();
            l.Add(new MailboxListItem("RSMU", cpu.smu.Rsmu));
            l.Add(new MailboxListItem("MP1", cpu.smu.Mp1Smu));
            l.Add(new MailboxListItem("HSMP", cpu.smu.Hsmp));
        }

        private void AddMailboxToList(string label, SmuAddressSet addressSet)
        {
            comboBoxMailboxSelect.Items.Add(new MailboxListItem(label, addressSet));
        }

        private void InitCoreControl()
        {
            uint cores = (uint)GetPhysicalCoreCount();
            //var performanceOfCores = cpu.info.topology.performanceOfCore;
            uint coresPerGroup = 8;
            uint logicalIndexGroup1 = 0;
            uint logicalIndexGroup2 = 0;

            for (uint i = 0; i < cores; i++)
            {
                uint mapIndex = i / coresPerGroup;
                uint coreInGroup = i % coresPerGroup;
                //bool isDisabled = ((~cpu.info.topology.coreDisableMap[mapIndex] >> (int)coreInGroup) & 1) == 0;

                if (IsCoreEnabled((int)i))
                {
                    try
                    {
                        CheckBox control = (CheckBox)Controls.Find($"checkBox{i}", true)[0];
                        if (control != null)
                        {
                            control.Enabled = true;
                            control.Checked = true;

                            if (mapIndex == 0) // Group 1
                            {
                                control.Tag = $"{logicalIndexGroup1}";
                                //var performanceOfCore = performanceOfCores[logicalIndexGroup1];
                                logicalIndexGroup1++;
                            }
                            else // Group 2
                            {
                                control.Tag = $"{logicalIndexGroup2}";
                                logicalIndexGroup2++;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error initializing core {i}: {e}");
                    }
                }
            }

            checkBoxSMT.Checked = cpu.systemInfo.SMT;
        }

        private static int ConvertMarginToInt(uint value)
        {
            return (sbyte)(unchecked(value));
        }

        private void InitCS(bool showStatus = false)
        {
            uint[] csValues = cpu.GetAllCurveShaperMargins();

            cs_min_low.Value = ConvertMarginToInt(csValues[0] >> 8 & 0xFF);
            cs_min_med.Value = ConvertMarginToInt(csValues[0] >> 16 & 0xFF);
            cs_min_high.Value = ConvertMarginToInt(csValues[0] >> 24 & 0xFF);

            cs_low_low.Value = ConvertMarginToInt(csValues[1] >> 8 & 0xFF);
            cs_low_med.Value = ConvertMarginToInt(csValues[1] >> 16 & 0xFF);
            cs_low_high.Value = ConvertMarginToInt(csValues[1] >> 24 & 0xFF);

            cs_med_low.Value = ConvertMarginToInt(csValues[2] >> 8 & 0xFF);
            cs_med_med.Value = ConvertMarginToInt(csValues[2] >> 16 & 0xFF);
            cs_med_high.Value = ConvertMarginToInt(csValues[2] >> 24 & 0xFF);

            cs_high_low.Value = ConvertMarginToInt(csValues[3] >> 8 & 0xFF);
            cs_high_med.Value = ConvertMarginToInt(csValues[3] >> 16 & 0xFF);
            cs_high_high.Value = ConvertMarginToInt(csValues[3] >> 24 & 0xFF);

            cs_max_low.Value = ConvertMarginToInt(csValues[4] >> 8 & 0xFF);
            cs_max_med.Value = ConvertMarginToInt(csValues[4] >> 16 & 0xFF);
            cs_max_high.Value = ConvertMarginToInt(csValues[4] >> 24 & 0xFF);

            if (showStatus)
                SetStatusText("曲线塑形器裕量已刷新。");
        }

        private void InitPBO()
        {
            if (cpu.smu.Rsmu.SMU_MSG_SetDldoPsmMargin != 0)
            {
                uint cores = (uint)GetPhysicalCoreCount();
                for (var i = 0; i < cores; i++)
                {
                    if (IsCoreEnabled(i))
                    {
                        NumericUpDown control = GetCOControl(i);
                        if (control != null)
                        {
                            control.Enabled = true;
                            uint? margin = cpu.GetPsmMarginSingleCore(EncodeCoreMarginBitmask(i));
                            if (margin != null)
                                control.Value = Convert.ToDecimal((int)margin);
                        }
                    }
                }
            }

            /*using (RegistryKey key = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    checkBoxApplyCOStartup.Checked = key.GetValue("RyzenSDT") != null;
                }
            }*/

            bool startupEnabled = TaskExists("RyzenSDT");
            checkBoxApplyCOStartup.Checked = startupEnabled;
            checkBoxApplyFmaxStartup.Checked =
                startupEnabled &&
                TaskHasArgument("RyzenSDT", "--applyfmax");
            checkBoxApplyFmaxStartup.Enabled = true;
            numericUpDownFmax.Value = cpu.GetFMax();
        }

        private void InitPboLayout()
        {
            tableLayoutPanel12.SuspendLayout();

            tableLayoutPanel12.Controls.Clear();
            tableLayoutPanel12.ColumnStyles.Clear();
            tableLayoutPanel12.RowStyles.Clear();
            tableLayoutPanel12.AutoSize = false;
            tableLayoutPanel12.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            tableLayoutPanel12.ColumnCount = 2;
            tableLayoutPanel12.RowCount = 4;
            tableLayoutPanel12.Padding = new Padding(5, 4, 5, 4);
            tableLayoutPanel12.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel12.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 88F));
            tableLayoutPanel12.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel12.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 31F));
            tableLayoutPanel12.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 37F));
            tableLayoutPanel12.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 36F));

            ConfigurePboActionColumn();
            ConfigurePboFooter();
            BuildCcdBlocks();

            tableLayoutPanel12.SetColumnSpan(flowLayoutPanelCOList, 1);
            tableLayoutPanel12.SetColumnSpan(flowLayoutPanelPboActions, 1);
            tableLayoutPanel12.Controls.Add(flowLayoutPanelCOList, 0, 0);
            tableLayoutPanel12.Controls.Add(flowLayoutPanelPboActions, 1, 0);
            tableLayoutPanel12.Controls.Add(checkBoxApplyCOStartup, 0, 1);
            tableLayoutPanel12.SetColumnSpan(checkBoxApplyCOStartup, 2);
            tableLayoutPanel12.Controls.Add(
                checkBoxApplyFmaxStartup,
                0,
                2);
            tableLayoutPanel12.SetColumnSpan(
                checkBoxApplyFmaxStartup,
                2);

            TableLayoutPanel fmaxRow = new TableLayoutPanel
            {
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Name = "tableLayoutPanelFmax",
                RowCount = 1
            };
            fmaxRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
            fmaxRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fmaxRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            fmaxRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            fmaxRow.Controls.Add(label51, 0, 0);
            fmaxRow.Controls.Add(numericUpDownFmax, 1, 0);
            fmaxRow.Controls.Add(buttonApplyFMax, 2, 0);
            tableLayoutPanel12.Controls.Add(fmaxRow, 0, 3);
            tableLayoutPanel12.SetColumnSpan(fmaxRow, 2);

            tableLayoutPanel12.ResumeLayout(true);
        }

        private void ConfigurePboActionColumn()
        {
            flowLayoutPanelPboActions.Controls.Clear();
            flowLayoutPanelPboActions.ColumnStyles.Clear();
            flowLayoutPanelPboActions.RowStyles.Clear();
            flowLayoutPanelPboActions.AutoSize = false;
            flowLayoutPanelPboActions.GrowStyle =
                TableLayoutPanelGrowStyle.AddRows;
            flowLayoutPanelPboActions.ColumnCount = 1;
            flowLayoutPanelPboActions.RowCount = 6;
            flowLayoutPanelPboActions.Dock = DockStyle.Fill;
            flowLayoutPanelPboActions.Margin = new Padding(5, 0, 0, 0);
            flowLayoutPanelPboActions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            flowLayoutPanelPboActions.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 28F));
            for (int row = 1; row <= 4; row++)
            {
                flowLayoutPanelPboActions.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 28F));
            }
            flowLayoutPanelPboActions.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));

            buttonApplyCO.Text = "应用";
            ConfigurePboActionButton(buttonApplyCO);
            ConfigurePboActionButton(buttonGetCO);
            ConfigurePboActionButton(btnSaveCOProfile);
            ConfigurePboActionButton(btnLoadCOProfile);

            flowLayoutPanelPboActions.Controls.Add(buttonApplyCO, 0, 1);
            flowLayoutPanelPboActions.Controls.Add(buttonGetCO, 0, 2);
            flowLayoutPanelPboActions.Controls.Add(btnSaveCOProfile, 0, 3);
            flowLayoutPanelPboActions.Controls.Add(btnLoadCOProfile, 0, 4);
        }

        private static void ConfigurePboActionButton(Button button)
        {
            button.AutoSize = false;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 2, 0, 2);
            button.Padding = new Padding(0);
        }

        private void ConfigurePboFooter()
        {
            if (checkBoxApplyFmaxStartup == null)
            {
                checkBoxApplyFmaxStartup = new CheckBox
                {
                    AutoSize = true,
                    ForeColor = Color.FromArgb(205, 45, 45),
                    Name = "checkBoxApplyFmaxStartup",
                    TabIndex = 56,
                    Text = "启动时同时应用 FMax"
                };
                checkBoxApplyFmaxStartup.Click +=
                    CheckBoxApplyFmaxStartup_Click;
                toolTip1.SetToolTip(
                    checkBoxApplyFmaxStartup,
                    "登录时同时应用配置文件中保存的 FMax 值。");
            }

            checkBoxApplyCOStartup.AutoSize = true;
            checkBoxApplyCOStartup.Dock = DockStyle.Left;
            checkBoxApplyCOStartup.Margin = new Padding(2, 2, 0, 2);

            checkBoxApplyFmaxStartup.Dock = DockStyle.Left;
            checkBoxApplyFmaxStartup.Margin =
                new Padding(2, 2, 0, 6);

            label51.AutoSize = true;
            label51.Dock = DockStyle.Fill;
            label51.Margin = new Padding(2, 4, 3, 3);
            label51.TextAlign = ContentAlignment.MiddleLeft;

            numericUpDownFmax.Dock = DockStyle.Fill;
            numericUpDownFmax.Margin = new Padding(0, 4, 5, 3);

            buttonApplyFMax.AutoSize = false;
            buttonApplyFMax.Dock = DockStyle.Fill;
            buttonApplyFMax.Margin = new Padding(0, 2, 0, 2);
        }

        private void BuildCcdBlocks()
        {
            coControls.Clear();

            flowLayoutPanelCOList.SuspendLayout();
            flowLayoutPanelCOList.Controls.Clear();
            flowLayoutPanelCOList.AutoScroll = true;
            flowLayoutPanelCOList.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelCOList.WrapContents = false;
            flowLayoutPanelCOList.Dock = DockStyle.Fill;
            flowLayoutPanelCOList.Margin = new Padding(0);
            flowLayoutPanelCOList.Padding = new Padding(0);

            int ccdCount = GetCcdCount();
            for (int firstCcd = 0; firstCcd < ccdCount; firstCcd += 2)
            {
                TableLayoutPanel section = BuildCcdPairSection(
                    firstCcd,
                    firstCcd + 1 < ccdCount ? (int?)firstCcd + 1 : null);
                flowLayoutPanelCOList.Controls.Add(section);
            }

            ResizePboCoreSections();
            flowLayoutPanelCOList.SizeChanged -= FlowLayoutPanelCOList_SizeChanged;
            flowLayoutPanelCOList.SizeChanged += FlowLayoutPanelCOList_SizeChanged;
            flowLayoutPanelCOList.ResumeLayout();
        }

        private TableLayoutPanel BuildCcdPairSection(int leftCcd, int? rightCcd)
        {
            List<int> leftCores = GetCoresForCcd(leftCcd);
            if (!rightCcd.HasValue)
                return BuildSingleCcdSection(leftCcd, leftCores);

            List<int> rightCores = GetCoresForCcd(rightCcd.Value);
            int coreRows = Math.Max(leftCores.Count, rightCores.Count);
            TableLayoutPanel section = new TableLayoutPanel
            {
                ColumnCount = 6,
                RowCount = coreRows + 2,
                Height = (coreRows + 2) * 27,
                Margin = new Padding(0, 0, 0, 5),
                Name = $"tableLayoutPanelCcdPair_{leftCcd}",
                Padding = new Padding(0)
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < section.RowCount; row++)
                section.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));

            section.Controls.Add(CreateCcdTitleLabel(leftCcd), 0, 0);
            section.Controls.Add(CreateCcdTitleLabel(rightCcd.Value), 3, 0);
            section.Controls.Add(CreateCcdAdjustButton(leftCcd, 1), 1, 0);
            section.Controls.Add(CreateCcdAdjustButton(rightCcd.Value, 1), 4, 0);
            section.Controls.Add(CreateCcdAdjustButton(leftCcd, -1), 1, coreRows + 1);
            section.Controls.Add(CreateCcdAdjustButton(rightCcd.Value, -1), 4, coreRows + 1);

            AddCoreColumn(section, leftCores, 0);
            AddCoreColumn(section, rightCores, 3);
            return section;
        }

        private TableLayoutPanel BuildSingleCcdSection(int ccd, IList<int> cores)
        {
            int coreRows = cores.Count;
            TableLayoutPanel section = new TableLayoutPanel
            {
                ColumnCount = 6,
                RowCount = coreRows + 2,
                Height = (coreRows + 2) * 27,
                Margin = new Padding(0, 0, 0, 5),
                Name = $"tableLayoutPanelCcd_{ccd}",
                Padding = new Padding(0)
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < section.RowCount; row++)
                section.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));

            section.Controls.Add(CreateCcdTitleLabel(ccd), 0, 0);
            section.Controls.Add(CreateCcdAdjustButton(ccd, 1), 1, 0);
            section.Controls.Add(CreateCcdAdjustButton(ccd, -1), 1, coreRows + 1);
            AddCoreColumn(section, cores, 0);
            return section;
        }

        private static Label CreateCcdTitleLabel(int ccd)
        {
            return new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                Margin = new Padding(2, 0, 0, 0),
                Text = $"CCD {ccd}",
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private List<int> GetCoresForCcd(int ccd)
        {
            int startCore = ccd * 8;
            int endCore = Math.Min(startCore + 8, GetPhysicalCoreCount());
            return Enumerable.Range(startCore, Math.Max(0, endCore - startCore)).ToList();
        }

        private void AddCoreColumn(TableLayoutPanel section, IList<int> coreIndexes, int labelColumn)
        {
            for (int index = 0; index < coreIndexes.Count; index++)
            {
                int coreIndex = coreIndexes[index];
                Label label = new Label
                {
                    AutoEllipsis = true,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2, 0, 0, 0),
                    Name = $"labelCO_{coreIndex}",
                    Text = $"核心 {coreIndex}",
                    TextAlign = ContentAlignment.MiddleLeft
                };
                NumericUpDown marginControl = new NumericUpDown
                {
                    Dock = DockStyle.Fill,
                    Enabled = false,
                    Margin = new Padding(1, 4, 3, 3),
                    Maximum = 999,
                    Minimum = -999,
                    Name = $"numericUpDownCO_{coreIndex}",
                    Tag = coreIndex
                };

                section.Controls.Add(label, labelColumn, index + 1);
                section.Controls.Add(marginControl, labelColumn + 1, index + 1);
                coControls[coreIndex] = marginControl;
            }
        }

        private Button CreateCcdAdjustButton(int ccd, int step)
        {
            Button button = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 2, 3, 2),
                Tag = Tuple.Create(ccd, step),
                Text = step > 0 ? "+" : "\u2212",
                UseVisualStyleBackColor = true
            };
            button.Click += CcdBulkButton_Click;
            toolTip1.SetToolTip(
                button,
                $"将 CCD {ccd} 的全部核心偏移量{(step > 0 ? "增加" : "减少")} 1");
            return button;
        }

        private void FlowLayoutPanelCOList_SizeChanged(object sender, EventArgs e)
        {
            ResizePboCoreSections();
        }

        private void ResizePboCoreSections()
        {
            int width = flowLayoutPanelCOList.ClientSize.Width -
                        flowLayoutPanelCOList.Padding.Horizontal - 1;
            if (flowLayoutPanelCOList.VerticalScroll.Visible)
                width -= SystemInformation.VerticalScrollBarWidth;

            width = Math.Max(210, width);
            foreach (Control control in flowLayoutPanelCOList.Controls)
                control.Width = width;
        }

        private void CcdBulkButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Tuple<int, int> action = button?.Tag as Tuple<int, int>;
            if (action != null)
            {
                BulkMarginChangeHandler(action.Item1, action.Item2);
            }
        }

        private NumericUpDown GetCOControl(int coreIndex)
        {
            NumericUpDown control;
            return coControls.TryGetValue(coreIndex, out control) ? control : null;
        }

        private int GetCcdCount()
        {
            if (cpu.info.topology.ccds > 0)
            {
                return (int)cpu.info.topology.ccds;
            }

            return Math.Max(1, (int)Math.Ceiling(GetPhysicalCoreCount() / 8.0));
        }

        private int GetPhysicalCoreCount()
        {
            return (int)cpu.info.topology.physicalCores;
        }

        private bool IsCoreEnabled(int coreIndex)
        {
            int mapIndex = coreIndex / 8;
            int coreInGroup = coreIndex % 8;
            return mapIndex >= 0
                && mapIndex < cpu.info.topology.coreDisableMap.Length
                && ((~cpu.info.topology.coreDisableMap[mapIndex] >> coreInGroup) & 1) == 1;
        }

        private void ApplyFrequencyAllCoreSetting(int frequency)
        {
            if (cpu.SetFrequencyAllCore(Convert.ToUInt32(frequency)))
                SetStatusText(string.Format("频率已设置为 {0} MHz！", frequency));
            else
                HandleError("设置频率时出错！");
        }

        private void ApplyFrequencySingleCoreSetting(CoreListItem i, int frequency)
        {
            uint coreMask = Convert.ToUInt32(((i.CCD << 4 | i.CCX % 2 & 15) << 4 | i.CORE % 4 & 15) << 20);
            if (cpu.SetFrequencySingleCore(coreMask, Convert.ToUInt32(frequency)))
                SetStatusText(string.Format("核心 {0} 的频率已设置为 {1} MHz！", i, frequency));
            else
                HandleError("设置频率时出错！");
        }

        private void EnableOCMode(bool prochotEnabled = true)
        {
            if (cpu.smu.SendSmuCommand(cpu.smu.Rsmu, cpu.smu.Rsmu.SMU_MSG_EnableOcMode, prochotEnabled ? 0U : 0x1000000))
                SetStatusText(prochotEnabled ? "PROCHOT 已启用。" : "PROCHOT 已禁用。");
            else
                HandleError("设置超频模式时出错！");
        }

        private void DisableOCMode()
        {
            if (cpu.DisableOcMode() == SMU.Status.OK)
                SetStatusText("设置成功！");
            else
                HandleError("禁用超频模式时出错！");
        }

        private void SetStatusText(string status)
        {
            labelStatus.Text = status;
            Console.WriteLine($"CMD Status: {status}");
        }

        private void SetButtonsState(bool enabled = true)
        {
            buttonApply.Enabled = enabled;
            buttonDefaults.Enabled = enabled;
            buttonProbe.Enabled = enabled;
            buttonPciRead.Enabled = enabled;
            buttonPciScan.Enabled = enabled;
            buttonExport.Enabled = enabled;
            buttonMsrRead.Enabled = enabled;
            buttonMsrScan.Enabled = enabled;
            buttonMsrWrite.Enabled = enabled;
            buttonPMTable.Enabled = enabled;
            buttonSmuLog.Enabled = enabled;

            textBoxCMDAddress.Enabled = enabled;
            textBoxRSPAddress.Enabled = enabled;
            textBoxARGAddress.Enabled = enabled;
            textBoxCMD.Enabled = enabled;
            textBoxARG0.Enabled = enabled;
            textBoxPciAddress.Enabled = enabled;
            textBoxPciValue.Enabled = enabled;
            textBoxPciStartReg.Enabled = enabled;
            textBoxPciEndReg.Enabled = enabled;
            textBoxMsrAddress.Enabled = enabled;
            textBoxMsrEdx.Enabled = enabled;
            textBoxMsrEax.Enabled = enabled;
            textBoxMsrStart.Enabled = enabled;
            textBoxMsrEnd.Enabled = enabled;
            comboBoxMailboxSelect.Enabled = enabled;
            // textBoxResult.Enabled = enabled;
        }

        private void TryConvertToUint(string text, out uint address)
        {
            try
            {
                address = Convert.ToUInt32(text.Trim().ToLower(), 16);
            }
            catch
            {
                throw new ApplicationException("十六进制值无效。");
            }
        }

        private void HandleError(string message, string title = "错误")
        {
            SetStatusText(Resources.Error);
            MessageBox.Show(message, title);
        }

        private void ShowResultMessageBox(uint data)
        {
            uint[] d = { data };
            ShowResultMessageBox(d);
        }

        private void ShowResultMessageBox(uint[] data)
        {
            string responseString = "";
            string[] hexArray = new string[data.Length];
            string[] decArray = new string[data.Length];
            string[] binArray = new string[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                hexArray[i] = $"0x{Convert.ToString(data[i], 16).ToUpper()}";
                decArray[i] = $"{Convert.ToString(data[i], 10).ToUpper()}";
                binArray[i] = $"{Convert.ToString(data[i], 2).ToUpper()}";
            }

            responseString += "HEX: " + string.Join(", ", hexArray);
            responseString += Environment.NewLine;
            responseString += "DEC: " + string.Join(", ", decArray);
            responseString += Environment.NewLine;
            responseString += "BIN: " + string.Join(", ", binArray);
            responseString += Environment.NewLine;
            responseString += Environment.NewLine;

            Console.WriteLine($"Response: {responseString}");
            textBoxResult.Text = responseString + textBoxResult.Text;
        }

        private void ShowResult(uint data)
        {
            string responseString =
                $"REG: {textBoxPciAddress.Text.Trim()}" +
                Environment.NewLine +
                $"HEX: 0x{Convert.ToString(data, 16).ToUpper()}" +
                Environment.NewLine +
                $"INT: {Convert.ToString(data, 10).ToUpper()}" +
                Environment.NewLine +
                $"BIN: {Convert.ToString(data, 2).PadLeft(32, '0')}" +
                Environment.NewLine +
                Environment.NewLine;
            Console.WriteLine($"Response: {responseString}");
            textBoxResult.Text = responseString + textBoxResult.Text;
        }

        private void ShowResultForm(string title="结果", string result="无结果")
        {
            Invoke(new MethodInvoker(delegate
            {
                var resultForm = new ResultForm();
                resultForm.textBoxFormResult.Text = result;
                resultForm.Text = title;
                resultForm.Show();
            }));
        }

        // TODO: Show all args
        private void ApplySettings()
        {
            try
            {
                uint[] args = ZenStates.Core.Utils.MakeCmdArgs();
                string[] userArgs = textBoxARG0.Text.Trim().Split(',');

                TryConvertToUint(textBoxCMDAddress.Text, out uint addrMsg);
                TryConvertToUint(textBoxRSPAddress.Text, out uint addrRsp);
                TryConvertToUint(textBoxARGAddress.Text, out uint addrArg);
                TryConvertToUint(textBoxCMD.Text, out uint command);

                testMailbox.SMU_ADDR_MSG = addrMsg;
                testMailbox.SMU_ADDR_RSP = addrRsp;
                testMailbox.SMU_ADDR_ARG = addrArg;

                for (var i = 0; i < userArgs.Length; i++)
                {
                    if (i == args.Length)
                        break;

                    TryConvertToUint(userArgs[i], out uint temp);
                    args[i] = temp;
                }
                

                Console.WriteLine("MSG Address:  0x" + Convert.ToString(testMailbox.SMU_ADDR_MSG, 16).ToUpper());
                Console.WriteLine("RSP Address:  0x" + Convert.ToString(testMailbox.SMU_ADDR_RSP, 16).ToUpper());
                Console.WriteLine("ARG0 Address: 0x" + Convert.ToString(testMailbox.SMU_ADDR_ARG, 16).ToUpper());
                Console.WriteLine("ARG0        : 0x" + Convert.ToString(args[0], 16).ToUpper());

                SMU.Status status = cpu.smu.SendSmuCommand(testMailbox, command, ref args);

                if (status == SMU.Status.OK)
                {
                    ShowResultMessageBox(args);
                }

                SetStatusText(GetSMUStatus.GetByType(status));
            }
            catch (ApplicationException ex)
            {
                HandleError(ex.Message);
            }
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            InitTestMailbox(cpu.smu.Rsmu);
            comboBoxMailboxSelect.SelectedIndex = 0;
            textBoxCMD.Value = 1;
            textBoxARG0.Text = "0";
        }

        private void ButtonApply_Click(object sender, EventArgs e)
        {
            try
            {
                ApplySettings();
            }
            catch (ApplicationException ex)
            {
                HandleError(ex.Message, "读取响应时出错");
            }
        }

        private void HandlePciReadBtnClick()
        {
            try
            {
                SetStatusText("正在读取，请稍候...");
                SetButtonsState(false);

                TryConvertToUint(textBoxPciAddress.Text, out uint address);
                uint data = cpu.ReadDword(address);

                textBoxPciValue.Text = $"0x{data:X8}";

                SetButtonsState();
                SetStatusText(GetSMUStatus.GetByType(SMU.Status.OK));
                ShowResult(data);
            }
            catch (ApplicationException ex)
            {
                SetButtonsState();
                HandleError(ex.Message);
            }
        }

        private void HandlePciWriteBtnClick()
        {
            try
            {
                SetStatusText("正在写入，请稍候...");
                SetButtonsState(false);

                TryConvertToUint(textBoxPciAddress.Text, out uint address);
                TryConvertToUint(textBoxPciValue.Text, out uint data);

                bool res = false;
                if (cpu.WriteDwordEx(cpu.smu.SMU_OFFSET_ADDR, address))
                    res = cpu.WriteDwordEx(cpu.smu.SMU_OFFSET_DATA, data);

                if (res)
                    SetStatusText("写入成功。");
                else
                    SetStatusText(Resources.Error);

                SetButtonsState();
            }
            catch (ApplicationException ex)
            {
                SetButtonsState();
                HandleError(ex.Message);
            }
        }

        private void ButtonPciRead_Click(object sender, EventArgs e)
        {
            HandlePciReadBtnClick();
        }

        private void TextBoxPciAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                HandlePciReadBtnClick();
        }

        private void ButtonPciWrite_Click(object sender, EventArgs e)
        {
            HandlePciWriteBtnClick();
        }

        private void TextBoxPciValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                HandlePciWriteBtnClick();
        }

        private SMU.Status TrySettings(uint msgAddr, uint rspAddr, uint argAddr, uint cmd, uint value)
        {
            uint[] args = new uint[6];
            args[0] = value;

            testMailbox.SMU_ADDR_MSG = msgAddr;
            testMailbox.SMU_ADDR_RSP = rspAddr;
            testMailbox.SMU_ADDR_ARG = argAddr;

            return cpu.smu.SendSmuCommand(testMailbox, cmd, ref args);
        }

        private void ScanSmuRange(uint start, uint end, uint step, uint offset)
        {
            matches = new List<SmuAddressSet>();

            List<KeyValuePair<uint, uint>> temp = new List<KeyValuePair<uint, uint>>();

            while (start <= end)
            {
                uint smuRspAddress = start + offset;
 
                if (cpu.ReadDword(start) != 0xFFFFFFFF)
                {
                    // Send unknown command 0xFF to each pair of this start and possible response addresses
                    if (cpu.WriteDwordEx(start, 0xFF))
                    {
                        Thread.Sleep(10);

                        while (smuRspAddress <= end)
                        {
                            // Expect UNKNOWN_CMD status to be returned if the mailbox works
                            if (cpu.ReadDword(smuRspAddress) == 0xFE)
                            {
                                // Send Get_SMU_Version command
                                if (cpu.WriteDwordEx(start, 0x2))
                                {
                                    Thread.Sleep(10);
                                    if (cpu.ReadDword(smuRspAddress) == 0x1)
                                        temp.Add(new KeyValuePair<uint, uint>(start, smuRspAddress));
                                }
                            }
                            smuRspAddress += step;
                        }
                    }
                }

                start += step;
            }

            if (temp.Count > 0)
            {
                for (var i = 0; i < temp.Count; i++)
                {
                    Console.WriteLine($"{temp[i].Key:X8}: {temp[i].Value:X8}");
                }

                Console.WriteLine();
            }

            List<uint> possibleArgAddresses = new List<uint>();

            foreach (var pair in temp)
            {
                Console.WriteLine($"Testing {pair.Key:X8}: {pair.Value:X8}");

                if (TrySettings(pair.Key, pair.Value, 0xFFFFFFFF, 0x2, 0xFF) == SMU.Status.OK)
                {
                    var smuArgAddress = pair.Value + 4;
                    while (smuArgAddress <= end)
                    {
                        if (cpu.ReadDword(smuArgAddress) == cpu.smu.Version)
                        {
                            possibleArgAddresses.Add(smuArgAddress);
                        }
                        smuArgAddress += step;
                    }
                }

                // Verify the arg address returns correct value (should be test argument + 1)
                foreach (var address in possibleArgAddresses)
                {
                    uint testArg = 0xFAFAFAFA;
                    var retries = 3;

                    while (retries > 0)
                    {
                        testArg++;
                        retries--;

                        // Send test command
                        if (TrySettings(pair.Key, pair.Value, address, 0x1, testArg) == SMU.Status.OK)
                            if (cpu.ReadDword(address) != testArg + 1)
                                retries = -1;
                    }

                    if (retries == 0)
                    {
                        matches.Add(new SmuAddressSet(pair.Key, pair.Value, address));

                        string responseString =
                                $"CMD:  0x{pair.Key:X8}" +
                                Environment.NewLine +
                                $"RSP:  0x{pair.Value:X8}" +
                                Environment.NewLine +
                                $"ARG:  0x{address:X8}" +
                                Environment.NewLine +
                                Environment.NewLine;

                        Invoke(new MethodInvoker(delegate
                        {
                            textBoxResult.Text += responseString;
                        }));

                        break;
                    }
                }
            }
        }

        /*private void ScanSmuRange_old(uint start, uint end, int step, byte offset)
        {
            matches = new List<SmuAddressSet>();

            while (start <= end)
            {
                uint smuRspAddress = start + offset;
                uint smuArgAddress = 0xFFFFFFFF;

                if (cpu.ReadDword(start) != 0xFFFFFFFF)
                {
                    // Check if CMD-RSP pair returns correct status, while using a placeholder ARG address
                    if (TrySettings(start, smuRspAddress, smuArgAddress, testMailbox.SMU_MSG_TestMessage, 0x0) == SMU.Status.OK)
                    {
                        // Send smu version command, so the corresponding ARG0 address changes its value
                        TrySettings(start, smuRspAddress, smuArgAddress, testMailbox.SMU_MSG_GetSmuVersion, 0x0);
                        bool match = false;

                        smuArgAddress = smuRspAddress + 4;

                        // Scan for ARG address
                        while ((smuArgAddress <= end) && !match)
                        {
                            // Check if smu version major is in range
                            var currentRegValue = (cpu.ReadDword(smuArgAddress) & 0x00FF0000) >> 16;
                            Console.WriteLine($"REG: 0x{smuArgAddress:X8} Value: 0x{currentRegValue:X8}");
                            if (currentRegValue > 1 && currentRegValue <= 99)
                            {
                                // Send test message with an argument, using the potential ARG0 address
                                var argValue = (uint)matches.Count * 2 + 99;
                                TrySettings(start, smuRspAddress, smuArgAddress, testMailbox.SMU_MSG_TestMessage, argValue);
                                currentRegValue = cpu.ReadDword(smuArgAddress);
                                Console.WriteLine($"REG: 0x{smuArgAddress:X8} Value: 0x{currentRegValue:X8}");

                                // Check the address for expected value (argument + 1)
                                if (currentRegValue == argValue + 1)
                                {
                                    match = true;
                                    matches.Add(new SmuAddressSet(start, smuRspAddress, smuArgAddress));

                                    string responseString =
                                        $"CMD:  0x{start:X8}" +
                                        Environment.NewLine +
                                        $"RSP:  0x{smuRspAddress:X8}" +
                                        Environment.NewLine +
                                        $"ARG:  0x{smuArgAddress:X8}" +
                                        Environment.NewLine +
                                        Environment.NewLine;

                                    smuArgAddress += 20;

                                    Invoke(new MethodInvoker(delegate
                                    {
                                        textBoxResult.Text += responseString;
                                    }));
                                }
                            }

                            smuArgAddress += 0x4;
                        }
                    }
                }

                start += (uint)step;
            }
        }*/

        private void RunBackgroundTask(DoWorkEventHandler task, RunWorkerCompletedEventHandler completedHandler)
        {
            try
            {
                SetButtonsState(false);
                textBoxResult.Clear();

                backgroundWorker1 = new BackgroundWorker();
                backgroundWorker1.DoWork += task;
                backgroundWorker1.RunWorkerCompleted += completedHandler;
                backgroundWorker1.RunWorkerAsync();
            }
            catch (ApplicationException ex)
            {
                SetStatusText(Resources.Error);
                SetButtonsState();
                HandleError(ex.Message);
            }
        }

        private void BackgroundWorkerTrySettings_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetStatusText("正在扫描 SMU 地址，请稍候...");
                }));

                switch (cpu.info.codeName)
                {
                    case Cpu.CodeName.BristolRidge:
                        //ScanSmuRange(0x13000000, 0x13000F00, 4, 0x10);
                        break;
                    case Cpu.CodeName.RavenRidge:
                    case Cpu.CodeName.Picasso:
                    case Cpu.CodeName.FireFlight:
                    case Cpu.CodeName.Dali:
                    case Cpu.CodeName.Renoir:
                        ScanSmuRange(0x03B10500, 0x03B10998, 8, 0x3C);
                        ScanSmuRange(0x03B10A00, 0x03B10AFF, 4, 0x60);
                        break;
                    case Cpu.CodeName.PinnacleRidge:
                    case Cpu.CodeName.SummitRidge:
                    case Cpu.CodeName.Matisse:
                    case Cpu.CodeName.Whitehaven:
                    case Cpu.CodeName.Naples:
                    case Cpu.CodeName.Colfax:
                    case Cpu.CodeName.Vermeer:
                    //case Cpu.CodeName.Raphael:
                        ScanSmuRange(0x03B10500, 0x03B10998, 8, 0x3C);
                        ScanSmuRange(0x03B10500, 0x03B10AFF, 4, 0x4C);
                        break;
                    case Cpu.CodeName.Raphael:
                    case Cpu.CodeName.GraniteRidge:
                        ScanSmuRange(0x03B10500, 0x03B10998, 8, 0x3C);
                        // ScanSmuRange(0x03B10500, 0x03B10AFF, 4, 0x4C);
                        break;
                    case Cpu.CodeName.Rome:
                        ScanSmuRange(0x03B10500, 0x03B10AFF, 4, 0x4C);
                        break;
                    default:
                        break;
                }
            }
            catch (ApplicationException)
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetButtonsState();
                    SetStatusText(Resources.Error);
                }));
            }
        }

        private void ButtonScan_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show(
                "扫描过程可能导致系统崩溃或产生其他意外结果。" +
                Environment.NewLine +
                "视系统和当前负载而定，最长可能需要 1 分钟。" +
                Environment.NewLine +
                "是否继续？",
                "确认扫描",
                MessageBoxButtons.OKCancel
            );

            if (confirmResult == DialogResult.OK)
                RunBackgroundTask(BackgroundWorkerTrySettings_DoWork, SmuScan_WorkerCompleted);
        }

        private void TabControl1_Selected(object sender, TabControlEventArgs e)
        {
            UpdateFixedTabButtons();
            if (e.TabPage == tabPageInfo)
                splitContainer1.Panel2Collapsed = true;
            else if (splitContainer1.Panel2Collapsed)
                splitContainer1.Panel2Collapsed = false;
        }

        public string GenerateReportJson()
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented
            };

            // {
            writer.WriteStartObject();

            writer.WritePropertyName("AppVersion");
            writer.WriteValue(Application.ProductVersion);

            writer.WritePropertyName("OSVersion");
            writer.WriteValue(new ComputerInfo().OSFullName);

            Type type = cpu.systemInfo.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                writer.WritePropertyName(property.Name);
                if (property.Name == "CpuId" || property.Name == "PatchLevel")
                    writer.WriteValue($"{property.GetValue(cpu.systemInfo, null):X8}");
                else if (property.Name == "SmuVersion")
                    writer.WriteValue(cpu.systemInfo.SmuVersionString);
                else
                    writer.WriteValue(property.GetValue(cpu.systemInfo, null));
            }

            // "SmuAddresses:"
            writer.WritePropertyName("Mailboxes");
            writer.WriteStartArray();
            foreach (SmuAddressSet set in matches)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("MsgAddress");
                writer.WriteValue($"0x{set.MsgAddress:X8}");
                writer.WritePropertyName("RspAddress");
                writer.WriteValue($"0x{set.RspAddress:X8}");
                writer.WritePropertyName("ArgAddress");
                writer.WriteValue($"0x{set.ArgAddress:X8}");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            // }
            writer.WriteEndObject();

            sw.Close();

            return sw.ToString();
        }

        private void BackgroundWorkerReport_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string unixTimestamp = Convert.ToString((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMinutes);
            string fileName = $@"SMUDebug_{unixTimestamp}.json";

            if (File.Exists(fileName))
                File.Delete(fileName);

            using (var sw = new StreamWriter(fileName, true))
            {
                sw.WriteLine(GenerateReportJson());
            }

            //ResetSmuAddresses();
            SetButtonsState();
            SetStatusText("报告已生成。");
            MessageBox.Show($"报告已保存为 {fileName}");
        }

        public static void CalculatePstateDetails(uint eax, ref uint IddDiv, ref uint IddVal, ref uint CpuVid, ref uint CpuDfsId, ref uint CpuFid)
        {
            IddDiv = eax >> 30;
            IddVal = eax >> 22 & 0xFF;
            CpuVid = eax >> 14 & 0xFF;
            CpuDfsId = eax >> 8 & 0x3F;
            CpuFid = eax & 0xFF;
        }

        private void ButtonExport_Click(object sender, EventArgs e)
        {
            RunBackgroundTask(BackgroundWorkerTrySettings_DoWork, BackgroundWorkerReport_RunWorkerCompleted);
        }

        private bool nonNumberEntered;

        private void PstateFidDid_KeyDown(object sender, KeyEventArgs e)
        {
            nonNumberEntered = false;

            if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
            {
                if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                {
                    if (e.KeyCode != Keys.Back)
                    {
                        nonNumberEntered = true;
                    }
                }
            }

            if (ModifierKeys == Keys.Shift)
            {
                nonNumberEntered = true;
            }
        }

        private void PstateFidDid_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (nonNumberEntered)
            {
                e.Handled = true;
            }
        }

        private void PstateFidDid_KeyUp(object sender, KeyEventArgs e)
        {
            var fid = string.IsNullOrEmpty(pstateFid.Text) ? 0 : int.Parse(pstateFid.Text);
            var did = string.IsNullOrEmpty(pstateDid.Text) ? 1 : int.Parse(pstateDid.Text);
            pstateFrequency.Text = (fid * 25 / (did * 12.5)) * 100 + "MHz";
        }

        private void BtnPstateRead_Click(object sender, EventArgs e)
        {
            uint eax = default, edx = default;
            var pstateId = pstateIdBox.SelectedIndex;
            if (!cpu.ReadMsr(Convert.ToUInt32(Convert.ToInt64(0xC0010064) + pstateId), ref eax, ref edx))
            {
                SetStatusText($@"读取 PState {pstateId} 时出错！");
                return;
            }

            uint IddDiv = 0x0;
            uint IddVal = 0x0;
            uint CpuVid = 0x0;
            uint CpuDfsId = 0x0;
            uint CpuFid = 0x0;

            CalculatePstateDetails(eax, ref IddDiv, ref IddVal, ref CpuVid, ref CpuDfsId, ref CpuFid);

            pstateDid.Text = Convert.ToString(CpuDfsId, 10);
            pstateFid.Text = Convert.ToString(CpuFid, 10);
            pstateFrequency.Text = (CpuFid * 25 / (CpuDfsId * 12.5)) * 100 + "MHz";

            SetStatusText($@"已成功读取 PState {pstateId}。");

            pstateDid.ReadOnly = false;
            pstateFid.ReadOnly = false;
            btnPstateWrite.Enabled = true;
        }

        private void BtnPstateWrite_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show(
                @"此操作将更改所选 PState 和 CPU 频率。" +
                Environment.NewLine +
                @"设置过高的频率可能导致系统崩溃或硬件损坏。" +
                Environment.NewLine +
                @"是否继续？",
                @"确认更改 PState",
                MessageBoxButtons.OKCancel
            );

            if (confirmResult != DialogResult.OK) return;

            if (string.IsNullOrEmpty(pstateDid.Text) || string.IsNullOrEmpty(pstateFid.Text))
            {
                MessageBox.Show("DID/FID 为空，无法写入！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var pstateId = pstateIdBox.SelectedIndex;
            uint eax = default, edx = default;
            uint IddDiv = 0x0;
            uint IddVal = 0x0;
            uint CpuVid = 0x0;
            uint CpuDfsId = 0x0;
            uint CpuFid = 0x0;

            if (!cpu.ReadMsr(Convert.ToUInt32(Convert.ToInt64(0xC0010064) + pstateId), ref eax, ref edx))
            {
                SetStatusText($@"读取 PState {pstateId} 时出错！");
                return;
            }

            CalculatePstateDetails(eax, ref IddDiv, ref IddVal, ref CpuVid, ref CpuDfsId, ref CpuFid);

            eax = (IddDiv & 0xFF) << 30 | (IddVal & 0xFF) << 22 | (CpuVid & 0xFF) << 14 | (uint.Parse(pstateDid.Text) & 0xFF) << 8 | uint.Parse(pstateFid.Text) & 0xFF;

            if (_numaUtil.HighestNumaNode > 0)
            {
                for (var i = 0; i < (int)_numaUtil.HighestNumaNode; i++)
                {
                    if (!WritePstateClick(pstateId, eax, edx, i)) return;
                }
            }
            else
            {
                if (!WritePstateClick(pstateId, eax, edx)) return;
            }

            SetStatusText($@"已成功写入 PState {pstateId}。");
        }

        // P0 fix C001_0015 HWCR[21]=1
        // Fixes timer issues when not using HPET
        public bool ApplyTscWorkaround()
        {
            uint eax = 0, edx = 0;

            if (cpu.ReadMsr(0xC0010015, ref eax, ref edx))
            {
                eax |= 0x200000;
                return cpu.WriteMsr(0xC0010015, eax, edx);
            }

            SetStatusText(@"应用 TSC 修复时出错！");
            return false;
        }

        private bool WritePstateClick(int pstateId, uint eax, uint edx, int numanode = 0)
        {
            if (_numaUtil.HighestNumaNode > 0) _numaUtil.SetThreadProcessorAffinity((ushort)(numanode + 1), Enumerable.Range(0, Environment.ProcessorCount).ToArray());

            if (!ApplyTscWorkaround()) return false;

            if (!cpu.WriteMsr(Convert.ToUInt32(Convert.ToInt64(0xC0010064) + pstateId), eax, edx))
            {
                SetStatusText($@"写入 PState {pstateId} 时出错！");
                return false;
            }

            return true;
        }

        private void PciScan_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                TryConvertToUint(textBoxPciStartReg.Text, out uint startReg);
                TryConvertToUint(textBoxPciEndReg.Text, out uint endReg);

                if (endReg <= startReg)
                {
                    HandleError("结束寄存器必须大于起始寄存器。");
                    return;
                }

                Invoke(new MethodInvoker(delegate
                {
                    SetStatusText("正在扫描 PCI 地址，请稍候...");
                }));

                string result = "REG         Value(HEX) Value(BIN)" + Environment.NewLine;

                while (startReg <= endReg)
                {
                    var data = cpu.ReadDword(startReg);
                    result += $"0x{startReg:X8}: 0x{data:X8} {Convert.ToString(data, 2).PadLeft(32, '0')}" + Environment.NewLine;
                    startReg += 4;
                }
                    
                ShowResultForm("PCI 扫描结果", result);
            }
            catch (ApplicationException ex)
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetButtonsState();
                    HandleError(ex.Message);
                }));
            }
        }

        private void Scan_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetButtonsState();
            SetStatusText("扫描完成。");
        }

        private void SmuScan_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int index = comboBoxMailboxSelect.SelectedIndex;
            PopulateMailboxesList(comboBoxMailboxSelect.Items);

            for (var i = 0; i < matches.Count; i++)
            {
                AddMailboxToList($"邮箱 {i + 1}", matches[i]);
            }

            if (index > comboBoxMailboxSelect.Items.Count)
                index = 0;

            comboBoxMailboxSelect.SelectedIndex = index;
            SetButtonsState();
            //ResetSmuAddresses();
            SetStatusText("扫描完成。");
        }

        private void ButtonPciScan_Click(object sender, EventArgs e)
        {
            RunBackgroundTask(PciScan_DoWork, Scan_WorkerCompleted);
        }

        private void ButtonApplyAC_Click(object sender, EventArgs e)
        {
            int frequency = (int)(((FrequencyListItem)comboBoxACF.SelectedItem).multi * 100.00);
            ApplyFrequencyAllCoreSetting(frequency);
        }

        private void ButtonApplySC_Click(object sender, EventArgs e)
        {
            ApplyFrequencyAllCoreSetting(550);
            int frequency = (int)(((FrequencyListItem)comboBoxSCF.SelectedItem).multi * 100.00);
            ApplyFrequencySingleCoreSetting((CoreListItem)comboBoxCore.SelectedItem, frequency);
        }

        private void ButtonApplyPROCHOT_Click(object sender, EventArgs e)
        {
            if (checkBoxPROCHOT.Checked)
            {
                DisableOCMode();
            }
            EnableOCMode(checkBoxPROCHOT.Checked);
            if (!checkBoxPROCHOT.Checked && cpu.IsProchotEnabled() == true)
            {
                checkBoxPROCHOT.Checked = true;
                HandleError(@"错误：无法禁用 PROCHOT！");
            }
            /*else
            {
                checkBoxPROCHOT.Enabled = false;
                buttonApplyPROCHOT.Enabled = false;
            }*/
        }

        private void ReadMsr_Task(object sender, DoWorkEventArgs e)
        {
            try
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetStatusText("正在扫描 MSR 范围，请稍候...");
                }));

                string result = "MSR         EDX(63-32) EAX(31-0)" + Environment.NewLine;

                TryConvertToUint(textBoxMsrStart.Text, out uint startReg);
                TryConvertToUint(textBoxMsrEnd.Text, out uint endReg);

                while (startReg <= endReg)
                {
                    uint eax = default, edx = default;
                    if (cpu.ReadMsr(startReg, ref eax, ref edx))
                    {
                        result += $"0x{startReg:X8}: 0x{edx:X8} 0x{eax:X8}" + Environment.NewLine;
                    }

                    startReg += 1;
                }

                ShowResultForm("MSR 扫描结果", result);
            }
            catch (ApplicationException ex)
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetButtonsState();
                    HandleError(ex.Message);
                }));
            }
        }

        private void ButtonMsrRead_Click(object sender, EventArgs e)
        {
            TryConvertToUint(textBoxMsrAddress.Text, out uint msr);
            uint eax = default, edx = default;
            if (cpu.ReadMsr(msr, ref eax, ref edx))
            {
                textBoxMsrEdx.Text = $"0x{edx:X8}";
                textBoxMsrEax.Text = $"0x{eax:X8}";
            }
        }

        private void ButtonMsrWrite_Click(object sender, EventArgs e)
        {
            TryConvertToUint(textBoxMsrEdx.Text, out uint edx);
            TryConvertToUint(textBoxMsrEax.Text, out uint eax);
            TryConvertToUint(textBoxMsrAddress.Text, out uint msr);

            if (!cpu.WriteMsr(msr, eax, edx))
            {
                HandleError($@"写入 MSR {textBoxMsrAddress.Text} 时出错！");
                return;
            }

            SetStatusText("写入成功。");
        }

        private void ButtonMsrScan_Click(object sender, EventArgs e)
        {
            RunBackgroundTask(ReadMsr_Task, Scan_WorkerCompleted);
        }

        private void ReadCPUID_Task(object sender, DoWorkEventArgs e)
        {
            try
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetStatusText("正在扫描 CPUID 范围，请稍候...");
                }));

                string result = "CPUID       EAX        EBX        ECX        EDX" + Environment.NewLine;
                uint LFuncStd = 0, LFuncExt = 0;
                uint eax = 0, ebx = 0, ecx = 0, edx = 0;

                if (cpu.Cpuid(0x00000000, ref eax, ref ebx, ref ecx, ref edx))
                    LFuncStd = eax;

                if (cpu.Cpuid(0x80000000, ref eax, ref ebx, ref ecx, ref edx))
                    LFuncExt = eax - 0x80000000;

                for (uint i = 0; i <= LFuncStd; ++i)
                {
                    var index = 0x00000000 + i;
                    cpu.Cpuid(index, ref eax, ref ebx, ref ecx, ref edx);
                    result += $"0x{index:X8}: 0x{eax:X8} 0x{ebx:X8} 0x{ecx:X8} 0x{edx:X8}" + Environment.NewLine;
                }

                for (uint i = 0; i <= LFuncExt; ++i)
                {
                    var index = 0x80000000 + i;
                    cpu.Cpuid(index, ref eax, ref ebx, ref ecx, ref edx);
                    result += $"0x{index:X8}: 0x{eax:X8} 0x{ebx:X8} 0x{ecx:X8} 0x{edx:X8}" + Environment.NewLine;
                }

                ShowResultForm("CPUID 扫描结果", result);
            }
            catch (ApplicationException ex)
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetButtonsState();
                    HandleError(ex.Message);
                }));
            }
        }

        private void ButtonCPUIDRead_Click(object sender, EventArgs e)
        {
            TryConvertToUint(textBoxCPUIDAddress.Text, out uint index);
            uint eax = 0, ebx = 0, ecx = 0, edx = 0;
            if (cpu.Cpuid(index, ref eax, ref ebx, ref ecx, ref edx))
            {
                textBoxCPUIDeax.Text = $"0x{eax:X8}";
                textBoxCPUIDebx.Text = $"0x{ebx:X8}";
                textBoxCPUIDecx.Text = $"0x{ecx:X8}";
                textBoxCPUIDedx.Text = $"0x{edx:X8}";
            }
        }

        private void ButtonCPUIDScan_Click(object sender, EventArgs e)
        {
            RunBackgroundTask(ReadCPUID_Task, Scan_WorkerCompleted);
        }

        private void ButtonPMTable_Click(object sender, EventArgs e)
        {
            if (cpu.Status == IODriver.LibStatus.OK)
                new Thread(() => new PowerTableMonitor(cpu).ShowDialog()).Start();
            else
                HandleError("I/O 驱动程序未响应或尚未加载。");
        }

        private void ButtonSMUMonitor_Click(object sender, EventArgs e)
        {
            TryConvertToUint(textBoxCMDAddress.Text, out uint addrMsg);
            TryConvertToUint(textBoxRSPAddress.Text, out uint addrRsp);
            TryConvertToUint(textBoxARGAddress.Text, out uint addrArg);

            new Thread(() => new SMUMonitor(cpu, addrMsg, addrArg, addrRsp).ShowDialog()).Start();
        }

        private void ComboBoxMailboxSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            MailboxListItem item = comboBoxMailboxSelect.SelectedItem as MailboxListItem;
            InitTestMailbox(item.msgAddr, item.rspAddr, item.argAddr);
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ExitApplication();
        }

        private void buttonGetCO_Click(object sender, EventArgs e)
        {
            InitPBO();
        }

        private uint EncodeCoreMarginBitmask(int coreIndex, int coresPerCCD = 8)
        {
            if (cpu.smu.SMU_TYPE >= SMU.SmuType.TYPE_APU0 && cpu.smu.SMU_TYPE <= SMU.SmuType.TYPE_APU2)
            {
                return (uint)coreIndex;
            }

            int ccdIndex = Convert.ToInt32(coreIndex / coresPerCCD);
            int localCoreIndex = coreIndex % coresPerCCD;

            int ccdMask = ccdIndex << 8;
            int mask = ccdMask | localCoreIndex;

            return (uint)(mask << 20);
        }

        private void ApplyCO()
        {
            //if (cpu.info.family == Cpu.Family.FAMILY_19H)
            //if (cpu.smu.Rsmu.SMU_MSG_SetDldoPsmMargin != 0)
            {
                for (var i = 0; i < GetPhysicalCoreCount(); i++)
                {
                    if (IsCoreEnabled(i))
                    {
                        NumericUpDown control = GetCOControl(i);
                        if (control != null)
                        {
                            cpu.SetPsmMarginSingleCore(EncodeCoreMarginBitmask(i), Convert.ToInt32(control.Value));
                        }
                    }
                }
            }
            //else
            //{
            //    HandleError("Not supported");
            //}
        }

        private void ButtonApplyCO_Click(object sender, EventArgs e)
        {
            ApplyCO();
            InitPBO();
        }

        private string GetWmiInstanceName()
        {
            try
            {
                instanceName = WMI.GetInstanceName(wmiScope, wmiAMDACPI);
            }
            catch
            {
                // ignored
            }

            return instanceName;
        }

        private void PopulateWmiFunctions()
        {
            try
            {
                instanceName = GetWmiInstanceName();
                classInstance = new ManagementObject(wmiScope,
                    $"{wmiAMDACPI}.InstanceName='{instanceName}'",
                    null);

                // Get function names with their IDs
                string[] functionObjects = { "GetObjectID", "GetObjectID2" };
                var index = 1;

                foreach (var functionObject in functionObjects)
                {
                    try
                    {
                        pack = WMI.InvokeMethodAndGetValue(classInstance, functionObject, "pack", null, 0);

                        if (pack != null)
                        {
                            var ID = (uint[])pack.GetPropertyValue("ID");
                            var IDString = (string[])pack.GetPropertyValue("IDString");
                            var Length = (byte)pack.GetPropertyValue("Length");

                            for (var i = 0; i < Length; ++i)
                            {
                                if (IDString[i] == "")
                                    break;

                                WmiCmdListItem item = new WmiCmdListItem($"{IDString[i] + ": "}{ID[i]:X8}", ID[i], !IDString[i].StartsWith("Get"));
                                comboBoxAvailableCommands.Items.Add(item);
                            }
                        }
                        else
                        {
                            comboBoxAvailableCommands.Items.Add("<获取失败>");
                        }

                        comboBoxAvailableCommands.SelectedIndex = 0;
                    }
                    catch
                    {
                        // ignored
                    }

                    index++;
                }
            }
            catch
            {
                // ignored
            }
        }

        private void ComboBoxAvailableCommands_SelectedIndexChanged(object sender, EventArgs e)
        {
            WmiCmdListItem command = comboBoxAvailableCommands.SelectedItem as WmiCmdListItem;

            comboBoxAvailableValues.Items.Clear();
            comboBoxAvailableValues.Enabled = false;
            textBoxWmiArgument.Text = "";
            textBoxWmiArgument.Enabled = false;

            if (command.isSet) {
                // Get possible values (index) of a memory option in BIOS
                var dvaluesPack = WMI.InvokeMethodAndGetValue(classInstance, "Getdvalues", "pack", "ID", command.value);
                if (dvaluesPack != null)
                {
                    uint[] DValuesBuffer = (uint[])dvaluesPack.GetPropertyValue("DValuesBuffer");
                    Console.WriteLine(command.text);
                    foreach (uint value in DValuesBuffer)
                    {
                        if (value != 0)
                        {
                            WmiCmdListItem item = new WmiCmdListItem(value.ToString(), value);
                            Console.WriteLine(value);
                            comboBoxAvailableValues.Items.Add(item);
                        }
                    }
                    Console.WriteLine("------------------------");

                    if (comboBoxAvailableValues.Items.Count > 0)
                        comboBoxAvailableValues.Enabled = true;
                    else
                        comboBoxAvailableValues.Items.Add("此命令没有可用值");
                }
                textBoxWmiArgument.Enabled = true;
            }
            else
            {
                comboBoxAvailableValues.Items.Add("Get 命令不支持值");
            }

            comboBoxAvailableValues.SelectedIndex = 0;
        }

        private void ComboBoxAvailableValues_SelectedIndexChanged(object sender, EventArgs e)
        {
            WmiCmdListItem command = comboBoxAvailableCommands.SelectedItem as WmiCmdListItem;
            if (command.isSet && comboBoxAvailableValues.Enabled)
                textBoxWmiArgument.Text = comboBoxAvailableValues.Text;
            else
                textBoxWmiArgument.Text = "";
        }

        private void ButtonWmiCmdSend_Click(object sender, EventArgs e)
        {
            WmiCmdListItem command = comboBoxAvailableCommands.SelectedItem as WmiCmdListItem;
            uint value = 0;
            if (command.isSet)
            {
                string text = textBoxWmiArgument.Text;
                //if (text.StartsWith("0x"))
                {
                    //TryConvertToUint(text, out value);
                }
                //else
                {
                    value = uint.Parse(text);
                }
            }

            if (value >= 0 && value < 0x10000)
            {
                var response = WMI.RunCommand(classInstance, command.value, value);
                var text = command.text + Environment.NewLine + "------------------------" + Environment.NewLine;
                foreach (byte b in response)
                {
                    text += "0x" + b.ToString("X2") + Environment.NewLine;
                }
                text += "------------------------" + Environment.NewLine;
                textBoxResult.Text = text + Environment.NewLine + textBoxResult.Text;
            }
        }

        private void ButtonBCLKApply_Click(object sender, EventArgs e)
        {
            double targetBclk = double.Parse(numericUpDownBclk.Text);
            cpu.SetBclk(targetBclk);

            double? currentBclk = cpu.GetBclk();
            labelBCLK.Text = currentBclk + " MHz";
            numericUpDownBclk.Text = $"{currentBclk}";
        }

        private void BulkMarginChangeHandler(int ccd, int step = 1)
        {
            int startCore = ccd * 8;
            int endCore = Math.Min(startCore + 8, GetPhysicalCoreCount());

            for (var i = startCore; i < endCore; ++i)
            {
                NumericUpDown control = GetCOControl(i);
                if (control != null && control.Enabled && IsCoreEnabled(i))
                {
                    decimal newValue = control.Value + step;
                    newValue = Math.Max(control.Minimum, Math.Min(control.Maximum, newValue));
                    control.Value = newValue;
                }
            }
        }

        private void Button_ccd0_inc_Click(object sender, EventArgs e)
        {
            BulkMarginChangeHandler(0, 1);
        }

        private void Button_ccd1_inc_Click(object sender, EventArgs e)
        {
            BulkMarginChangeHandler(1, 1);
        }

        private void Button_ccd0_dec_Click(object sender, EventArgs e)
        {
            BulkMarginChangeHandler(0, -1);
        }

        private void Button_ccd1_dec_Click(object sender, EventArgs e)
        {
            BulkMarginChangeHandler(1, -1);
        }

        private void ButtonCpuidDecode_Click(object sender, EventArgs e)
        {
            TryConvertToUint(textBoxCpuid.Text.Trim(), out uint eax);

            Cpu.CPUInfo info = new Cpu.CPUInfo
            {
                cpuid = eax
            };
            info.family = (Family)(((info.cpuid & 0xf00) >> 8) + ((info.cpuid & 0xff00000) >> 20));
            info.baseModel = (info.cpuid & 0xf0) >> 4;
            info.extModel = (info.cpuid & 0xf0000) >> 16;
            info.model = info.baseModel + info.extModel * 0x10;
            info.stepping = eax & 0xf;

            string responseString =
                Environment.NewLine +
                $"CPUID：0x{info.cpuid:X8}" +
                Environment.NewLine +
                $"系列：{info.family} ({(uint)info.family:X2}h)" +
                Environment.NewLine +
                $"基础型号：0x{info.baseModel:X1}" +
                Environment.NewLine +
                $"扩展型号：0x{info.extModel:X1}" +
                Environment.NewLine +
                $"型号：0x{info.model:X2}" +
                Environment.NewLine +
                $"步进：{info.stepping}" +
                Environment.NewLine +
                Environment.NewLine;

            Invoke(new MethodInvoker(delegate
            {
                textBoxResult.Text += responseString;
            }));
        }

        private void BtnSaveCOProfile_Click(object sender, EventArgs e)
        {
            List<Tuple<int, int>> margins = new List<Tuple<int, int>>();

            if (cpu.smu.Rsmu.SMU_MSG_SetDldoPsmMargin != 0)
            {
                for (var i = 0; i < GetPhysicalCoreCount(); i++)
                {
                    NumericUpDown control = GetCOControl(i);
                    if (control != null && control.Enabled)
                    {
                        margins.Add(new Tuple<int, int>(i, Convert.ToInt32(control.Value)));
                    }
                }
            }

            try
            {
                using (StreamWriter file = new StreamWriter(defaultsPath))
                {
                    foreach (var entry in margins)
                        file.WriteLine("[{0},{1}]", entry.Item1, entry.Item2);

                    file.WriteLine(
                        "fmax={0}",
                        numericUpDownFmax.Value.ToString(
                            CultureInfo.InvariantCulture));

                    textBoxResult.Text =
                        $"配置文件已保存至 {defaultsPath}" +
                        Environment.NewLine +
                        textBoxResult.Text;
                }
            }
            catch (Exception)
            {
                HandleError("无法将配置文件保存到文件！");
            }
        }

        private List<Tuple<int, int>> LoadCOProfile()
        {
            List<Tuple<int, int>> margins = new List<Tuple<int, int>>();
            try
            {
                if (!Directory.Exists(profilesPath))
                {
                    MessageBox.Show("配置文件目录不存在，已为你创建。");
                    Directory.CreateDirectory(profilesPath);
                }

                // load from file if it exists
                if (File.Exists(defaultsPath))
                {
                    var lines = File.ReadAllLines(defaultsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("["))
                        {
                            var values = line.Replace("[", "").Replace("]", "").Replace(" ", "").Split(',');
                            Int32.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index);
                            Int32.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int margin);
                            margins.Add(new Tuple<int, int>(index, margin));
                        }
                        else if (line.StartsWith("fmax="))
                        {
                            var fmaxStr = line.Substring(5);
                            if (decimal.TryParse(fmaxStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fmaxVal))
                                fmaxVal = Math.Max(numericUpDownFmax.Minimum, Math.Min(numericUpDownFmax.Maximum, fmaxVal));
                            else
                                fmaxVal = numericUpDownFmax.Value;
                            // store temporarily in Tag for retrieval in BtnLoadCOProfile_Click
                            numericUpDownFmax.Tag = fmaxVal;
                        }
                    }
                }
                else
                {
                    HandleError("没有已保存的 CO 配置文件。");
                }
            }
            catch (Exception ex)
            {
                HandleError("无法加载已保存的配置文件！");
            }
            
            return margins;
        }

        private void BtnLoadCOProfile_Click(object sender, EventArgs e)
        {
            numericUpDownFmax.Tag = null;
            List<Tuple<int, int>> margins = LoadCOProfile();

            if (margins.Count > 0 && cpu.smu.Rsmu.SMU_MSG_SetDldoPsmMargin != 0)
            {
                for (var i = 0; i < margins.Count; i++)
                {
                    NumericUpDown control = GetCOControl(margins[i].Item1);
                    if (control != null && control.Enabled)
                    {
                        control.Value = margins[i].Item2;
                    }
                }

                if (numericUpDownFmax.Tag is decimal savedFmax)
                    numericUpDownFmax.Value = savedFmax;

                textBoxResult.Text = $"已从 {defaultsPath} 加载 CO 配置文件" + Environment.NewLine + textBoxResult.Text;
            }
        }

        static bool TaskExists(string taskName)
        {
            // Open the task service
            using (TaskService taskService = new TaskService())
            {
                // Attempt to retrieve the task
                Task task = taskService.GetTask(taskName);
                return task != null;
            }
        }

        static bool TaskHasArgument(
            string taskName,
            string argument)
        {
            using (TaskService taskService = new TaskService())
            {
                Task task = taskService.GetTask(taskName);
                if (task == null)
                    return false;

                return task.Definition.Actions
                    .OfType<ExecAction>()
                    .Any(action =>
                        !string.IsNullOrEmpty(action.Arguments) &&
                        action.Arguments.IndexOf(
                            argument,
                            StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        static void AddTaskToScheduler(
            string taskName,
            string executablePath,
            bool applyFmax,
            int delaySeconds = 0)
        {
            // Create a new task service
            using (TaskService taskService = new TaskService())
            {
                // Create a new task definition
                TaskDefinition taskDefinition = taskService.NewTask();

                // Set the task properties
                taskDefinition.RegistrationInfo.Description =
                    applyFmax
                        ? "用户登录时运行 Ryzen SMU Debug Tool，以应用 CO 配置文件和已保存的 FMax。由 RyzenSDT 自动创建。"
                        : "用户登录时运行 Ryzen SMU Debug Tool，以应用 CO 配置文件。由 RyzenSDT 自动创建。";
                taskDefinition.Principal.UserId = WindowsIdentity.GetCurrent().Name;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;

                // Create a trigger that starts the task at logon with a specified delay
                LogonTrigger logonTrigger = new LogonTrigger();
                logonTrigger.Delay = TimeSpan.FromSeconds(delaySeconds); // Set the delay
                taskDefinition.Triggers.Add(logonTrigger);

                // Create an action that runs the specified executable
                string startupArguments = applyFmax
                    ? "--applyprofile --applyfmax"
                    : "--applyprofile";
                ExecAction execAction =
                    new ExecAction(
                        executablePath,
                        startupArguments);
                taskDefinition.Actions.Add(execAction);

                // Register the task in the root folder of the Task Scheduler
                taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);
            }
        }

        static void RemoveTaskFromScheduler(string taskName)
        {
            // Open the task service
            using (TaskService taskService = new TaskService())
            {
                // Delete the task from the Task Scheduler
                taskService.RootFolder.DeleteTask(taskName, false);
            }
        }

        private void SetStartup(bool isChecked = false)
        {
            /*using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (isChecked && key.GetValue("RyzenSDT") == null)
                {
                    key.SetValue("RyzenSDT", Application.ExecutablePath + " --applyprofile");
                }
                else if (!isChecked && key.GetValue("RyzenSDT") != null)
                {
                    key.DeleteValue("RyzenSDT", false);
                }
            }*/

            if (isChecked)
            {
                bool applyFmax =
                    checkBoxApplyFmaxStartup != null &&
                    checkBoxApplyFmaxStartup.Checked;
                AddTaskToScheduler(
                    "RyzenSDT",
                    Application.ExecutablePath,
                    applyFmax,
                    0);
            }
            else if (TaskExists("RyzenSDT"))
            {
                RemoveTaskFromScheduler("RyzenSDT");
            }
        }

        private void CheckBoxApplyCOStartup_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox startup = sender as CheckBox;
            bool enabled =
                startup != null && startup.Checked;
            if (!enabled)
                checkBoxApplyFmaxStartup.Checked = false;
            SetStartup(enabled);
            textBoxResult.Text = "启动设置已保存。" + Environment.NewLine + textBoxResult.Text;
        }

        private void CheckBoxApplyFmaxStartup_Click(
            object sender,
            EventArgs e)
        {
            if (checkBoxApplyFmaxStartup.Checked &&
                !checkBoxApplyCOStartup.Checked)
                checkBoxApplyCOStartup.Checked = true;

            SetStartup(checkBoxApplyCOStartup.Checked);
            textBoxResult.Text =
                (checkBoxApplyFmaxStartup.Checked
                    ? "已启用启动时应用已保存的 FMax。"
                    : "已关闭启动时应用 FMax。") +
                Environment.NewLine +
                textBoxResult.Text;
        }

        private void tableLayoutPanel14_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ButtonApplyCoreMap_Click(object sender, EventArgs e)
        {
            uint ccd0 = 0x8000;
            uint ccd1 = 0x8100;

            for (int i = 0; i < 8; i++)
            {
                CheckBox control = (CheckBox)Controls.Find($"checkBox{i}", true)[0];
                if (control != null && control.Enabled)
                {
                    if (!control.Checked)
                    {
                        int logicalIndex = Convert.ToInt32(control.Tag as string);
                        ccd0 = Utils.SetBits(ccd0, logicalIndex, 1, 1);
                    }
                }
            }

            for (int i = 0; i < 8; i++)
            {
                CheckBox control = (CheckBox)Controls.Find($"checkBox{i + 8}", true)[0];
                if (control != null && control.Enabled)
                {
                    if (!control.Checked)
                    {
                        int logicalIndex = Convert.ToInt32(control.Tag as string);
                        ccd1 = Utils.SetBits(ccd1, logicalIndex, 1, 1);
                    }
                }
            }

            var cmdItem = comboBoxAvailableCommands.Items
                     .OfType<WmiCmdListItem>()
                     .FirstOrDefault(item => item.text.Contains("Software Downcore Config"));

            if (cmdItem != null) {
                WMI.RunCommand(classInstance, cmdItem.value, ccd0);
                WMI.RunCommand(classInstance, cmdItem.value, ccd1);
            }

            cmdItem = comboBoxAvailableCommands.Items
                     .OfType<WmiCmdListItem>()
                     .FirstOrDefault(item => item.text.Contains("Set SMTEn"));

            if (cmdItem != null)
            {
                WMI.RunCommand(classInstance, cmdItem.value, checkBoxSMT.Checked ? 1u : 0);
            }

            ConfirmWindowsRestart();
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            var cmdItem = comboBoxAvailableCommands.Items
                     .OfType<WmiCmdListItem>()
                     .FirstOrDefault(item => item.text.Contains("Software Downcore Config"));

            if (cmdItem != null)
            {
                WMI.RunCommand(classInstance, cmdItem.value, 0x8000);
                WMI.RunCommand(classInstance, cmdItem.value, 0x81FF);
            }

            cmdItem = comboBoxAvailableCommands.Items
                     .OfType<WmiCmdListItem>()
                     .FirstOrDefault(item => item.text.Contains("Set SMTEn"));

            if (cmdItem != null)
            {
                WMI.RunCommand(classInstance, cmdItem.value, 0);
            }

            ConfirmWindowsRestart();
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            var cmdItem = comboBoxAvailableCommands.Items
                     .OfType<WmiCmdListItem>()
                     .FirstOrDefault(item => item.text.Contains("Software Downcore Config"));

            if (cmdItem != null)
            {
                WMI.RunCommand(classInstance, cmdItem.value, 0x8000);
                WMI.RunCommand(classInstance, cmdItem.value, 0x8100);
            }

            cmdItem = comboBoxAvailableCommands.Items
                     .OfType<WmiCmdListItem>()
                     .FirstOrDefault(item => item.text.Contains("Set SMTEn"));

            if (cmdItem != null)
            {
                WMI.RunCommand(classInstance, cmdItem.value, 1);
            }

            ConfirmWindowsRestart();
        }

        private void ConfirmWindowsRestart()
        {
            var result = MessageBox.Show(
                "需要重新启动才能应用更改。是否立即重新启动？",
                "确认重新启动",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Restart Windows
                    Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                catch (Exception ex)
                {
                    HandleError($"重新启动失败：{ex.Message}");
                }
            }
        }

        private void RadioButtonManualCoreControl_CheckedChanged(object sender, EventArgs e)
        {
            bool manual = radioButtonManualCoreControl.Checked == true;
            panelManualCoreControl.Enabled = manual;
            panelX3D.Enabled = !manual;
        }

        private void ButtonApplyFMax_Click(object sender, EventArgs e)
        {
            if (cpu.SetFMax((uint)numericUpDownFmax.Value)) {
                numericUpDownFmax.Value = cpu.GetFMax();
            }
        }

        private void ButtonPCIRangeMonitor_Click(object sender, EventArgs e)
        {
            TryConvertToUint(textBoxPciStartReg.Text, out uint startAddress);
            TryConvertToUint(textBoxPciEndReg.Text, out uint endAddress);

            new Thread(() => new PCIRangeMonitor(cpu, startAddress, endAddress).ShowDialog()).Start();
        }

        private void ButtonDump_Click(object sender, EventArgs e)
        {
            string name = textBoxDumpName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                HandleError("请输入有效的文件名！");
                return;
            }

            if (File.Exists(name))
            {
                var result = MessageBox.Show(
                    $"文件 {name} 已存在。是否覆盖？",
                    "确认覆盖",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                TryConvertToUint(textBoxDumpStartAddress.Text.Trim(), out uint startAddress);
                TryConvertToUint(textBoxDumpEndAddress.Text.Trim(), out uint endAddress);

                SetStatusText(name + "：正在转储内存，请稍候...");
                
                var stopwatch = Stopwatch.StartNew();
                MemoryDumper.Dump32BitAddressSpaceAsBytes(name, startAddress, endAddress);
                stopwatch.Stop();
                
                string elapsedTime = $"{stopwatch.Elapsed.TotalSeconds:F2}";
                SetStatusText(name + $"：转储完成。（{elapsedTime} 秒）");
                MessageBox.Show($"内存已成功转储到文件：{name}\n\n耗时：{elapsedTime} 秒", "转储完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                HandleError("地址格式无效！");
                return;
            }
        }

        private void ButtonRefreshCS_Click(object sender, EventArgs e)
        {
            InitCS(showStatus: true);
        }

        private void ButtonApplyCS_Click(object sender, EventArgs e)
        {
            var errorMessages = new List<string>();

            if (cpu.SetCurveShaperMargin(marginHigh: (int)cs_min_high.Value, marginMedium: (int)cs_min_med.Value, marginLow: (int)cs_min_low.Value, 0) != SMU.Status.OK)
            {
                errorMessages.Add("无法设置频率档位 0（最低）的曲线塑形器裕量。");
            }
            if (cpu.SetCurveShaperMargin(marginHigh: (int)cs_low_high.Value, marginMedium: (int)cs_low_med.Value, marginLow: (int)cs_low_low.Value, 1) != SMU.Status.OK)
            {
                errorMessages.Add("无法设置频率档位 1（低）的曲线塑形器裕量。");
            }
            if (cpu.SetCurveShaperMargin(marginHigh: (int)cs_med_high.Value, marginMedium: (int)cs_med_med.Value, marginLow: (int)cs_med_low.Value, 2) != SMU.Status.OK)
            {
                errorMessages.Add("无法设置频率档位 2（中）的曲线塑形器裕量。");
            }
            if (cpu.SetCurveShaperMargin(marginHigh: (int)cs_high_high.Value, marginMedium: (int)cs_high_med.Value, marginLow: (int)cs_high_low.Value, 3) != SMU.Status.OK)
            {
                errorMessages.Add("无法设置频率档位 3（高）的曲线塑形器裕量。");
            }
            if (cpu.SetCurveShaperMargin(marginHigh: (int)cs_max_high.Value, marginMedium: (int)cs_max_med.Value, marginLow: (int)cs_max_low.Value, 4) != SMU.Status.OK)
            {
                errorMessages.Add("无法设置频率档位 4（最高）的曲线塑形器裕量。");
            }

            if (errorMessages.Count == 0)
            {
                SetStatusText("曲线塑形器裕量已成功应用。");
            }
            else
            {
                textBoxResult.Text = string.Join(Environment.NewLine, errorMessages) + Environment.NewLine + textBoxResult.Text;
                SetStatusText("应用曲线塑形器裕量时发生一个或多个错误。");
            }

            InitCS();
        }
    }
}
