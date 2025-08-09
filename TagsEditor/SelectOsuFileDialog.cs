using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TagsEditor
{
    public partial class SelectOsuFileDialog : Form
    {
        private readonly Dictionary<string, string> diffNameToFilePath;

        // 初期化しておく（null許容にしないため）
        public string SelectedFilePath { get; private set; } = string.Empty;

        public SelectOsuFileDialog(Dictionary<string, string> diffNameToFilePath, string diffList)
        {
            InitializeComponent();

            this.diffNameToFilePath = diffNameToFilePath ?? new Dictionary<string, string>();

            lblDiffList.Text = $"Metadataが異なるDiffを検知しました。どのDiffを読み込みますか？ \n異なる項目: {diffList}";

            foreach (var diffName in this.diffNameToFilePath.Keys)
            {
                lstDiffNames.Items.Add(diffName);
            }

            if (lstDiffNames.Items.Count > 0)
            {
                lstDiffNames.SelectedIndex = 0;
            }

            btnOK.Click += BtnOK_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (lstDiffNames.SelectedItem == null)
            {
                MessageBox.Show("難易度を選択してください。", "エラー");
                return;
            }

            string selectedDiff = lstDiffNames.SelectedItem.ToString()!;
            SelectedFilePath = diffNameToFilePath[selectedDiff];
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

    }
}
