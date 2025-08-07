namespace TagsEditor
{
    partial class TagsEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TagsEditor));
            this.label1 = new System.Windows.Forms.Label();
            this.txtFolderPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.CurrentTags = new System.Windows.Forms.Label();
            this.txtCurrentTags = new System.Windows.Forms.RichTextBox();
            this.NewTags = new System.Windows.Forms.Label();
            this.txtNewTags = new System.Windows.Forms.RichTextBox();
            this.chkBackup = new System.Windows.Forms.CheckBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnOpenExeFolder = new System.Windows.Forms.Button();
            this.btnOpenBackupFolder = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "ファイルパス";
            // 
            // txtFolderPath
            // 
            this.txtFolderPath.Location = new System.Drawing.Point(12, 27);
            this.txtFolderPath.Name = "txtFolderPath";
            this.txtFolderPath.Size = new System.Drawing.Size(307, 23);
            this.txtFolderPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(325, 27);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "参照";
            this.btnBrowse.UseVisualStyleBackColor = true;
            // 
            // CurrentTags
            // 
            this.CurrentTags.AutoSize = true;
            this.CurrentTags.Location = new System.Drawing.Point(12, 53);
            this.CurrentTags.Name = "CurrentTags";
            this.CurrentTags.Size = new System.Drawing.Size(59, 15);
            this.CurrentTags.TabIndex = 3;
            this.CurrentTags.Text = "既存のタグ";
            // 
            // txtCurrentTags
            // 
            this.txtCurrentTags.Location = new System.Drawing.Point(12, 71);
            this.txtCurrentTags.Name = "txtCurrentTags";
            this.txtCurrentTags.ReadOnly = true;
            this.txtCurrentTags.Size = new System.Drawing.Size(388, 96);
            this.txtCurrentTags.TabIndex = 4;
            this.txtCurrentTags.Text = "";
            // 
            // NewTags
            // 
            this.NewTags.AutoSize = true;
            this.NewTags.Location = new System.Drawing.Point(12, 170);
            this.NewTags.Name = "NewTags";
            this.NewTags.Size = new System.Drawing.Size(55, 15);
            this.NewTags.TabIndex = 5;
            this.NewTags.Text = "新しいタグ";
            // 
            // txtNewTags
            // 
            this.txtNewTags.Location = new System.Drawing.Point(12, 188);
            this.txtNewTags.Name = "txtNewTags";
            this.txtNewTags.Size = new System.Drawing.Size(388, 96);
            this.txtNewTags.TabIndex = 6;
            this.txtNewTags.Text = "";
            // 
            // chkBackup
            // 
            this.chkBackup.AutoSize = true;
            this.chkBackup.Location = new System.Drawing.Point(12, 290);
            this.chkBackup.Name = "chkBackup";
            this.chkBackup.Size = new System.Drawing.Size(79, 19);
            this.chkBackup.TabIndex = 7;
            this.chkBackup.Text = "バックアップ";
            this.chkBackup.UseVisualStyleBackColor = true;
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(12, 353);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(388, 85);
            this.btnUpdate.TabIndex = 8;
            this.btnUpdate.Text = "更新";
            this.btnUpdate.UseVisualStyleBackColor = true;
            // 
            // btnOpenExeFolder
            // 
            this.btnOpenExeFolder.Location = new System.Drawing.Point(12, 315);
            this.btnOpenExeFolder.Name = "btnOpenExeFolder";
            this.btnOpenExeFolder.Size = new System.Drawing.Size(164, 32);
            this.btnOpenExeFolder.TabIndex = 9;
            this.btnOpenExeFolder.Text = "exeフォルダを開く";
            this.btnOpenExeFolder.UseVisualStyleBackColor = true;
            // 
            // btnOpenBackupFolder
            // 
            this.btnOpenBackupFolder.Location = new System.Drawing.Point(236, 315);
            this.btnOpenBackupFolder.Name = "btnOpenBackupFolder";
            this.btnOpenBackupFolder.Size = new System.Drawing.Size(164, 32);
            this.btnOpenBackupFolder.TabIndex = 10;
            this.btnOpenBackupFolder.Text = "Backupフォルダを開く";
            this.btnOpenBackupFolder.UseVisualStyleBackColor = true;
            // 
            // TagsEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(412, 450);
            this.Controls.Add(this.btnOpenBackupFolder);
            this.Controls.Add(this.btnOpenExeFolder);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.chkBackup);
            this.Controls.Add(this.txtNewTags);
            this.Controls.Add(this.NewTags);
            this.Controls.Add(this.txtCurrentTags);
            this.Controls.Add(this.CurrentTags);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtFolderPath);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TagsEditor";
            this.Text = "TagsEditor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label label1;
        private TextBox txtFolderPath;
        private Button btnBrowse;
        private Label CurrentTags;
        private RichTextBox txtCurrentTags;
        private Label NewTags;
        private RichTextBox txtNewTags;
        private CheckBox chkBackup;
        private Button btnUpdate;
        private Button btnOpenExeFolder;
        private Button btnOpenBackupFolder;
    }
}