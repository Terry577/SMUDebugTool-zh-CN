using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using AntButton = AntdUI.Button;
using AntPanel = AntdUI.Panel;
using WinButton = System.Windows.Forms.Button;

namespace ZenStatesDebugTool
{
    public partial class SettingsForm
    {
        private static readonly Color ThemeAccentColor =
            Color.FromArgb(22, 119, 255);
        private static readonly Color ThemeAccentHoverColor =
            Color.FromArgb(64, 150, 255);
        private static readonly Color ThemeAccentActiveColor =
            Color.FromArgb(9, 88, 217);
        private static readonly Color ThemeBorderColor =
            Color.FromArgb(214, 220, 229);
        private static readonly Color ModernPageColor =
            Color.FromArgb(244, 247, 251);
        private static readonly Color ModernCardColor = Color.White;
        private static readonly Color ModernTextColor =
            Color.FromArgb(25, 35, 51);
        private static readonly Color ModernMutedColor =
            Color.FromArgb(112, 124, 143);
        private const int ControlCornerRadius = 8;
        private const int NavigationCornerRadius = 10;
        private const int CardCornerRadius = 12;
        private const float StandardInputWidth = 220F;
        private readonly Dictionary<TabPage, AntButton> fixedTabButtons =
            new Dictionary<TabPage, AntButton>();
        private readonly Dictionary<TabPage, Panel> modernPageSurfaces =
            new Dictionary<TabPage, Panel>();
        private Panel modernPageHost;
        private TabPage modernSelectedPage;

        private void BuildModernInterface()
        {
            SuspendLayout();

            BackColor = ModernPageColor;
            ConfigureModernTabControl();
            BuildCpuPage();
            BuildSmuPage();
            BuildPciPage();
            BuildMsrPage();
            BuildCpuidPage();
            BuildPboPage();
            BuildCurveShaperPage();
            BuildAcpiPage();
            BuildPstatesPage();
            BuildInfoPage();

            ConfigureModernSplitArea();

            TableLayoutPanel root = new TableLayoutPanel
            {
                BackColor = ModernPageColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = new Padding(0),
                Name = "modernRoot",
                Padding = new Padding(0),
                RowCount = 3
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

            Control navigation = BuildModernNavigation();
            statusStrip1.BackColor = Color.FromArgb(238, 243, 249);
            statusStrip1.Dock = DockStyle.Fill;
            statusStrip1.Padding = new Padding(14, 0, 14, 0);
            statusStrip1.SizingGrip = false;

            Controls.Clear();
            root.Controls.Add(navigation, 0, 0);
            root.Controls.Add(splitContainer1, 0, 1);
            root.Controls.Add(statusStrip1, 0, 2);
            Controls.Add(root);

            ResumeLayout(true);
        }

        private void ConfigureModernTabControl()
        {
            tabControl1.Visible = false;

            modernPageHost = new Panel
            {
                BackColor = ModernPageColor,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            foreach (TabPage page in tabControl1.TabPages)
            {
                page.AutoScroll = false;
                page.BackColor = ModernPageColor;
                page.Margin = new Padding(0);
                page.Padding = new Padding(0);
                page.UseVisualStyleBackColor = false;
            }
            tabPageCS.Text = "Curve Shaper";
        }

        private Control BuildModernNavigation()
        {
            AntPanel surface = new AntPanel
            {
                Back = Color.White,
                BorderColor = Color.FromArgb(225, 231, 239),
                BorderWidth = 0F,
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 6, 8, 0),
                Padding = new Padding(8, 3, 8, 3),
                Radius = NavigationCornerRadius,
                Shadow = 2,
                ShadowColor = Color.FromArgb(53, 72, 97),
                ShadowOffsetY = 1,
                ShadowOpacity = 0.08F
            };
            TableLayoutPanel navigation = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 10,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            for (int index = 0; index < 10; index++)
                navigation.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 10F));
            navigation.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            fixedTabButtons.Clear();
            AddModernTabButton(navigation, tabPageCPU, "CPU", 0);
            AddModernTabButton(navigation, tabPageSmu, "SMU", 1);
            AddModernTabButton(navigation, tabPagePci, "PCI", 2);
            AddModernTabButton(navigation, tabPageMsr, "MSR", 3);
            AddModernTabButton(navigation, tabPageCPUID, "CPUID", 4);
            AddModernTabButton(navigation, tabPagePbo, "PBO", 5);
            AddModernTabButton(navigation, tabPageCS, "Curve Shaper", 6);
            AddModernTabButton(navigation, tabPageWmi, "AMD ACPI", 7);
            AddModernTabButton(navigation, tabPagePstates, "PStates", 8);
            AddModernTabButton(navigation, tabPageInfo, "系统信息", 9);
            UpdateFixedTabButtons();

            surface.Controls.Add(navigation);
            return surface;
        }

        private void AddModernTabButton(
            TableLayoutPanel navigation,
            TabPage page,
            string text,
            int column)
        {
            AntButton button = new AntButton
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9F),
                Margin = new Padding(3, 2, 3, 2),
                Radius = ControlCornerRadius,
                TabStop = false,
                Tag = page,
                Text = text,
                WaveSize = 0
            };
            button.Click += FixedTabButton_Click;
            navigation.Controls.Add(button, column, 0);
            fixedTabButtons[page] = button;
        }

        private void FixedTabButton_Click(object sender, EventArgs e)
        {
            AntButton button = sender as AntButton;
            TabPage page = button == null ? null : button.Tag as TabPage;
            if (page != null)
                SelectModernPage(page);
        }

        private void SelectModernPage(TabPage page)
        {
            if (page == null || modernPageHost == null)
                return;

            Panel previousSurface;
            if (modernSelectedPage != null &&
                modernPageSurfaces.TryGetValue(
                    modernSelectedPage,
                    out previousSurface))
            {
                previousSurface.Visible = false;
            }

            modernSelectedPage = page;
            Panel selectedSurface;
            if (modernPageSurfaces.TryGetValue(
                page,
                out selectedSurface))
            {
                selectedSurface.Visible = true;
                selectedSurface.BringToFront();
            }

            splitContainer1.Panel2Collapsed = page == tabPageInfo;
            UpdateFixedTabButtons();
        }

        private void UpdateFixedTabButtons()
        {
            foreach (KeyValuePair<TabPage, AntButton> pair in fixedTabButtons)
            {
                bool selected = pair.Key == modernSelectedPage;
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
            ModernUi.UpgradeDataControls(
                this,
                new HashSet<TextBox> { textBoxResult },
                toolTip1,
                ThemeAccentColor,
                ThemeBorderColor);
        }

        private void ConfigureModernSplitArea()
        {
            splitContainer1.BackColor = ModernPageColor;
            splitContainer1.BorderStyle = BorderStyle.None;
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Margin = new Padding(0);
            splitContainer1.Panel1.BackColor = ModernPageColor;
            splitContainer1.Panel1.Padding = new Padding(14, 12, 6, 12);
            splitContainer1.Panel1MinSize = 690;
            splitContainer1.Panel2.BackColor = ModernPageColor;
            splitContainer1.Panel2.Padding = new Padding(6, 12, 14, 12);
            splitContainer1.Panel2MinSize = 250;
            splitContainer1.SplitterWidth = 8;
            splitContainer1.SplitterDistance = 718;

            splitContainer1.Panel1.Controls.Clear();
            BuildModernPageSurfaces();
            splitContainer1.Panel1.Controls.Add(modernPageHost);
            splitContainer1.Panel2.Controls.Clear();
            splitContainer1.Panel2.Controls.Add(BuildModernOutputCard());
            SelectModernPage(tabPageCPU);
        }

        private void BuildModernPageSurfaces()
        {
            modernPageHost.Controls.Clear();
            modernPageSurfaces.Clear();

            foreach (TabPage page in tabControl1.TabPages)
            {
                Panel surface = new Panel
                {
                    BackColor = ModernPageColor,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    Visible = false
                };

                while (page.Controls.Count > 0)
                {
                    Control content = page.Controls[0];
                    page.Controls.RemoveAt(0);
                    surface.Controls.Add(content);
                }

                modernPageHost.Controls.Add(surface);
                modernPageSurfaces[page] = surface;
            }
        }

        private Control BuildModernOutputCard()
        {
            TableLayoutPanel sidebar = new TableLayoutPanel
            {
                BackColor = ModernPageColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 3
            };
            sidebar.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            sidebar.RowStyles.Add(
                new RowStyle(SizeType.Percent, 55F));
            sidebar.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 10F));
            sidebar.RowStyles.Add(
                new RowStyle(SizeType.Percent, 45F));
            sidebar.Controls.Add(BuildModernLogCard(), 0, 0);
            sidebar.Controls.Add(BuildModernAboutCard(), 0, 2);
            return sidebar;
        }

        private Control BuildModernLogCard()
        {
            tableLayoutPanel11.SuspendLayout();
            tableLayoutPanel11.Controls.Clear();
            tableLayoutPanel11.ColumnStyles.Clear();
            tableLayoutPanel11.RowStyles.Clear();
            tableLayoutPanel11.BackColor = Color.White;
            tableLayoutPanel11.ColumnCount = 1;
            tableLayoutPanel11.Dock = DockStyle.Fill;
            tableLayoutPanel11.Margin = new Padding(0);
            tableLayoutPanel11.Padding = new Padding(0);
            tableLayoutPanel11.RowCount = 3;
            tableLayoutPanel11.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel11.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 56F));
            tableLayoutPanel11.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel11.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 46F));

            TableLayoutPanel heading = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(14, 8, 10, 4),
                RowCount = 2
            };
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            heading.Controls.Add(CreateTextLabel(
                "运行日志",
                10.5F,
                FontStyle.Bold,
                ModernTextColor), 0, 0);
            heading.Controls.Add(CreateTextLabel(
                "命令结果与硬件状态",
                8.5F,
                FontStyle.Regular,
                ModernMutedColor), 0, 1);

            textBoxResult.BackColor = Color.White;
            textBoxResult.BorderStyle = BorderStyle.None;
            textBoxResult.Dock = DockStyle.Fill;
            textBoxResult.Font = new Font(
                "Microsoft YaHei UI",
                9F,
                FontStyle.Regular);
            textBoxResult.Margin = new Padding(14, 6, 8, 6);

            TableLayoutPanel actions = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(10, 6, 10, 8),
                RowCount = 1
            };
            actions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            actions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 72F));
            actions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 72F));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AntButton clear = CreateSmallAntButton("清空");
            clear.Click += delegate { textBoxResult.Clear(); };
            AntButton copy = CreateSmallAntButton("复制");
            copy.Click += delegate
            {
                if (!string.IsNullOrEmpty(textBoxResult.Text))
                    Clipboard.SetText(textBoxResult.Text);
            };
            actions.Controls.Add(clear, 1, 0);
            actions.Controls.Add(copy, 2, 0);

            tableLayoutPanel11.Controls.Add(heading, 0, 0);
            tableLayoutPanel11.Controls.Add(textBoxResult, 0, 1);
            tableLayoutPanel11.Controls.Add(actions, 0, 2);
            tableLayoutPanel11.ResumeLayout(true);

            return CreateCardSurface(tableLayoutPanel11);
        }

        private Control BuildModernAboutCard()
        {
            TableLayoutPanel about = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(14, 9, 10, 10),
                RowCount = 8
            };
            about.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 26F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 22F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 8F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 25F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 25F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 25F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            about.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42F));

            about.Controls.Add(CreateTextLabel(
                "关于与开源",
                10.5F,
                FontStyle.Bold,
                ModernTextColor), 0, 0);
            about.Controls.Add(CreateTextLabel(
                $"SMUDebugTool zh-CN · v{Application.ProductVersion}",
                8.5F,
                FontStyle.Regular,
                ModernMutedColor), 0, 1);
            about.Controls.Add(CreateAboutInfoRow(
                "汉化维护",
                "Terry577"), 0, 3);
            about.Controls.Add(CreateAboutInfoRow(
                "原项目",
                "irusanov / SMUDebugTool"), 0, 4);
            about.Controls.Add(CreateAboutInfoRow(
                "开源许可",
                "GNU GPL v3.0"), 0, 5);

            Label note = CreateTextLabel(
                "简体中文开源修改版\n感谢原作者与所有贡献者",
                8.5F,
                FontStyle.Regular,
                ModernMutedColor);
            note.Padding = new Padding(0, 5, 0, 2);
            about.Controls.Add(note, 0, 6);

            AntButton project = new AntButton
            {
                BackActive = ThemeAccentActiveColor,
                BackColor = ThemeAccentColor,
                BackHover = ThemeAccentHoverColor,
                BorderWidth = 0F,
                Dock = DockStyle.Fill,
                ForeActive = Color.White,
                ForeColor = Color.White,
                ForeHover = Color.White,
                Margin = new Padding(0, 4, 0, 0),
                Radius = ControlCornerRadius,
                Text = "GitHub · 查看项目",
                Type = AntdUI.TTypeMini.Primary,
                WaveSize = 0
            };
            project.Click += delegate
            {
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(
                            "https://github.com/Terry577/SMUDebugTool-zh-CN")
                        {
                            UseShellExecute = true
                        });
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        "无法打开项目页面。\n\n" + exception.Message,
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };
            about.Controls.Add(project, 0, 7);

            return CreateCardSurface(about);
        }

        private Control CreateAboutInfoRow(
            string label,
            string value)
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            row.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 68F));
            row.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            row.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            row.Controls.Add(CreateTextLabel(
                label,
                8.5F,
                FontStyle.Regular,
                ModernMutedColor), 0, 0);
            row.Controls.Add(CreateTextLabel(
                value,
                8.5F,
                FontStyle.Regular,
                ModernTextColor), 1, 0);
            return row;
        }

        private AntButton CreateSmallAntButton(string text)
        {
            return new AntButton
            {
                DefaultBack = Color.White,
                DefaultBorderColor = ThemeBorderColor,
                BorderWidth = 1F,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 0, 0),
                Radius = ControlCornerRadius,
                Text = text,
                Type = AntdUI.TTypeMini.Default,
                WaveSize = 0
            };
        }

        private AntPanel CreateCardSurface(Control content)
        {
            AntPanel card = new AntPanel
            {
                Back = ModernCardColor,
                BorderColor = Color.FromArgb(224, 230, 238),
                BorderWidth = 1F,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(1),
                Radius = CardCornerRadius,
                Shadow = 3,
                ShadowColor = Color.FromArgb(55, 73, 98),
                ShadowOffsetY = 2,
                ShadowOpacity = 0.08F
            };
            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(0);
            content.SizeChanged += delegate
            {
                ApplyRoundedContentClip(
                    content,
                    CardCornerRadius - 1);
            };
            card.Controls.Add(content);
            ApplyRoundedContentClip(
                content,
                CardCornerRadius - 1);
            return card;
        }

        private static void ApplyRoundedContentClip(
            Control control,
            int logicalRadius)
        {
            if (control.Width <= 1 || control.Height <= 1)
                return;

            int radius = Math.Max(
                1,
                (int)Math.Round(
                    logicalRadius * control.DeviceDpi / 96F));
            int diameter = radius * 2;
            Rectangle bounds = new Rectangle(
                0,
                0,
                control.Width,
                control.Height);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(
                    bounds.Left,
                    bounds.Top,
                    diameter,
                    diameter,
                    180F,
                    90F);
                path.AddArc(
                    bounds.Right - diameter,
                    bounds.Top,
                    diameter,
                    diameter,
                    270F,
                    90F);
                path.AddArc(
                    bounds.Right - diameter,
                    bounds.Bottom - diameter,
                    diameter,
                    diameter,
                    0F,
                    90F);
                path.AddArc(
                    bounds.Left,
                    bounds.Bottom - diameter,
                    diameter,
                    diameter,
                    90F,
                    90F);
                path.CloseFigure();

                Region oldRegion = control.Region;
                control.Region = new Region(path);
                oldRegion?.Dispose();
            }
        }

        private AntPanel CreateSectionCard(
            string title,
            string description,
            Control content)
        {
            TableLayoutPanel host = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(14, 8, 14, 12),
                RowCount = 2
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel heading = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 2
            };
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            heading.Controls.Add(CreateTextLabel(
                title,
                10.5F,
                FontStyle.Bold,
                ModernTextColor), 0, 0);
            heading.Controls.Add(CreateTextLabel(
                description,
                8.5F,
                FontStyle.Regular,
                ModernMutedColor), 0, 1);

            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(0);
            host.Controls.Add(heading, 0, 0);
            host.Controls.Add(content, 0, 1);
            return CreateCardSurface(host);
        }

        private TableLayoutPanel CreatePageBody(
            TabPage page,
            string title,
            string description,
            params Tuple<Control, float>[] sections)
        {
            page.Controls.Clear();

            TableLayoutPanel frame = new TableLayoutPanel
            {
                BackColor = ModernPageColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 2
            };
            frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel heading = new TableLayoutPanel
            {
                BackColor = ModernPageColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(4, 2, 0, 4),
                RowCount = 2
            };
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            heading.Controls.Add(CreateTextLabel(
                title,
                16F,
                FontStyle.Bold,
                ModernTextColor), 0, 0);
            heading.Controls.Add(CreateTextLabel(
                description,
                9F,
                FontStyle.Regular,
                ModernMutedColor), 0, 1);

            TableLayoutPanel body = new TableLayoutPanel
            {
                AutoScroll = true,
                BackColor = ModernPageColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(4, 0, 6, 3),
                RowCount = sections.Length + 1
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int index = 0; index < sections.Length; index++)
            {
                body.RowStyles.Add(new RowStyle(
                    SizeType.Absolute,
                    sections[index].Item2 + 10F));
                sections[index].Item1.Dock = DockStyle.Fill;
                sections[index].Item1.Margin = new Padding(0, 0, 0, 10);
                body.Controls.Add(sections[index].Item1, 0, index);
            }
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            frame.Controls.Add(heading, 0, 0);
            frame.Controls.Add(body, 0, 1);
            page.Controls.Add(frame);
            return body;
        }

        private Label CreateTextLabel(
            string text,
            float size,
            FontStyle style,
            Color color)
        {
            return new Label
            {
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", size, style),
                ForeColor = color,
                Margin = new Padding(0),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(55, 66, 82),
                Margin = new Padding(0, 0, 8, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private void PrepareInput(Control control)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(3, 4, 3, 4);
        }

        private void PrepareAction(Control control)
        {
            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            control.Height = 28;
            control.Margin = new Padding(5, 4, 0, 4);
            control.MinimumSize = new Size(72, 28);
            control.MaximumSize = new Size(0, 28);

            Button button = control as Button;
            if (button != null)
                button.AutoSize = false;
        }

        private void DetachControl(Control control)
        {
            if (control == null || control.Parent == null)
                return;

            Control previousParent = control.Parent;
            previousParent.SuspendLayout();
            previousParent.Controls.Remove(control);
            if (!(previousParent is TableLayoutPanel))
                previousParent.ResumeLayout(false);
        }

        private TableLayoutPanel CreateStandardGrid(
            int rowCount,
            float labelWidth,
            float auxiliaryWidth,
            float actionWidth)
        {
            TableLayoutPanel grid = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 5,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = rowCount
            };
            grid.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, labelWidth));
            grid.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, StandardInputWidth));
            grid.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, auxiliaryWidth));
            grid.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, actionWidth));
            grid.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < rowCount; row++)
                grid.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100F / rowCount));
            return grid;
        }

        private void AddGridRow(
            TableLayoutPanel grid,
            int row,
            string label,
            Control input,
            Control auxiliary,
            Control action)
        {
            grid.SuspendLayout();
            grid.Controls.Add(CreateFieldLabel(label), 0, row);
            if (input != null)
            {
                DetachControl(input);
                PrepareInput(input);
                grid.Controls.Add(input, 1, row);
            }
            if (auxiliary != null)
            {
                DetachControl(auxiliary);
                PrepareInput(auxiliary);
                grid.Controls.Add(auxiliary, 2, row);
            }
            if (action != null)
            {
                DetachControl(action);
                PrepareAction(action);
                grid.Controls.Add(action, 3, row);
            }
            grid.ResumeLayout(true);
        }

        private void BuildCpuPage()
        {
            TableLayoutPanel tuning = CreateStandardGrid(3, 108F, 116F, 84F);
            AddGridRow(
                tuning,
                0,
                "全核频率",
                comboBoxACF,
                null,
                buttonApplyAC);
            AddGridRow(
                tuning,
                1,
                "单核频率",
                comboBoxSCF,
                comboBoxCore,
                buttonApplySC);

            checkBoxPROCHOT.Text = "启用 PROCHOT";
            DetachControl(checkBoxPROCHOT);
            PrepareInput(checkBoxPROCHOT);
            tuning.Controls.Add(CreateFieldLabel("温控保护"), 0, 2);
            tuning.Controls.Add(checkBoxPROCHOT, 1, 2);
            tuning.SetColumnSpan(checkBoxPROCHOT, 2);
            DetachControl(buttonApplyPROCHOT);
            PrepareAction(buttonApplyPROCHOT);
            tuning.Controls.Add(buttonApplyPROCHOT, 3, 2);

            AntPanel tuningCard = CreateSectionCard(
                "频率与温控",
                "集中调整处理器频率和温度保护策略",
                tuning);

            TableLayoutPanel coreContent = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 2
            };
            coreContent.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            coreContent.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 48F));
            coreContent.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel modeRow = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 4,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 1
            };
            modeRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 150F));
            modeRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 130F));
            modeRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            modeRow.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 164F));
            modeRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            radioButtonX3D.Text = "X3D 加速模式";
            radioButtonManualCoreControl.Text = "手动核心控制";
            DetachControl(radioButtonX3D);
            DetachControl(radioButtonManualCoreControl);
            PrepareInput(radioButtonX3D);
            PrepareInput(radioButtonManualCoreControl);

            panelX3D.Controls.Clear();
            panelX3D.BackColor = Color.White;
            panelX3D.Dock = DockStyle.Fill;
            panelX3D.Margin = new Padding(0);
            TableLayoutPanel x3dActions = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 1
            };
            x3dActions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50F));
            x3dActions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50F));
            x3dActions.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            PrepareAction(button5);
            PrepareAction(button6);
            x3dActions.Controls.Add(button5, 0, 0);
            x3dActions.Controls.Add(button6, 1, 0);
            panelX3D.Controls.Add(x3dActions);

            modeRow.Controls.Add(radioButtonX3D, 0, 0);
            modeRow.Controls.Add(radioButtonManualCoreControl, 1, 0);
            modeRow.Controls.Add(panelX3D, 3, 0);

            ConfigureModernCoreGrid();
            coreContent.Controls.Add(modeRow, 0, 0);
            coreContent.Controls.Add(panelManualCoreControl, 0, 1);

            AntPanel coreCard = CreateSectionCard(
                "核心控制",
                "选择工作模式，并按需启用或停用核心与 SMT",
                coreContent);

            CreatePageBody(
                tabPageCPU,
                "CPU 调校",
                "频率、温控与核心拓扑集中管理",
                Tuple.Create<Control, float>(tuningCard, 176F),
                Tuple.Create<Control, float>(coreCard, 230F));
        }

        private void ConfigureModernCoreGrid()
        {
            panelManualCoreControl.Controls.Clear();
            panelManualCoreControl.BackColor = Color.White;
            panelManualCoreControl.Dock = DockStyle.Fill;
            panelManualCoreControl.Margin = new Padding(0);

            tableLayoutPanel15.ColumnStyles.Clear();
            tableLayoutPanel15.RowStyles.Clear();
            tableLayoutPanel15.AutoSize = false;
            tableLayoutPanel15.BackColor = Color.White;
            tableLayoutPanel15.ColumnCount = 8;
            tableLayoutPanel15.Dock = DockStyle.Fill;
            tableLayoutPanel15.Margin = new Padding(0);
            tableLayoutPanel15.Padding = new Padding(0, 4, 14, 0);
            tableLayoutPanel15.RowCount = 5;
            for (int column = 0; column < 8; column++)
                tableLayoutPanel15.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 12.5F));
            tableLayoutPanel15.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 22F));
            tableLayoutPanel15.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 18F));
            tableLayoutPanel15.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 4F));
            tableLayoutPanel15.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 22F));
            tableLayoutPanel15.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 18F));

            TableLayoutPanel manual = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 2
            };
            manual.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            manual.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 128F));
            manual.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            manual.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32F));

            TableLayoutPanel side = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(12, 4, 0, 10),
                RowCount = 3
            };
            side.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            checkBoxSMT.Text = "启用 SMT";
            PrepareInput(checkBoxSMT);
            PrepareAction(buttonApplyCoreMap);
            side.Controls.Add(checkBoxSMT, 0, 0);
            side.Controls.Add(buttonApplyCoreMap, 0, 1);

            label67.Dock = DockStyle.Fill;
            label67.ForeColor = ModernMutedColor;
            label67.Margin = new Padding(0, 4, 0, 0);
            label67.Text = "取消勾选要停用的核心，然后点击“应用”。";
            label67.TextAlign = ContentAlignment.MiddleLeft;

            manual.Controls.Add(tableLayoutPanel15, 0, 0);
            manual.Controls.Add(side, 1, 0);
            manual.Controls.Add(label67, 0, 1);
            manual.SetColumnSpan(label67, 2);
            panelManualCoreControl.Controls.Add(manual);
        }

        private void BuildSmuPage()
        {
            TableLayoutPanel content = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 2
            };
            content.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 52F));

            TableLayoutPanel fields = CreateStandardGrid(6, 112F, 0F, 0F);
            AddGridRow(fields, 0, "邮箱", comboBoxMailboxSelect, null, null);
            AddGridRow(fields, 1, "CMD 地址", textBoxCMDAddress, null, null);
            AddGridRow(fields, 2, "RSP 地址", textBoxRSPAddress, null, null);
            AddGridRow(fields, 3, "ARG 地址", textBoxARGAddress, null, null);
            AddGridRow(fields, 4, "命令 ID", textBoxCMD, null, null);
            AddGridRow(fields, 5, "参数", textBoxARG0, null, null);

            TableLayoutPanel actions = CreateActionStrip(
                buttonPMTable,
                buttonSmuLog,
                buttonProbe,
                buttonApply,
                buttonDefaults);
            content.Controls.Add(fields, 0, 0);
            content.Controls.Add(actions, 0, 1);

            AntPanel card = CreateSectionCard(
                "SMU 邮箱",
                "配置地址并直接发送 SMU 命令",
                content);
            CreatePageBody(
                tabPageSmu,
                "SMU 控制台",
                "底层邮箱访问、监视和诊断工具",
                Tuple.Create<Control, float>(card, 340F));
        }

        private TableLayoutPanel CreateActionStrip(params Control[] actions)
        {
            TableLayoutPanel strip = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = actions.Length + 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0, 5, 0, 0),
                RowCount = 1
            };
            for (int index = 0; index < actions.Length; index++)
                strip.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Absolute, 80F));
            strip.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            strip.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            for (int index = 0; index < actions.Length; index++)
            {
                DetachControl(actions[index]);
                PrepareAction(actions[index]);
                actions[index].Margin = new Padding(
                    index == 0 ? 0 : 5,
                    4,
                    0,
                    4);
                strip.Controls.Add(actions[index], index, 0);
            }
            return strip;
        }

        private void BuildPciPage()
        {
            TableLayoutPanel access = CreateStandardGrid(2, 108F, 0F, 84F);
            AddGridRow(
                access,
                0,
                "PCI 寄存器",
                textBoxPciAddress,
                null,
                buttonPciRead);
            AddGridRow(
                access,
                1,
                "写入值",
                textBoxPciValue,
                null,
                buttonPciWrite);
            AntPanel accessCard = CreateSectionCard(
                "寄存器访问",
                "读取或写入单个 PCI 配置寄存器",
                access);

            TableLayoutPanel tools = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 2
            };
            tools.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tools.RowStyles.Add(
                new RowStyle(SizeType.Percent, 44F));
            tools.RowStyles.Add(
                new RowStyle(SizeType.Percent, 56F));

            TableLayoutPanel scan = CreateStandardGrid(2, 108F, 0F, 84F);
            AddGridRow(
                scan,
                0,
                "起始寄存器",
                textBoxPciStartReg,
                null,
                buttonPciScan);
            AddGridRow(
                scan,
                1,
                "结束寄存器",
                textBoxPciEndReg,
                null,
                ButtonPCIRangeMonitor);

            TableLayoutPanel dump = CreateStandardGrid(3, 108F, 0F, 84F);
            AddGridRow(
                dump,
                0,
                "起始地址",
                textBoxDumpStartAddress,
                null,
                null);
            AddGridRow(
                dump,
                1,
                "结束地址",
                textBoxDumpEndAddress,
                null,
                null);
            AddGridRow(
                dump,
                2,
                "转储文件",
                textBoxDumpName,
                null,
                buttonDump);
            tools.Controls.Add(scan, 0, 0);
            tools.Controls.Add(dump, 0, 1);

            AntPanel toolsCard = CreateSectionCard(
                "范围与转储",
                "扫描寄存器范围，或将指定内存区域保存为文件",
                tools);
            CreatePageBody(
                tabPagePci,
                "PCI 工具",
                "配置空间访问、范围监视与内存转储",
                Tuple.Create<Control, float>(accessCard, 145F),
                Tuple.Create<Control, float>(toolsCard, 250F));
        }

        private void BuildMsrPage()
        {
            TableLayoutPanel access = CreateStandardGrid(3, 110F, 0F, 84F);
            AddGridRow(
                access,
                0,
                "MSR 地址",
                textBoxMsrAddress,
                null,
                buttonMsrRead);
            AddGridRow(
                access,
                1,
                "EAX (31-0)",
                textBoxMsrEax,
                null,
                buttonMsrWrite);
            AddGridRow(
                access,
                2,
                "EDX (63-32)",
                textBoxMsrEdx,
                null,
                null);
            AntPanel accessCard = CreateSectionCard(
                "寄存器访问",
                "读取或写入模型特定寄存器",
                access);

            TableLayoutPanel scan = CreateStandardGrid(2, 110F, 0F, 84F);
            AddGridRow(
                scan,
                0,
                "起始地址",
                textBoxMsrStart,
                null,
                buttonMsrScan);
            AddGridRow(
                scan,
                1,
                "结束地址",
                textBoxMsrEnd,
                null,
                null);
            AntPanel scanCard = CreateSectionCard(
                "范围扫描",
                "批量读取指定范围内的 MSR",
                scan);

            CreatePageBody(
                tabPageMsr,
                "MSR 工具",
                "模型特定寄存器读写与范围分析",
                Tuple.Create<Control, float>(accessCard, 175F),
                Tuple.Create<Control, float>(scanCard, 145F));
        }

        private void BuildCpuidPage()
        {
            TableLayoutPanel query = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 2
            };
            query.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            query.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42F));
            query.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel address = CreateStandardGrid(1, 108F, 84F, 84F);
            AddGridRow(
                address,
                0,
                "CPUID 叶",
                textBoxCPUIDAddress,
                buttonCPUIDScan,
                buttonCPUIDRead);

            TableLayoutPanel registers = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0),
                RowCount = 4
            };
            registers.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 70F));
            registers.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 280F));
            registers.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < 4; row++)
                registers.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 25F));
            AddRegisterField(registers, "EAX", textBoxCPUIDeax, 0, 0);
            AddRegisterField(registers, "EBX", textBoxCPUIDebx, 0, 1);
            AddRegisterField(registers, "ECX", textBoxCPUIDecx, 0, 2);
            AddRegisterField(registers, "EDX", textBoxCPUIDedx, 0, 3);
            query.Controls.Add(address, 0, 0);
            query.Controls.Add(registers, 0, 1);

            AntPanel queryCard = CreateSectionCard(
                "CPUID 查询",
                "读取指定 CPUID 叶并查看通用寄存器结果",
                query);

            TableLayoutPanel decoder = CreateStandardGrid(1, 108F, 0F, 84F);
            AddGridRow(
                decoder,
                0,
                "签名值",
                textBoxCpuid,
                null,
                buttonCpuidDecode);
            AntPanel decoderCard = CreateSectionCard(
                "签名解析",
                "将原始 CPUID 签名解析为可读信息",
                decoder);

            CreatePageBody(
                tabPageCPUID,
                "CPUID 浏览器",
                "查询处理器能力叶并解析型号签名",
                Tuple.Create<Control, float>(queryCard, 280F),
                Tuple.Create<Control, float>(decoderCard, 115F));
        }

        private void AddRegisterField(
            TableLayoutPanel grid,
            string name,
            Control input,
            int column,
            int row)
        {
            grid.Controls.Add(CreateFieldLabel(name), column, row);
            DetachControl(input);
            PrepareInput(input);
            grid.Controls.Add(input, column + 1, row);
        }

        private void BuildPboPage()
        {
            tableLayoutPanel12.BackColor = Color.White;
            tableLayoutPanel12.BorderStyle = BorderStyle.None;
            tableLayoutPanel12.Dock = DockStyle.Fill;
            tableLayoutPanel12.Margin = new Padding(0);

            AntPanel card = CreateSectionCard(
                "Curve Optimizer",
                "按核心设置曲线偏移，并管理配置文件与 FMax",
                tableLayoutPanel12);
            CreatePageBody(
                tabPagePbo,
                "PBO 调校",
                "Curve Optimizer、启动配置与最大频率控制",
                Tuple.Create<Control, float>(card, 410F));
        }

        private void BuildCurveShaperPage()
        {
            NumericUpDown[,] values =
            {
                { cs_min_low, cs_min_med, cs_min_high },
                { cs_low_low, cs_low_med, cs_low_high },
                { cs_med_low, cs_med_med, cs_med_high },
                { cs_high_low, cs_high_med, cs_high_high },
                { cs_max_low, cs_max_med, cs_max_high }
            };
            Label[] columnLabels = { label31, label32, label33 };
            Label[] rowLabels =
            {
                label34,
                label35,
                label36,
                label37,
                label38
            };
            string[] columnTexts = { "低温", "中温", "高温" };
            string[] rowTexts = { "最低", "低", "中", "高", "最高" };

            tableLayoutPanel16.SuspendLayout();
            tableLayoutPanel16.Controls.Clear();
            tableLayoutPanel16.BackColor = Color.White;
            tableLayoutPanel16.ColumnStyles.Clear();
            tableLayoutPanel16.RowStyles.Clear();
            tableLayoutPanel16.ColumnCount = 5;
            tableLayoutPanel16.Dock = DockStyle.Fill;
            tableLayoutPanel16.Margin = new Padding(0);
            tableLayoutPanel16.Padding = new Padding(0, 2, 0, 0);
            tableLayoutPanel16.RowCount = 7;
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 72F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 118F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 118F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 118F));
            tableLayoutPanel16.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel16.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30F));
            for (int row = 1; row < 6; row++)
                tableLayoutPanel16.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel16.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));

            for (int column = 0; column < 3; column++)
            {
                columnLabels[column].Dock = DockStyle.Fill;
                columnLabels[column].Margin = new Padding(3, 0, 3, 0);
                columnLabels[column].Text = columnTexts[column];
                columnLabels[column].TextAlign =
                    ContentAlignment.MiddleCenter;
                tableLayoutPanel16.Controls.Add(
                    columnLabels[column],
                    column + 1,
                    0);
            }
            for (int row = 0; row < 5; row++)
            {
                rowLabels[row].Dock = DockStyle.Fill;
                rowLabels[row].Margin = new Padding(0);
                rowLabels[row].Text = rowTexts[row];
                rowLabels[row].TextAlign = ContentAlignment.MiddleLeft;
                tableLayoutPanel16.Controls.Add(rowLabels[row], 0, row + 1);
                for (int column = 0; column < 3; column++)
                {
                    PrepareInput(values[row, column]);
                    tableLayoutPanel16.Controls.Add(
                        values[row, column],
                        column + 1,
                        row + 1);
                }
            }
            tableLayoutPanel16.ResumeLayout(true);

            TableLayoutPanel actions = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 1
            };
            actions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            actions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 80F));
            actions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 80F));
            actions.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            PrepareAction(buttonRefreshCS);
            PrepareAction(buttonApplyCS);
            buttonRefreshCS.Margin = new Padding(0, 4, 4, 4);
            buttonApplyCS.Margin = new Padding(4, 4, 0, 4);
            actions.Controls.Add(buttonRefreshCS, 1, 0);
            actions.Controls.Add(buttonApplyCS, 2, 0);

            TableLayoutPanel content = new TableLayoutPanel
            {
                BackColor = Color.White,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                RowCount = 2
            };
            content.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38F));
            content.Controls.Add(tableLayoutPanel16, 0, 0);
            content.Controls.Add(actions, 0, 1);

            AntPanel card = CreateSectionCard(
                "温区偏移矩阵",
                "为最低、低、中、高和最高频率区间设置三档温度偏移",
                content);
            CreatePageBody(
                tabPageCS,
                "Curve Shaper",
                "按频率与温度区域精细调整电压曲线",
                Tuple.Create<Control, float>(card, 310F));
        }

        private void BuildAcpiPage()
        {
            TableLayoutPanel grid = CreateStandardGrid(4, 112F, 0F, 84F);
            AddGridRow(
                grid,
                0,
                "命令",
                comboBoxAvailableCommands,
                null,
                null);
            AddGridRow(
                grid,
                1,
                "预设值",
                comboBoxAvailableValues,
                null,
                null);
            AddGridRow(
                grid,
                2,
                "参数",
                textBoxWmiArgument,
                null,
                null);
            DetachControl(buttonWmiCmdSend);
            PrepareAction(buttonWmiCmdSend);
            grid.Controls.Add(buttonWmiCmdSend, 3, 3);

            Label hint = CreateTextLabel(
                "命令将通过 AMD ACPI/WMI 接口发送。",
                8.5F,
                FontStyle.Regular,
                ModernMutedColor);
            hint.Margin = new Padding(3, 0, 0, 0);
            grid.Controls.Add(hint, 1, 3);
            grid.SetColumnSpan(hint, 2);

            AntPanel card = CreateSectionCard(
                "ACPI 命令",
                "选择固件接口命令、预设值和可选参数",
                grid);
            CreatePageBody(
                tabPageWmi,
                "AMD ACPI",
                "通过系统固件接口执行受支持的处理器命令",
                Tuple.Create<Control, float>(card, 220F));
        }

        private void BuildPstatesPage()
        {
            TableLayoutPanel state = CreateStandardGrid(4, 106F, 0F, 84F);
            AddGridRow(
                state,
                0,
                "P 状态 ID",
                pstateIdBox,
                null,
                btnPstateRead);
            AddGridRow(
                state,
                1,
                "DID",
                pstateDid,
                null,
                btnPstateWrite);
            AddGridRow(
                state,
                2,
                "FID",
                pstateFid,
                null,
                null);
            AddGridRow(
                state,
                3,
                "频率",
                pstateFrequency,
                null,
                null);
            AntPanel stateCard = CreateSectionCard(
                "P-State 参数",
                "读取或写入传统处理器性能状态",
                state);

            TableLayoutPanel bclk = CreateStandardGrid(1, 106F, 80F, 84F);
            Label unit = CreateTextLabel(
                "100 MHz",
                9F,
                FontStyle.Regular,
                ModernMutedColor);
            AddGridRow(
                bclk,
                0,
                "BCLK",
                numericUpDownBclk,
                unit,
                buttonBCLKApply);
            numericUpDownBclk.Dock = DockStyle.None;
            numericUpDownBclk.Anchor =
                AnchorStyles.Left | AnchorStyles.Right;
            numericUpDownBclk.Margin = new Padding(3, 0, 3, 0);
            AntPanel bclkCard = CreateSectionCard(
                "基准时钟",
                "设置频率计算使用的 BCLK 基准",
                bclk);

            CreatePageBody(
                tabPagePstates,
                "PStates",
                "传统 P-State 参数与基准时钟工具",
                Tuple.Create<Control, float>(stateCard, 210F),
                Tuple.Create<Control, float>(bclkCard, 115F));
        }

        private void BuildInfoPage()
        {
            tableLayoutPanel3.BackColor = Color.White;
            tableLayoutPanel3.ColumnStyles.Clear();
            tableLayoutPanel3.RowStyles.Clear();
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Padding = new Padding(0, 4, 0, 0);
            tableLayoutPanel3.RowCount = 12;
            tableLayoutPanel3.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 130F));
            tableLayoutPanel3.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < 10; row++)
                tableLayoutPanel3.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, 27F));
            tableLayoutPanel3.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 36F));

            foreach (Control control in tableLayoutPanel3.Controls)
            {
                Label label = control as Label;
                if (label != null)
                {
                    label.Dock = DockStyle.Fill;
                    label.Margin = new Padding(0);
                    label.TextAlign = ContentAlignment.MiddleLeft;
                    label.ForeColor = label == cpuInfoLabel ||
                                      label == cpuIdLabel ||
                                      label == modelInfoLabel ||
                                      label == packageTypeInfoLabel ||
                                      label == configInfoLabel ||
                                      label == mbVendorInfoLabel ||
                                      label == mbModelInfoLabel ||
                                      label == biosInfoLabel ||
                                      label == firmwareInfoLabel ||
                                      label == smuInfoLabel
                        ? ModernTextColor
                        : ModernMutedColor;
                }
            }
            buttonExport.AutoSize = false;
            buttonExport.Dock = DockStyle.None;
            buttonExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonExport.Margin = new Padding(0, 4, 0, 0);
            buttonExport.MinimumSize = new Size(96, 28);
            buttonExport.MaximumSize = new Size(96, 28);
            buttonExport.Size = new Size(96, 28);
            tableLayoutPanel3.SetColumnSpan(buttonExport, 2);

            AntPanel card = CreateSectionCard(
                "硬件摘要",
                "处理器、主板、固件与 SMU 版本信息",
                tableLayoutPanel3);
            CreatePageBody(
                tabPageInfo,
                "系统信息",
                "当前平台的关键硬件与固件标识",
                Tuple.Create<Control, float>(card, 400F));
        }
    }
}
