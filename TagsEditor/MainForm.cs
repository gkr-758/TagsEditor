using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TagsEditor
{
    public partial class TagsEditor : Form
    {
        private string selectedFolder = string.Empty;
        private string[] osuFiles = new string[0];
        private string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

        public TagsEditor()
        {
            InitializeComponent();
            this.Text = "TagsEditor";

            btnBrowse.Click += btnBrowse_Click;
            btnUpdate.Click += btnUpdate_Click;
            btnOpenExeFolder.Click += btnOpenExeFolder_Click;
            btnOpenBackupFolder.Click += btnOpenBackupFolder_Click;

        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolder = dialog.SelectedPath;
                    txtFolderPath.Text = selectedFolder;
                    osuFiles = Directory.GetFiles(selectedFolder, "*.osu", SearchOption.AllDirectories);

                    LoadTags();
                }
            }
        }

        private void btnOpenExeFolder_Click(object sender, EventArgs e)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = exeDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("exeフォルダを開く際にエラーが発生しました。\n" + ex.Message, "エラー");
            }
        }

        private void btnOpenBackupFolder_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                Process.Start(new ProcessStartInfo()
                {
                    FileName = backupDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Backupフォルダを開く際にエラーが発生しました。\n" + ex.Message, "エラー");
            }
        }

        private void LoadTags()
        {
            // file path -> tags
            var fileTags = new Dictionary<string, string>();
            var diffNameToFilePath = new Dictionary<string, string>();

            foreach (var file in osuFiles)
            {
                foreach (var line in File.ReadLines(file))
                {
                    if (line.StartsWith("Tags:"))
                    {
                        string tags = line.Substring(5).Trim();
                        fileTags[file] = tags;

                        // 最後の [] の中身を取る
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        int open = fileName.LastIndexOf('[');
                        int close = fileName.LastIndexOf(']');
                        if (open != -1 && close != -1 && close > open)
                        {
                            string diff = fileName.Substring(open + 1, close - open - 1);
                            if (!diffNameToFilePath.ContainsKey(diff))
                                diffNameToFilePath[diff] = file;
                        }
                        break;
                    }
                }
            }

            var distinctTags = fileTags.Values.Distinct().ToList();

            if (distinctTags.Count == 1)
            {
                txtCurrentTags.Text = distinctTags[0];
                txtNewTags.Text = distinctTags[0];
            }
            else
            {
                using (var dialog = new SelectOsuFileDialog(diffNameToFilePath))
                {
                    if (dialog.ShowDialog() == DialogResult.OK && File.Exists(dialog.SelectedFilePath))
                    {
                        string selectedFile = dialog.SelectedFilePath;
                        string selectedTags = fileTags[selectedFile];
                        txtCurrentTags.Text = selectedTags;
                        txtNewTags.Text = selectedTags;
                    }
                    else
                    {
                        txtCurrentTags.Text = "";
                        txtNewTags.Text = string.Empty;
                    }
                }
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (osuFiles.Length == 0)
            {
                MessageBox.Show(".osuファイルが見つかりません。", "エラー");
                return;
            }

            var result = MessageBox.Show($"{osuFiles.Length} 件の .osu ファイルの Tags を更新しますか？", "確認", MessageBoxButtons.OKCancel);
            if (result != DialogResult.OK) return;

            try
            {
                if (chkBackup.Checked)
                {
                    foreach (var file in osuFiles)
                    {
                        string relPath = Path.GetRelativePath(selectedFolder, file);
                        string backupPath = Path.Combine(backupDir, Path.GetFileName(selectedFolder), relPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                        File.Copy(file, backupPath, true);
                    }
                }

                foreach (var file in osuFiles)
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("Tags:"))
                        {
                            lines[i] = "Tags:" + txtNewTags.Text.Trim();
                            break;
                        }
                    }
                    File.WriteAllLines(file, lines, Encoding.UTF8);
                }

                MessageBox.Show("更新しました。", "完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました。", "エラー");
                File.AppendAllText("error.log", $"[{DateTime.Now}] {ex.Message}\n{ex.StackTrace}\n");
            }
        }
    }
}
