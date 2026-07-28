using System.Drawing;
using System.Windows.Forms;

namespace ZenStatesDebugTool
{
    /// <summary>
    /// Applies shared fonts and window policies without performing manual DPI
    /// scaling. Per-monitor scaling is owned exclusively by WinForms 4.8.1.
    /// </summary>
    internal static class UiRuntimeStyle
    {
        internal static readonly Font InterfaceFont =
            new Font(
                "Microsoft YaHei UI",
                9F,
                FontStyle.Regular,
                GraphicsUnit.Point);

        internal static void ConfigureMainWindow(SettingsForm form)
        {
            form.SuspendLayout();
            form.Font = InterfaceFont;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.MaximizeBox = false;
            form.MinimizeBox = true;
            form.SizeGripStyle = SizeGripStyle.Hide;
            form.MinimumSize = Size.Empty;
            form.MaximumSize = Size.Empty;
            form.ClientSize = new Size(1000, 620);
            form.ResumeLayout(false);
        }

        internal static void Apply(Form form)
        {
            form.SuspendLayout();
            form.Font = InterfaceFont;
            form.StartPosition = FormStartPosition.CenterScreen;
            StyleControlTree(form);

            if (!(form is SettingsForm))
                ConfigureAuxiliaryWindow(form);

            form.ResumeLayout(true);
        }

        private static void ConfigureAuxiliaryWindow(Form form)
        {
            if (form is SMUMonitor)
            {
                form.MinimumSize = new Size(480, 460);
                form.ClientSize = new Size(560, 560);
            }
            else if (form is PowerTableMonitor)
            {
                form.MinimumSize = new Size(430, 460);
                form.ClientSize = new Size(520, 560);
            }
            else if (form is PCIRangeMonitor)
            {
                form.MinimumSize = new Size(680, 440);
                form.ClientSize = new Size(880, 560);
            }
            else if (form is ResultForm)
            {
                form.MinimumSize = new Size(520, 340);
                form.ClientSize = new Size(680, 440);
            }
        }

        private static void StyleControlTree(Control root)
        {
            foreach (Control control in root.Controls)
            {
                if (control.Font.Name == "Microsoft Sans Serif")
                    control.Font = InterfaceFont;

                Button button = control as Button;
                if (button != null)
                {
                    button.AutoEllipsis = true;
                    button.UseVisualStyleBackColor = true;
                }

                Label label = control as Label;
                if (label != null)
                    label.AutoEllipsis = true;

                bool isModernMainWindow =
                    control.FindForm() is SettingsForm;
                TabControl tabControl = control as TabControl;
                if (tabControl != null && !isModernMainWindow)
                {
                    tabControl.Multiline = true;
                    tabControl.Padding = new Point(12, 5);
                    tabControl.SizeMode = TabSizeMode.Normal;
                }

                TabPage tabPage = control as TabPage;
                if (tabPage != null && !isModernMainWindow)
                    tabPage.AutoScroll = true;

                DataGridView grid = control as DataGridView;
                if (grid != null)
                {
                    grid.BackgroundColor = SystemColors.Window;
                    grid.ColumnHeadersHeightSizeMode =
                        DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                    grid.RowTemplate.Height = 24;
                }

                StyleControlTree(control);
            }
        }
    }
}
