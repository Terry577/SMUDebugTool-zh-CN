using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZenStatesDebugTool
{
    /// <summary>
    /// Applies a consistent, DPI-aware layout policy after Chinese text is loaded.
    /// The upstream Designer remains untouched so future upstream merges stay simple.
    /// </summary>
    internal static class ChineseUiLayout
    {
        private static readonly Font InterfaceFont =
            new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        internal static void Apply(Form form)
        {
            form.SuspendLayout();
            form.AutoScaleDimensions = new SizeF(96F, 96F);
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Font = InterfaceFont;
            form.StartPosition = FormStartPosition.CenterScreen;

            StyleControlTree(form);
            ConfigureWindow(form);

            form.ResumeLayout(true);
            form.PerformAutoScale();

            SettingsForm settingsForm = form as SettingsForm;
            if (settingsForm != null)
            {
                float scaleFactor =
                    settingsForm.CurrentAutoScaleDimensions.Width / 96F;
                settingsForm.FinalizeModernDpiLayout(scaleFactor);
            }
        }

        private static void ConfigureWindow(Form form)
        {
            if (form is SettingsForm)
            {
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.MaximizeBox = false;
                form.MinimizeBox = true;
                form.SizeGripStyle = SizeGripStyle.Hide;
                form.MinimumSize = Size.Empty;
                form.MaximumSize = Size.Empty;
                form.ClientSize = new Size(1180, 700);
                return;
            }

            if (form is SMUMonitor)
            {
                form.MinimumSize = new Size(480, 460);
                form.ClientSize = new Size(560, 560);
                return;
            }

            if (form is PowerTableMonitor)
            {
                form.MinimumSize = new Size(430, 460);
                form.ClientSize = new Size(520, 560);
                return;
            }

            if (form is PCIRangeMonitor)
            {
                form.MinimumSize = new Size(680, 440);
                form.ClientSize = new Size(880, 560);
                return;
            }

            if (form is ResultForm)
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

                TabControl tabControl = control as TabControl;
                bool isModernMainWindow =
                    control.FindForm() is SettingsForm;
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
