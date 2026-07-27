using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AntButton = AntdUI.Button;
using AntCheckbox = AntdUI.Checkbox;
using AntPanel = AntdUI.Panel;
using AntRadio = AntdUI.Radio;
using WinButton = System.Windows.Forms.Button;
using WinCheckbox = System.Windows.Forms.CheckBox;
using WinRadio = System.Windows.Forms.RadioButton;

namespace ZenStatesDebugTool
{
    /// <summary>
    /// Adds the modern AntdUI surface without changing the upstream event handlers.
    /// The original WinForms buttons remain as lightweight event proxies, which
    /// keeps future upstream merges and hardware logic changes isolated from UI work.
    /// </summary>
    internal static class ModernUi
    {
        internal static void WrapCards(
            IEnumerable<Control> cards,
            Color cardColor,
            Color borderColor)
        {
            foreach (Control card in cards.Where(item => item != null).ToArray())
                WrapCard(card, cardColor, borderColor);
        }

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
                Radius = 7,
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

        private static void WrapCard(
            Control card,
            Color cardColor,
            Color borderColor)
        {
            Control parent = card.Parent;
            if (parent == null || parent is AntPanel)
                return;

            AntPanel wrapper = new AntPanel
            {
                Anchor = card.Anchor,
                Back = cardColor,
                BorderColor = borderColor,
                BorderWidth = 1F,
                Dock = card.Dock,
                Location = card.Location,
                Margin = card.Margin,
                MinimumSize = card.MinimumSize,
                MaximumSize = card.MaximumSize,
                Name = card.Name + "Card",
                Padding = new Padding(1),
                Radius = 9,
                Shadow = 2,
                ShadowColor = Color.FromArgb(62, 81, 105),
                ShadowOffsetY = 1,
                ShadowOpacity = 0.08F,
                Size = card.Size,
                TabIndex = card.TabIndex
            };

            ReplaceControl(parent, card, wrapper);
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0);

            TableLayoutPanel table = card as TableLayoutPanel;
            if (table != null)
                table.BorderStyle = BorderStyle.None;

            wrapper.Controls.Add(card);
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
