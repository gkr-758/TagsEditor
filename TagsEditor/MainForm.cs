using System;
using System.Collections.Generic;
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

                    LoadMetadata();
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

        private void LoadMetadata()
        {
            Dictionary<string, Control> metadataMap = new Dictionary<string, Control>()
            {
                { "Artist:", txtArtist },
                { "ArtistUnicode:", txtRomanisedArtist },
                { "Title:", txtTitle },
                { "TitleUnicode:", txtRomanisedTitle },
                { "Creator:", txtCreator },
                { "Source:", txtSource },
                { "Tags:", txtNewTags }
            };

            var fileMetadata = new Dictionary<string, Dictionary<string, string>>();
            var diffNameToFilePath = new Dictionary<string, string>();

            foreach (var file in osuFiles)
            {
                var meta = new Dictionary<string, string>();
                var lines = File.ReadAllLines(file);

                foreach (var key in metadataMap.Keys)
                {
                    var line = lines.FirstOrDefault(l => l.StartsWith(key));
                    if (line != null)
                        meta[key] = line.Substring(key.Length).Trim();
                }

                // BGファイル名 & BG位置
                int bgIndex = Array.FindIndex(lines, l => l.StartsWith("//Background and Video events"));
                if (bgIndex != -1 && bgIndex + 1 < lines.Length)
                {
                    var parts = lines[bgIndex + 1].Split(',');
                    if (parts.Length >= 5)
                    {
                        meta["BGFile"] = parts[2].Trim().Trim('"');
                        meta["BGPos"] = parts[4].Trim();
                    }
                    else
                    {
                        meta["BGFile"] = "";
                        meta["BGPos"] = "";
                    }
                }
                else
                {
                    meta["BGFile"] = "";
                    meta["BGPos"] = "";
                }

                fileMetadata[file] = meta;

                // diff名を取得
                string fileName = Path.GetFileNameWithoutExtension(file);
                int open = fileName.LastIndexOf('[');
                int close = fileName.LastIndexOf(']');
                if (open != -1 && close != -1 && close > open)
                {
                    string diff = fileName.Substring(open + 1, close - open - 1);
                    if (!diffNameToFilePath.ContainsKey(diff))
                        diffNameToFilePath[diff] = file;
                }
            }

            var differingKeys = new List<string>();
            var firstMeta = fileMetadata.Values.FirstOrDefault();

            if (firstMeta == null)
            {
                // ファイルがない場合
                foreach (var tb in metadataMap.Values)
                    tb.Text = "";
                txtBGFile.Text = "";
                txtBGPos.Text = "";
                return;
            }

            foreach (var key in firstMeta.Keys)
            {
                var distinctVals = fileMetadata.Values.Select(m => m[key]).Distinct().ToList();
                if (distinctVals.Count > 1)
                    differingKeys.Add(key);
            }

            if (differingKeys.Count == 0)
            {
                foreach (var kv in metadataMap)
                    kv.Value.Text = firstMeta[kv.Key];

                txtBGFile.Text = firstMeta["BGFile"];
                txtBGPos.Text = firstMeta["BGPos"];
            }
            else
            {
                string diffList = string.Join(", ", differingKeys);
                using (var dialog = new SelectOsuFileDialog(diffNameToFilePath, diffList))
                {
                    if (dialog.ShowDialog() == DialogResult.OK && File.Exists(dialog.SelectedFilePath))
                    {
                        var meta = fileMetadata[dialog.SelectedFilePath];
                        foreach (var kv in metadataMap)
                            kv.Value.Text = meta[kv.Key];

                        txtBGFile.Text = meta["BGFile"];
                        txtBGPos.Text = meta["BGPos"];
                    }
                    else
                    {
                        foreach (var tb in metadataMap.Values)
                            tb.Text = "";
                        txtBGFile.Text = "";
                        txtBGPos.Text = "";
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

            var result = MessageBox.Show($"{osuFiles.Length} 件の .osu ファイルを更新しますか？", "確認", MessageBoxButtons.OKCancel);
            if (result != DialogResult.OK) return;

            try
            {
                if (chkBackup.Checked)
                {
                    foreach (var file in osuFiles)
                    {
                        string relPath = Path.GetRelativePath(selectedFolder, file);
                        string backupPath = Path.Combine(backupDir, Path.GetFileName(selectedFolder), relPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                        File.Copy(file, backupPath, true);
                    }
                }

                // 新しいファイルパスリスト（リネーム後に更新するため）
                List<string> updatedFiles = new List<string>();

                foreach (var originalFile in osuFiles)
                {
                    var file = originalFile; // ループ変数を直接変更しない

                    var (oldArtist, oldTitle, oldCreator) = ReadMetadata(file);

                    string newArtist = txtArtist.Text.Trim();
                    string newTitle = txtTitle.Text.Trim();
                    string newCreator = txtCreator.Text.Trim();

                    // リネーム処理（新しいファイルパスを返す）
                    string newFilePath = RenameFileIfNeeded(file, oldArtist, oldTitle, oldCreator, newArtist, newTitle, newCreator);

                    // ファイルパス更新
                    file = newFilePath;

                    // ファイルの内容を更新
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("Artist:"))
                            lines[i] = "Artist:" + newArtist;
                        else if (lines[i].StartsWith("ArtistUnicode:"))
                            lines[i] = "ArtistUnicode:" + txtRomanisedArtist.Text.Trim();
                        else if (lines[i].StartsWith("Title:"))
                            lines[i] = "Title:" + newTitle;
                        else if (lines[i].StartsWith("TitleUnicode:"))
                            lines[i] = "TitleUnicode:" + txtRomanisedTitle.Text.Trim();
                        else if (lines[i].StartsWith("Creator:"))
                            lines[i] = "Creator:" + newCreator;
                        else if (lines[i].StartsWith("Source:"))
                            lines[i] = "Source:" + txtSource.Text.Trim();
                        else if (lines[i].StartsWith("Tags:"))
                            lines[i] = "Tags:" + txtNewTags.Text.Trim();

                        // BG系の更新
                        if (lines[i].StartsWith("//Background and Video events") && i + 1 < lines.Length)
                        {
                            var parts = lines[i + 1].Split(',');
                            if (parts.Length >= 5)
                            {
                                parts[2] = $"\"{txtBGFile.Text.Trim()}\"";
                                parts[4] = txtBGPos.Text.Trim();
                                lines[i + 1] = string.Join(",", parts);
                            }
                        }
                    }

                    File.WriteAllLines(file, lines, Encoding.UTF8);

                    updatedFiles.Add(file);
                }

                // osuFiles配列を更新
                osuFiles = updatedFiles.ToArray();

                MessageBox.Show("更新しました。", "完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました。", "エラー");
                File.AppendAllText("error.log", $"[{DateTime.Now}] {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private (string Artist, string Title, string Creator) ReadMetadata(string filePath)
        {
            string artist = "";
            string title = "";
            string creator = "";

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("Artist:"))
                    artist = line.Substring("Artist:".Length).Trim();
                else if (line.StartsWith("Title:"))
                    title = line.Substring("Title:".Length).Trim();
                else if (line.StartsWith("Creator:"))
                    creator = line.Substring("Creator:".Length).Trim();

                if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(creator))
                    break;
            }

            return (artist, title, creator);
        }

        private string RenameFileIfNeeded(string filePath, string oldArtist, string oldTitle, string oldCreator,
                                string newArtist, string newTitle, string newCreator)
        {
            if (oldArtist == newArtist && oldTitle == newTitle && oldCreator == newCreator)
                return filePath;

            string dir = Path.GetDirectoryName(filePath)!;
            string oldFileName = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            int diffStart = oldFileName.LastIndexOf('[');
            string diffPart = "";
            if (diffStart != -1)
                diffPart = oldFileName.Substring(diffStart);

            string newFileName = $"{newArtist} - {newTitle} ({newCreator}) {diffPart}{ext}";
            string newFilePath = Path.Combine(dir, newFileName);

            if (!string.Equals(filePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(newFilePath))
                {
                    MessageBox.Show($"リネーム先のファイルが存在します: {newFileName}", "エラー");
                    return filePath;
                }

                File.Move(filePath, newFilePath);
                return newFilePath;
            }

            return filePath;
        }
    }
}
