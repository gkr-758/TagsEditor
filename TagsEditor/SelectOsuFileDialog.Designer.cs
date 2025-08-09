namespace TagsEditor
{
    partial class SelectOsuFileDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblDiffList;
        private System.Windows.Forms.ListBox lstDiffNames;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        private void InitializeComponent()
        {
            this.lblDiffList = new System.Windows.Forms.Label();
            this.lstDiffNames = new System.Windows.Forms.ListBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblDiffList
            // 
            this.lblDiffList.AutoSize = true;
            this.lblDiffList.Location = new System.Drawing.Point(12, 9);
            this.lblDiffList.Name = "lblDiffList";
            this.lblDiffList.Size = new System.Drawing.Size(83, 15);
            this.lblDiffList.TabIndex = 0;
            this.lblDiffList.Text = "異なる項目: ---";
            // 
            // lstDiffNames
            // 
            this.lstDiffNames.FormattingEnabled = true;
            this.lstDiffNames.ItemHeight = 15;
            this.lstDiffNames.Location = new System.Drawing.Point(12, 51);
            this.lstDiffNames.Name = "lstDiffNames";
            this.lstDiffNames.Size = new System.Drawing.Size(364, 109);
            this.lstDiffNames.TabIndex = 1;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(220, 170);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 25);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(301, 170);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 25);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // SelectOsuFileDialog
            // 
            this.ClientSize = new System.Drawing.Size(387, 207);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lstDiffNames);
            this.Controls.Add(this.lblDiffList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectOsuFileDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "代表ファイルを選択してください";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
