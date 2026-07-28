using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AntButton = AntdUI.Button;
using AntCheckbox = AntdUI.Checkbox;
using AntInput = AntdUI.Input;
using AntInputNumber = AntdUI.InputNumber;
using AntRadio = AntdUI.Radio;
using AntSelect = AntdUI.Select;
using WinButton = System.Windows.Forms.Button;
using WinCheckbox = System.Windows.Forms.CheckBox;
using WinComboBox = System.Windows.Forms.ComboBox;
using WinNumericUpDown = System.Windows.Forms.NumericUpDown;
using WinRadio = System.Windows.Forms.RadioButton;
using WinTextBox = System.Windows.Forms.TextBox;

namespace ZenStatesDebugTool
{
    /// <summary>
    /// Converts functional WinForms controls into the single AntdUI visual
    /// surface while keeping hardware event handlers isolated from presentation.
    /// </summary>
    internal static class ModernUi
    {
        private const int ControlCornerRadius = 8;

        internal static void UpgradeButtons(
            Control root,
            ISet<WinButton> primaryButtons,
            ToolTip toolTip,
            Color accentColor,
            Color accentHoverColor,
            Color accentActiveColor,
            Color borderColor)
        {
            List<WinButton> buttons = new List<WinButton>();
            CollectButtons(root, buttons);

            foreach (WinButton button in buttons)
            {
                ReplaceButton(
                    button,
                    primaryButtons.Contains(button),
                    toolTip,
                    accentColor,
                    accentHoverColor,
                    accentActiveColor,
                    borderColor);
            }
        }

        internal static void UpgradeSelectionControls(
            Control root,
            ToolTip toolTip,
            Color accentColor)
        {
            List<WinCheckbox> checkboxes = new List<WinCheckbox>();
            List<WinRadio> radios = new List<WinRadio>();
            CollectSelectionControls(root, checkboxes, radios);

            foreach (WinCheckbox checkbox in checkboxes)
                ReplaceCheckbox(checkbox, toolTip, accentColor);
            foreach (WinRadio radio in radios)
                ReplaceRadio(radio, toolTip, accentColor);
        }

        internal static void UpgradeDataControls(
            Control root,
            ISet<WinTextBox> excludedTextBoxes,
            ToolTip toolTip,
            Color accentColor,
            Color borderColor)
        {
            List<WinTextBox> textBoxes = new List<WinTextBox>();
            List<WinNumericUpDown> numericInputs =
                new List<WinNumericUpDown>();
            List<WinComboBox> comboBoxes = new List<WinComboBox>();
            CollectDataControls(
                root,
                textBoxes,
                numericInputs,
                comboBoxes);

            foreach (WinTextBox textBox in textBoxes)
            {
                if (excludedTextBoxes == null ||
                    !excludedTextBoxes.Contains(textBox))
                {
                    ReplaceTextBox(
                        textBox,
                        toolTip,
                        accentColor,
                        borderColor);
                }
            }
            foreach (WinNumericUpDown numericInput in numericInputs)
            {
                ReplaceNumericInput(
                    numericInput,
                    toolTip,
                    accentColor,
                    borderColor);
            }
            foreach (WinComboBox comboBox in comboBoxes)
            {
                ReplaceComboBox(
                    comboBox,
                    toolTip,
                    accentColor,
                    borderColor);
            }
        }

        private static void CollectDataControls(
            Control root,
            ICollection<WinTextBox> textBoxes,
            ICollection<WinNumericUpDown> numericInputs,
            ICollection<WinComboBox> comboBoxes)
        {
            foreach (Control control in root.Controls)
            {
                WinNumericUpDown numeric = control as WinNumericUpDown;
                if (numeric != null)
                {
                    numericInputs.Add(numeric);
                    continue;
                }

                WinComboBox combo = control as WinComboBox;
                if (combo != null)
                {
                    comboBoxes.Add(combo);
                    continue;
                }

                WinTextBox textBox = control as WinTextBox;
                if (textBox != null)
                {
                    textBoxes.Add(textBox);
                    continue;
                }

                CollectDataControls(
                    control,
                    textBoxes,
                    numericInputs,
                    comboBoxes);
            }
        }

        private static void ReplaceTextBox(
            WinTextBox original,
            ToolTip toolTip,
            Color accentColor,
            Color borderColor)
        {
            Control parent = original.Parent;
            if (parent == null)
                return;

            AntInput modern = new AntInput
            {
                Anchor = original.Anchor,
                AutoSize = false,
                BackColor = Color.White,
                BorderActive = accentColor,
                BorderColor = borderColor,
                BorderHover = accentColor,
                BorderWidth = 1F,
                CausesValidation = original.CausesValidation,
                Dock = original.Dock,
                Enabled = original.Enabled,
                Font = original.Font,
                ForeColor = original.ForeColor,
                Location = original.Location,
                Margin = original.Margin,
                MaximumSize = original.MaximumSize,
                MaxLength = original.MaxLength,
                MinimumSize = original.MinimumSize,
                Multiline = original.Multiline,
                Name = original.Name + "Modern",
                Padding = original.Padding,
                Radius = ControlCornerRadius,
                ReadOnly = original.ReadOnly,
                Size = original.Size,
                TabIndex = original.TabIndex,
                TabStop = original.TabStop,
                Tag = original.Tag,
                Text = original.Text,
                TextAlign = original.TextAlign,
                UseSystemPasswordChar =
                    original.UseSystemPasswordChar,
                Visible = true,
                WaveSize = 0
            };

            bool synchronizing = false;
            modern.TextChanged += delegate
            {
                if (synchronizing)
                    return;
                synchronizing = true;
                original.Text = modern.Text;
                synchronizing = false;
            };
            original.TextChanged += delegate
            {
                if (synchronizing)
                    return;
                synchronizing = true;
                modern.Text = original.Text;
                synchronizing = false;
            };
            original.EnabledChanged += delegate
            {
                modern.Enabled = original.Enabled;
            };
            modern.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                original.Text = modern.Text;
                RaiseKeyDown(original, e);
            };
            modern.KeyPress += delegate(object sender, KeyPressEventArgs e)
            {
                RaiseKeyPress(original, e);
            };
            modern.Validated += delegate
            {
                RaiseValidated(original);
            };

            CopyToolTip(original, modern, toolTip);
            ReplaceControl(parent, original, modern);
        }

        private static void ReplaceNumericInput(
            WinNumericUpDown original,
            ToolTip toolTip,
            Color accentColor,
            Color borderColor)
        {
            Control parent = original.Parent;
            if (parent == null)
                return;

            bool isCurveShaperValue =
                !string.IsNullOrEmpty(original.Name) &&
                original.Name.StartsWith(
                    "cs_",
                    StringComparison.OrdinalIgnoreCase);

            AntInputNumber modern = new AntInputNumber
            {
                AlwaysShowControl = !isCurveShaperValue,
                Anchor = original.Anchor,
                AutoSize = false,
                BackColor = Color.White,
                BorderActive = accentColor,
                BorderColor = borderColor,
                BorderHover = accentColor,
                BorderWidth = 1F,
                DecimalPlaces = original.DecimalPlaces,
                Dock = original.Dock,
                Enabled = original.Enabled,
                Font = original.Font,
                ForeColor = original.ForeColor,
                Hexadecimal = original.Hexadecimal,
                Increment = original.Increment,
                Location = original.Location,
                Margin = original.Margin,
                Maximum = original.Maximum,
                MaximumSize = original.MaximumSize,
                Minimum = original.Minimum,
                MinimumSize = original.MinimumSize,
                Name = original.Name + "Modern",
                Radius = ControlCornerRadius,
                ReadOnly = original.ReadOnly,
                ShowControl = !isCurveShaperValue,
                Size = original.Size,
                TabIndex = original.TabIndex,
                TabStop = original.TabStop,
                Tag = original.Tag,
                TextAlign = original.TextAlign,
                ThousandsSeparator = original.ThousandsSeparator,
                Value = original.Value,
                Visible = true,
                WaveSize = 0
            };

            bool synchronizing = false;
            modern.ValueChanged += delegate(
                object sender,
                AntdUI.DecimalEventArgs e)
            {
                if (synchronizing)
                    return;
                synchronizing = true;
                original.Value = e.Value;
                synchronizing = false;
            };
            original.ValueChanged += delegate
            {
                if (synchronizing)
                    return;
                synchronizing = true;
                modern.Value = original.Value;
                synchronizing = false;
            };
            original.EnabledChanged += delegate
            {
                modern.Enabled = original.Enabled;
            };

            CopyToolTip(original, modern, toolTip);
            ReplaceControl(parent, original, modern);
        }

        private static void ReplaceComboBox(
            WinComboBox original,
            ToolTip toolTip,
            Color accentColor,
            Color borderColor)
        {
            Control parent = original.Parent;
            if (parent == null)
                return;

            AntSelect modern = new AntSelect
            {
                Anchor = original.Anchor,
                AutoSize = false,
                BackColor = Color.White,
                BorderActive = accentColor,
                BorderColor = borderColor,
                BorderHover = accentColor,
                BorderWidth = 1F,
                ClickSwitchDropdown = true,
                Dock = original.Dock,
                DropDownArrow = true,
                DropDownRadius = ControlCornerRadius,
                Enabled = original.Enabled,
                Font = original.Font,
                ForeColor = original.ForeColor,
                List = true,
                ListAutoWidth = false,
                Location = original.Location,
                Margin = original.Margin,
                MaximumSize = original.MaximumSize,
                MinimumSize = original.MinimumSize,
                Name = original.Name + "Modern",
                Radius = ControlCornerRadius,
                // AntdUI Select is list-only by design. Its ReadOnly flag
                // disables opening the drop-down entirely, unlike the
                // WinForms DropDownList style.
                ReadOnly = false,
                Size = original.Size,
                TabIndex = original.TabIndex,
                TabStop = original.TabStop,
                Tag = original.Tag,
                TextAlign = HorizontalAlignment.Left,
                Visible = true,
                WaveSize = 0
            };

            bool synchronizing = false;
            Action refreshItems = delegate
            {
                synchronizing = true;
                modern.Items.Clear();
                foreach (object item in original.Items)
                    modern.Items.Add(item);
                modern.SelectedIndex =
                    original.SelectedIndex >= 0 &&
                    original.SelectedIndex < modern.Items.Count
                        ? original.SelectedIndex
                        : -1;
                synchronizing = false;
            };
            refreshItems();

            modern.SelectedIndexChanged += delegate(
                object sender,
                AntdUI.IntEventArgs e)
            {
                if (synchronizing)
                    return;
                synchronizing = true;
                original.SelectedIndex = e.Value;
                synchronizing = false;
            };
            original.SelectedIndexChanged += delegate
            {
                if (synchronizing)
                    return;
                refreshItems();
            };
            modern.MouseDown += delegate
            {
                refreshItems();
            };
            original.EnabledChanged += delegate
            {
                modern.Enabled = original.Enabled;
            };
            modern.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                RaiseKeyDown(original, e);
            };

            CopyToolTip(original, modern, toolTip);
            ReplaceControl(parent, original, modern);
        }

        private static void CollectSelectionControls(
            Control root,
            ICollection<WinCheckbox> checkboxes,
            ICollection<WinRadio> radios)
        {
            foreach (Control control in root.Controls)
            {
                WinCheckbox checkbox = control as WinCheckbox;
                if (checkbox != null)
                    checkboxes.Add(checkbox);

                WinRadio radio = control as WinRadio;
                if (radio != null)
                    radios.Add(radio);

                CollectSelectionControls(control, checkboxes, radios);
            }
        }

        private static void ReplaceCheckbox(
            WinCheckbox original,
            ToolTip toolTip,
            Color accentColor)
        {
            Control parent = original.Parent;
            if (parent == null)
                return;

            AntCheckbox modern = new AntCheckbox
            {
                Anchor = original.Anchor,
                AutoCheck = false,
                AutoSize = original.AutoSize,
                Checked = original.Checked,
                Dock = original.Dock,
                Enabled = original.Enabled,
                Fill = accentColor,
                Font = original.Font,
                ForeColor = original.ForeColor,
                Location = original.Location,
                Margin = original.Margin,
                Name = original.Name + "Modern",
                Padding = original.Padding,
                Size = original.Size,
                TabIndex = original.TabIndex,
                TabStop = original.TabStop,
                Tag = original.Tag,
                Text = original.Text,
                TextAlign = original.TextAlign
            };

            if (string.IsNullOrEmpty(original.Text))
            {
                modern.AutoSize = false;
                modern.Dock = DockStyle.Fill;
                modern.Margin = new Padding(0);
                modern.TextAlign = ContentAlignment.MiddleCenter;
            }

            bool syncing = false;
            modern.CheckedChanged += delegate(object sender, AntdUI.BoolEventArgs e)
            {
                if (syncing)
                    return;
                syncing = true;
                original.Checked = e.Value;
                syncing = false;
            };
            modern.Click += delegate
            {
                if (!original.Enabled)
                    return;

                modern.Checked = !modern.Checked;
                original.Checked = modern.Checked;
                RaiseClick(original);
            };
            original.CheckedChanged += delegate
            {
                if (syncing)
                    return;
                syncing = true;
                modern.Checked = original.Checked;
                syncing = false;
            };

            BindCommonState(original, modern);
            CopyToolTip(original, modern, toolTip);
            original.AutoCheck = false;
            ReplaceControl(parent, original, modern);
        }

        private static void ReplaceRadio(
            WinRadio original,
            ToolTip toolTip,
            Color accentColor)
        {
            Control parent = original.Parent;
            if (parent == null)
                return;

            AntRadio modern = new AntRadio
            {
                Anchor = original.Anchor,
                AutoCheck = true,
                AutoSize = original.AutoSize,
                Checked = original.Checked,
                Dock = original.Dock,
                Enabled = original.Enabled,
                Fill = accentColor,
                Font = original.Font,
                Location = original.Location,
                Margin = original.Margin,
                Name = original.Name + "Modern",
                Padding = original.Padding,
                Size = original.Size,
                TabIndex = original.TabIndex,
                TabStop = original.TabStop,
                Tag = original.Tag,
                Text = original.Text,
                TextAlign = original.TextAlign
            };

            bool syncing = false;
            modern.CheckedChanged += delegate(object sender, AntdUI.BoolEventArgs e)
            {
                if (syncing)
                    return;
                syncing = true;
                original.Checked = e.Value;
                syncing = false;
            };
            original.CheckedChanged += delegate
            {
                if (syncing)
                    return;
                syncing = true;
                modern.Checked = original.Checked;
                syncing = false;
            };

            BindCommonState(original, modern);
            CopyToolTip(original, modern, toolTip);
            ReplaceControl(parent, original, modern);
        }

        private static void BindCommonState(Control original, Control modern)
        {
            original.EnabledChanged += delegate
            {
                modern.Enabled = original.Enabled;
            };
            original.TextChanged += delegate
            {
                modern.Text = original.Text;
            };
            original.LocationChanged += delegate
            {
                modern.Location = original.Location;
            };
            original.SizeChanged += delegate
            {
                modern.Size = original.Size;
            };
        }

        private static void CopyToolTip(
            Control original,
            Control modern,
            ToolTip toolTip)
        {
            if (toolTip == null)
                return;

            string helpText = toolTip.GetToolTip(original);
            if (!string.IsNullOrEmpty(helpText))
                toolTip.SetToolTip(modern, helpText);
        }

        private static void RaiseClick(Control original)
        {
            typeof(Control)
                .GetMethod(
                    "OnClick",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .Invoke(original, new object[] { EventArgs.Empty });
        }

        private static void RaiseKeyDown(
            Control original,
            KeyEventArgs eventArgs)
        {
            typeof(Control)
                .GetMethod(
                    "OnKeyDown",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .Invoke(original, new object[] { eventArgs });
        }

        private static void RaiseKeyPress(
            Control original,
            KeyPressEventArgs eventArgs)
        {
            typeof(Control)
                .GetMethod(
                    "OnKeyPress",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .Invoke(original, new object[] { eventArgs });
        }

        private static void RaiseValidated(Control original)
        {
            typeof(Control)
                .GetMethod(
                    "OnValidated",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .Invoke(original, new object[] { EventArgs.Empty });
        }

        private static void CollectButtons(Control root, ICollection<WinButton> buttons)
        {
            foreach (Control control in root.Controls)
            {
                WinButton button = control as WinButton;
                if (button != null)
                    buttons.Add(button);

                CollectButtons(control, buttons);
            }
        }

        private static void ReplaceButton(
            WinButton original,
            bool primary,
            ToolTip toolTip,
            Color accentColor,
            Color accentHoverColor,
            Color accentActiveColor,
            Color borderColor)
        {
            Control parent = original.Parent;
            if (parent == null)
                return;

            AntButton modern = new AntButton
            {
                AccessibleDescription = original.AccessibleDescription,
                AccessibleName = original.AccessibleName,
                Anchor = original.Anchor,
                AutoEllipsis = original.AutoEllipsis,
                AutoSize = original.AutoSize,
                CausesValidation = original.CausesValidation,
                Cursor = original.Cursor,
                Dock = original.Dock,
                Enabled = original.Enabled,
                Font = original.Font,
                Location = original.Location,
                Margin = original.Margin,
                MinimumSize = original.MinimumSize,
                MaximumSize = original.MaximumSize,
                Name = original.Name + "Modern",
                Padding = original.Padding,
                Radius = ControlCornerRadius,
                Size = original.Size,
                TabIndex = original.TabIndex,
                TabStop = original.TabStop,
                Tag = original.Tag,
                Text = original.Text,
                TextAlign = original.TextAlign,
                WaveSize = 0
            };

            if (primary)
            {
                modern.Type = AntdUI.TTypeMini.Primary;
                modern.BackColor = accentColor;
                modern.BackHover = accentHoverColor;
                modern.BackActive = accentActiveColor;
                modern.ForeColor = Color.White;
                modern.ForeHover = Color.White;
                modern.ForeActive = Color.White;
                modern.BorderWidth = 0F;
            }
            else
            {
                modern.Type = AntdUI.TTypeMini.Default;
                modern.DefaultBack = Color.White;
                modern.DefaultBorderColor = borderColor;
                modern.BorderWidth = 1F;
            }

            CopyToolTip(original, modern, toolTip);

            modern.Click += delegate
            {
                if (original.Enabled)
                    original.PerformClick();
            };
            original.EnabledChanged += delegate
            {
                modern.Enabled = original.Enabled;
            };
            original.TextChanged += delegate
            {
                modern.Text = original.Text;
            };

            ReplaceControl(parent, original, modern);
        }

        private static void ReplaceControl(
            Control parent,
            Control original,
            Control replacement)
        {
            int childIndex = parent.Controls.GetChildIndex(original);
            TableLayoutPanel table = parent as TableLayoutPanel;
            TableLayoutPanelCellPosition position =
                table == null
                    ? new TableLayoutPanelCellPosition(-1, -1)
                    : table.GetPositionFromControl(original);
            int columnSpan = table == null ? 1 : table.GetColumnSpan(original);
            int rowSpan = table == null ? 1 : table.GetRowSpan(original);

            parent.SuspendLayout();
            parent.Controls.Remove(original);
            if (table != null && position.Column >= 0 && position.Row >= 0)
            {
                table.Controls.Add(replacement, position.Column, position.Row);
                table.SetColumnSpan(replacement, columnSpan);
                table.SetRowSpan(replacement, rowSpan);
            }
            else
            {
                parent.Controls.Add(replacement);
                parent.Controls.SetChildIndex(replacement, childIndex);
            }
            parent.ResumeLayout(true);
        }
    }
}
