using System;
using System.IO;
using System.Windows.Forms;

namespace ZenStatesDebugTool
{
    public partial class ResultForm : Form
    {
        public ResultForm()
        {
            InitializeComponent();
            UiLocalization.Apply(this);
        }

        private void SaveToFile(string filename="")
        {
            string fileName = filename;

            if (filename.Trim().Length == 0)
            {
                string unixTimestamp = Convert.ToString((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMinutes);
                fileName = $@"{String.Join("_", this.Text.Split())}_{unixTimestamp}.txt";
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var sw = new StreamWriter(fileName, true))
            {
                sw.WriteLine(textBoxFormResult.Text);
            }

            MessageBox.Show($"文件已保存为 {fileName}");
        }

        private void buttonSaveResult_Click(object sender, EventArgs e)
        {
            SaveToFile();
        }

        private void buttonSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = "txt",
                RestoreDirectory = true
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SaveToFile(saveFileDialog1.FileName);
            }
        }
    }
}
