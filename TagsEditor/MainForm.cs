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
            if (chkFolderMode.Checked)
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
            else
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "osu files (*.osu)|*.osu";
                    dialog.Multiselect = false;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        osuFiles = new string[] { dialog.FileName };
                        selectedFolder = Path.GetDirectoryName(dialog.FileName) ?? "";
                        txtFolderPath.Text = osuFiles[0];
                        LoadMetadata();
                    }
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
                MessageBox.Show("exeフォルダを開く際にエラーが発生しました。\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Backupフォルダを開く際にエラーが発生しました。\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadMetadata()
        {
            Dictionary<string, Control> metadataMap = new Dictionary<string, Control>()
            {
                { "ArtistUnicode:", txtArtist },
                { "Artist:", txtRomanisedArtist },
                { "TitleUnicode:", txtTitle },
                { "Title:", txtRomanisedTitle },
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
                MessageBox.Show(".osuファイルが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 入力文字チェック
            if (!ValidateInputs()) return;

            var result = MessageBox.Show($"{osuFiles.Length} 件の .osu ファイルを更新しますか？", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result != DialogResult.OK) return;

            try
            {
                if (chkBackup.Checked)
                {
                    foreach (var file in osuFiles)
                    {
                        string relPath = "";
                        if (chkFolderMode.Checked)
                            relPath = Path.GetRelativePath(selectedFolder, file);
                        else
                            relPath = Path.GetFileName(file);

                        string backupPath = Path.Combine(backupDir, Path.GetFileName(selectedFolder), relPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                        File.Copy(file, backupPath, true);
                    }
                }

                List<string> updatedFiles = new List<string>();

                foreach (var originalFile in osuFiles)
                {
                    var file = originalFile;

                    // Romanised Artist, Romanised Title, Creatorを取得
                    var (oldRomanisedArtist, oldRomanisedTitle, oldCreator) = ReadRomanisedMetadata(file);

                    string newRomanisedArtist = txtRomanisedArtist.Text.Trim();
                    string newRomanisedTitle = txtRomanisedTitle.Text.Trim();
                    string newCreator = txtCreator.Text.Trim();

                    string newFilePath = RenameFileIfNeeded(file, oldRomanisedArtist, oldRomanisedTitle, oldCreator,
                                                            newRomanisedArtist, newRomanisedTitle, newCreator);

                    file = newFilePath;

                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("Artist:"))
                            lines[i] = "Artist:" + newRomanisedArtist;
                        else if (lines[i].StartsWith("ArtistUnicode:"))
                            lines[i] = "ArtistUnicode:" + txtArtist.Text.Trim();
                        else if (lines[i].StartsWith("Title:"))
                            lines[i] = "Title:" + newRomanisedTitle;
                        else if (lines[i].StartsWith("TitleUnicode:"))
                            lines[i] = "TitleUnicode:" + txtTitle.Text.Trim();
                        else if (lines[i].StartsWith("Creator:"))
                            lines[i] = "Creator:" + newCreator;
                        else if (lines[i].StartsWith("Source:"))
                            lines[i] = "Source:" + txtSource.Text.Trim();
                        else if (lines[i].StartsWith("Tags:"))
                            lines[i] = "Tags:" + txtNewTags.Text.Trim();

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

                osuFiles = updatedFiles.ToArray();

                MessageBox.Show("更新しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました。\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.AppendAllText("error.log", $"[{DateTime.Now}] {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        // 各入力欄に警告(びっくりマーク&音)を出し、Yes→続行/No→中断
        private bool ValidateInputs()
        {
            // Romanised Artist と Romanised Title のASCIIチェック
            if (!IsAsciiAlphaNum(txtRomanisedArtist.Text))
            {
                var result = MessageBox.Show(
                    "Romanised Artist欄に英数字・半角記号以外の文字（日本語等）が含まれています。\n続行しますか？",
                    "警告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (result != DialogResult.Yes) return false;
            }
            if (!IsAsciiAlphaNum(txtRomanisedTitle.Text))
            {
                var result = MessageBox.Show(
                    "Romanised Title欄に英数字・半角記号以外の文字（日本語等）が含まれています。\n続行しますか？",
                    "警告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (result != DialogResult.Yes) return false;
            }

            // すべての入力欄で特殊文字を検出
            string[] allFields = {
                txtArtist.Text,
                txtTitle.Text,
                txtCreator.Text,
                txtSource.Text,
                txtNewTags.Text,
                txtBGFile.Text,
                txtBGPos.Text,
                txtRomanisedArtist.Text,
                txtRomanisedTitle.Text
            };

            foreach (var field in allFields)
            {
                if (ContainsForbiddenUnicode(field, out string foundChar))
                {
                    var warnResult = MessageBox.Show(
                        $"入力欄にosu!で使用できない特殊な文字が含まれています。\n続行しますか？",
                        "警告",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    if (warnResult != DialogResult.Yes) return false;
                }
            }

            // BG File Name の空欄/拡張子チェック（空欄のときは拡張子警告なし）
            if (string.IsNullOrWhiteSpace(txtBGFile.Text))
            {
                var warnResult = MessageBox.Show(
                    "BG File Nameが空欄ですが、このまま続行しますか？",
                    "警告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (warnResult != DialogResult.Yes) return false;
            }
            else
            {
                string lower = txtBGFile.Text.ToLower();
                if (!(lower.EndsWith(".jpg") || lower.EndsWith(".png")))
                {
                    var extResult = MessageBox.Show(
                        "BG File Nameの拡張子を検知できませんでした。拡張子が抜けている可能性があります。続行しますか？",
                        "警告",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    if (extResult != DialogResult.Yes) return false;
                }
            }

            return true;
        }

        // 特殊文字検出（charのみ、16bit超は考慮しなくてOK）
        private bool ContainsForbiddenUnicode(string input, out string foundChar)
        {
            foreach (char c in input)
            {
                if (char.IsControl(c))
                {
                    foundChar = c.ToString();
                    return true;
                }
                if ((c >= '\u2070' && c <= '\u209F') || (c >= '\u1D2C' && c <= '\u1D6A')) // 上付き/下付き/IPA拡張
                {
                    foundChar = c.ToString();
                    return true;
                }
                if (c >= '\uFB00' && c <= '\uFB06') // 合字
                {
                    foundChar = c.ToString();
                    return true;
                }
                if (c >= '\uE000' && c <= '\uF8FF') // Private Use Area
                {
                    foundChar = c.ToString();
                    return true;
                }
            }
            foundChar = "";
            return false;
        }

        // ASCII英数字・半角記号のみ許可（ASCII制御文字以外0x20-0x7E）
        private bool IsAsciiAlphaNum(string input)
        {
            foreach (char c in input)
            {
                if (!(c >= 0x20 && c <= 0x7E)) return false;
            }
            return true;
        }

        // ファイル名用にRomanised Artist/Title/Creatorを取得
        private (string RomanisedArtist, string RomanisedTitle, string Creator) ReadRomanisedMetadata(string filePath)
        {
            string romanisedArtist = "";
            string romanisedTitle = "";
            string creator = "";

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("Artist:"))
                    romanisedArtist = line.Substring("Artist:".Length).Trim();
                else if (line.StartsWith("Title:"))
                    romanisedTitle = line.Substring("Title:".Length).Trim();
                else if (line.StartsWith("Creator:"))
                    creator = line.Substring("Creator:".Length).Trim();

                if (!string.IsNullOrEmpty(romanisedArtist) && !string.IsNullOrEmpty(romanisedTitle) && !string.IsNullOrEmpty(creator))
                    break;
            }

            return (romanisedArtist, romanisedTitle, creator);
        }

        // ファイル名はRomanised Artist - Romanised Title (Creator) [Diff].osu形式にする
        private string RenameFileIfNeeded(string filePath, string oldRomanisedArtist, string oldRomanisedTitle, string oldCreator,
                                  string newRomanisedArtist, string newRomanisedTitle, string newCreator)
        {
            if (oldRomanisedArtist == newRomanisedArtist && oldRomanisedTitle == newRomanisedTitle && oldCreator == newCreator)
                return filePath;

            string dir = Path.GetDirectoryName(filePath)!;
            string oldFileName = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            int diffStart = oldFileName.LastIndexOf('[');
            string diffPart = "";
            if (diffStart != -1)
                diffPart = oldFileName.Substring(diffStart);

            string newFileName = $"{newRomanisedArtist} - {newRomanisedTitle} ({newCreator}) {diffPart}{ext}";
            string newFilePath = Path.Combine(dir, newFileName);

            if (!string.Equals(filePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(newFilePath))
                {
                    MessageBox.Show($"リネーム先のファイルが存在します: {newFileName}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return filePath;
                }

                File.Move(filePath, newFilePath);
                return newFilePath;
            }

            return filePath;
        }
    }
}