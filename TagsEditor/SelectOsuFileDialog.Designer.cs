using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TagsEditor
{
    public partial class SelectOsuFileDialog : Form
    {
        public string SelectedFilePath { get; private set; }

        public SelectOsuFileDialog(Dictionary<string, string> fileMap)
        {
            InitializeComponent();
            this.Text = "ファイル選択";
            Label lbl = new Label
            {
                Text = "Diff間でタグの内容が異なります。\nどの *.osu ファイルのタグを表示しますか？",
                AutoSize = true,
                Top = 10,
                Left = 10
            };

            ComboBox cmb = new ComboBox
            {
                Name = "comboFiles",
                Left = 10,
                Top = 50,
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (var kv in fileMap)
                cmb.Items.Add(kv.Key);

            if (cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;

            Button btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 130,
                Top = 90,
                Width = 80
            };

            Button btnCancel = new Button
            {
                Text = "キャンセル",
                DialogResult = DialogResult.Cancel,
                Left = 220,
                Top = 90,
                Width = 80
            };

            this.Controls.Add(lbl);
            this.Controls.Add(cmb);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(350, 130);

            btnOK.Click += (s, e) =>
            {
                if (cmb.SelectedItem != null)
                    SelectedFilePath = fileMap[cmb.SelectedItem.ToString()];
            };
        }
    }
}
