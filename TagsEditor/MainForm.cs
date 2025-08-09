using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        private List<DiffListItem> diffItems = new List<DiffListItem>();
        private bool ignoreFieldChange = false;

        public TagsEditor()
        {
            InitializeComponent();

            // イベント登録
            btnBrowse.Click += btnBrowse_Click;
            btnUpdate.Click += btnUpdate_Click;
            btnOpenExeFolder.Click += btnOpenExeFolder_Click;
            btnOpenBackupFolder.Click += btnOpenBackupFolder_Click;
            chkFolderMode.CheckedChanged += chkFolderMode_CheckedChanged;
            chkEnableIndividualEdit.CheckedChanged += chkEnableIndividualEdit_CheckedChanged;

            lstDiffs.SelectedIndexChanged += lstDiffs_SelectedIndexChanged;
            lstDiffs.DrawMode = DrawMode.OwnerDrawFixed;
            lstDiffs.DrawItem += lstDiffs_DrawItem;

            txtArtist.TextChanged += OnMetadataFieldChanged;
            txtRomanisedArtist.TextChanged += OnMetadataFieldChanged;
            txtTitle.TextChanged += OnMetadataFieldChanged;
            txtRomanisedTitle.TextChanged += OnMetadataFieldChanged;
            txtCreator.TextChanged += OnMetadataFieldChanged;
            txtSource.TextChanged += OnMetadataFieldChanged;
            txtNewTags.TextChanged += OnMetadataFieldChanged;
            txtBGFile.TextChanged += OnMetadataFieldChanged;
            txtBGPos.ValueChanged += OnMetadataFieldChanged;
            txtDifficulty.TextChanged += OnDifficultyChanged;

            // 初期状態のUI設定
            UpdateControlStates();
        }

        // [修正] UIコントロールの状態を更新するメソッドを新設
        private void UpdateControlStates()
        {
            bool isFolderMode = chkFolderMode.Checked;
            bool isIndividualEdit = chkEnableIndividualEdit.Checked;

            chkEnableIndividualEdit.Enabled = isFolderMode;
            lstDiffs.Enabled = isFolderMode && isIndividualEdit;

            // Difficulty, BG File, BG Pos の有効/無効状態を制御
            bool canEditSpecificFields = !isFolderMode || isIndividualEdit;
            txtDifficulty.Enabled = canEditSpecificFields;
            txtBGFile.Enabled = canEditSpecificFields;
            txtBGPos.Enabled = canEditSpecificFields;

            lstDiffs.Invalidate(); // ListBoxを再描画
        }

        private void chkFolderMode_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkFolderMode.Checked)
            {
                chkEnableIndividualEdit.Checked = false;
            }
            txtFolderPath.Text = "";
            ClearAllFields();
            osuFiles = new string[0];
            diffItems.Clear();
            lstDiffs.Items.Clear();
            UpdateControlStates();
        }

        private void chkEnableIndividualEdit_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStates();
            // 個別編集が無効になったら、最初のファイルのメタデータを再読み込み
            if (!chkEnableIndividualEdit.Checked && osuFiles.Length > 0)
            {
                LoadMetadata();
            }
        }

        private void SetMetadataFieldsEnabled(bool enabled)
        {
            txtArtist.Enabled = enabled;
            txtRomanisedArtist.Enabled = enabled;
            txtTitle.Enabled = enabled;
            txtRomanisedTitle.Enabled = enabled;
            txtCreator.Enabled = enabled;
            txtSource.Enabled = enabled;
            txtNewTags.Enabled = enabled;

            // [修正] このメソッドは共通項目のみを制御するように変更
            // txtBGFile, txtBGPos, txtDifficultyはUpdateControlStatesで制御
        }

        private void ClearAllFields()
        {
            ignoreFieldChange = true;
            txtArtist.Text = "";
            txtRomanisedArtist.Text = "";
            txtTitle.Text = "";
            txtRomanisedTitle.Text = "";
            txtCreator.Text = "";
            txtSource.Text = "";
            txtNewTags.Text = "";
            txtBGFile.Text = "";
            txtBGPos.Value = 0;
            txtDifficulty.Text = "";
            ignoreFieldChange = false;
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
                        LoadDiffList();
                    }
                }
            }
            else
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "osu! file (*.osu)|*.osu";
                    dialog.Multiselect = false;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        osuFiles = new string[] { dialog.FileName };
                        selectedFolder = Path.GetDirectoryName(dialog.FileName) ?? "";
                        txtFolderPath.Text = osuFiles[0];
                        LoadMetadata();
                        lstDiffs.Items.Clear();
                        diffItems.Clear();
                    }
                }
            }
            UpdateControlStates();
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

        private bool ValidateInputs()
        {
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

            string[] allFields = {
                txtArtist.Text,
                txtTitle.Text,
                txtCreator.Text,
                txtSource.Text,
                txtDifficulty.Text,
                txtNewTags.Text,
                txtBGFile.Text,
                txtBGPos.Value.ToString(),
                txtRomanisedArtist.Text,
                txtRomanisedTitle.Text
            };

            foreach (var field in allFields)
            {
                if (ContainsForbiddenUnicode(field, out string foundChar))
                {
                    var warnResult = MessageBox.Show(
                        $"入力欄にosu!で使用できない特殊な文字「{foundChar}」が含まれています。\n続行しますか？",
                        "警告",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    if (warnResult != DialogResult.Yes) return false;
                }
            }

            if (string.IsNullOrWhiteSpace(txtBGFile.Text))
            {
                var warnResult = MessageBox.Show(
                    "BG File Nameが空欄ですが、このままで続行しますか？",
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
                        "BG File Nameの拡張子が.jpgか.pngではありません。拡張子が抜けている可能性があります。続行しますか？",
                        "警告",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    if (extResult != DialogResult.Yes) return false;
                }
            }

            return true;
        }

        private bool ContainsForbiddenUnicode(string input, out string foundChar)
        {
            foreach (char c in input)
            {
                if (char.IsControl(c))
                {
                    foundChar = c.ToString();
                    return true;
                }
                if ((c >= '\u2070' && c <= '\u209F') || (c >= '\u1D2C' && c <= '\u1D6A'))
                {
                    foundChar = c.ToString();
                    return true;
                }
                if (c >= '\uFB00' && c <= '\uFB06')
                {
                    foundChar = c.ToString();
                    return true;
                }
                if (c >= '\uE000' && c <= '\uF8FF')
                {
                    foundChar = c.ToString();
                    return true;
                }
            }
            foundChar = "";
            return false;
        }

        private bool IsAsciiAlphaNum(string input)
        {
            foreach (char c in input)
            {
                if (!(c >= 0x20 && c <= 0x7E)) return false;
            }
            return true;
        }
        private void LoadMetadata()
        {
            if (osuFiles.Length == 0)
            {
                ClearAllFields();
                return;
            }

            // [修正] ロジックを簡略化。常に最初のファイルのメタデータを読み込む
            var firstFileMeta = ReadFullMetadata(osuFiles[0]);

            ignoreFieldChange = true;
            txtArtist.Text = firstFileMeta.Artist;
            txtRomanisedArtist.Text = firstFileMeta.RomanisedArtist;
            txtTitle.Text = firstFileMeta.Title;
            txtRomanisedTitle.Text = firstFileMeta.RomanisedTitle;
            txtCreator.Text = firstFileMeta.Creator;
            txtSource.Text = firstFileMeta.Source;
            txtNewTags.Text = firstFileMeta.Tags;
            txtBGFile.Text = firstFileMeta.BGFile;
            txtBGPos.Value = firstFileMeta.BGPos;
            txtDifficulty.Text = firstFileMeta.Difficulty;
            ignoreFieldChange = false;

            // 全ての共通フィールドを有効にする
            SetMetadataFieldsEnabled(true);
        }

        private string ExtractDiffName(string filePath)
        {
            var lines = File.ReadLines(filePath);
            var versionLine = lines.FirstOrDefault(l => l.StartsWith("Version:"));
            if (versionLine != null)
            {
                return versionLine.Substring("Version:".Length).Trim();
            }

            // Fallback for safety
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            int open = fileName.IndexOf('[');
            int close = fileName.LastIndexOf(']');
            if (open == -1 || close == -1 || close < open) return fileName;
            return fileName.Substring(open + 1, close - open - 1);
        }

        private void LoadDiffList()
        {
            lstDiffs.Items.Clear();
            diffItems.Clear();
            if (!chkFolderMode.Checked || osuFiles.Length == 0)
                return;

            foreach (var file in osuFiles)
            {
                var metadata = ReadFullMetadata(file);
                var item = new DiffListItem()
                {
                    DiffName = metadata.Difficulty, // [修正] ファイル名ではなく、Versionタグから取得
                    FilePath = file,
                    IsEdited = false,
                    OriginalMetadata = metadata.Clone(),
                    EditedMetadata = metadata.Clone()
                };
                diffItems.Add(item);
                lstDiffs.Items.Add(item);
            }
            lstDiffs.DisplayMember = "DiffName";
        }

        private class DiffListItem
        {
            public string DiffName { get; set; }
            public string FilePath { get; set; }
            public bool IsEdited { get; set; }
            public Metadata OriginalMetadata { get; set; }
            public Metadata EditedMetadata { get; set; }
            public override string ToString() => DiffName;
        }

        private class Metadata
        {
            public string Artist { get; set; } = "";
            public string RomanisedArtist { get; set; } = "";
            public string Title { get; set; } = "";
            public string RomanisedTitle { get; set; } = "";
            public string Creator { get; set; } = "";
            public string Source { get; set; } = "";
            public string Tags { get; set; } = "";
            public string Difficulty { get; set; } = "";
            public string BGFile { get; set; } = "";
            public decimal BGPos { get; set; } = 0;

            public Metadata Clone()
            {
                return (Metadata)MemberwiseClone();
            }
            public bool IsSame(Metadata other)
            {
                return Artist == other.Artist &&
                       RomanisedArtist == other.RomanisedArtist &&
                       Title == other.Title &&
                       RomanisedTitle == other.RomanisedTitle &&
                       Creator == other.Creator &&
                       Source == other.Source &&
                       Tags == other.Tags &&
                       Difficulty == other.Difficulty &&
                       BGFile == other.BGFile &&
                       BGPos == other.BGPos;
            }
        }

        private string RenameFileIfNeeded(
            string filePath,
            string oldRomanisedArtist, string oldRomanisedTitle, string oldCreator, string oldDiffName,
            string newRomanisedArtist, string newRomanisedTitle, string newCreator, string newDiffName)
        {
            // [修正] Sanitize characters forbidden in filenames
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string Sanitize(string input) => string.Concat(input.Split(invalidChars));

            newRomanisedArtist = Sanitize(newRomanisedArtist);
            newRomanisedTitle = Sanitize(newRomanisedTitle);
            newCreator = Sanitize(newCreator);
            newDiffName = Sanitize(newDiffName);

            if (oldRomanisedArtist == newRomanisedArtist &&
                oldRomanisedTitle == newRomanisedTitle &&
                oldCreator == newCreator &&
                oldDiffName == newDiffName)
                return filePath;

            string dir = Path.GetDirectoryName(filePath)!;
            string ext = Path.GetExtension(filePath);

            string newFileName = $"{newRomanisedArtist} - {newRomanisedTitle} ({newCreator}) [{newDiffName}]{ext}";
            string newFilePath = Path.Combine(dir, newFileName);

            if (!string.Equals(filePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(newFilePath))
                {
                    MessageBox.Show($"リネーム先のファイルが既に存在します: {newFileName}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return filePath;
                }
                File.Move(filePath, newFilePath);
                return newFilePath;
            }
            return filePath;
        }

        private Metadata ReadFullMetadata(string filePath)
        {
            var meta = new Metadata();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (line.StartsWith("ArtistUnicode:"))
                    meta.Artist = line.Substring("ArtistUnicode:".Length).Trim();
                else if (line.StartsWith("Artist:"))
                    meta.RomanisedArtist = line.Substring("Artist:".Length).Trim();
                else if (line.StartsWith("TitleUnicode:"))
                    meta.Title = line.Substring("TitleUnicode:".Length).Trim();
                else if (line.StartsWith("Title:"))
                    meta.RomanisedTitle = line.Substring("Title:".Length).Trim();
                else if (line.StartsWith("Creator:"))
                    meta.Creator = line.Substring("Creator:".Length).Trim();
                else if (line.StartsWith("Source:"))
                    meta.Source = line.Substring("Source:".Length).Trim();
                else if (line.StartsWith("Version:"))
                    meta.Difficulty = line.Substring("Version:".Length).Trim();
                else if (line.StartsWith("Tags:"))
                    meta.Tags = line.Substring("Tags:".Length).Trim();
            }

            int bgSectionIndex = Array.FindIndex(lines, l => l.StartsWith("//Background and Video events"));
            if (bgSectionIndex != -1 && bgSectionIndex + 1 < lines.Length)
            {
                var parts = lines[bgSectionIndex + 1].Split(',');
                if (parts.Length >= 5 && (parts[0] == "0" || parts[0] == "Background"))
                {
                    meta.BGFile = parts[2].Trim().Trim('"');
                    decimal.TryParse(parts[4].Trim(), out decimal posVal);
                    meta.BGPos = posVal;
                }
            }
            return meta;
        }

        private void lstDiffs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkEnableIndividualEdit.Checked && lstDiffs.SelectedItem is DiffListItem item)
            {
                ignoreFieldChange = true;
                txtArtist.Text = item.EditedMetadata.Artist;
                txtRomanisedArtist.Text = item.EditedMetadata.RomanisedArtist;
                txtTitle.Text = item.EditedMetadata.Title;
                txtRomanisedTitle.Text = item.EditedMetadata.RomanisedTitle;
                txtCreator.Text = item.EditedMetadata.Creator;
                txtSource.Text = item.EditedMetadata.Source;
                txtNewTags.Text = item.EditedMetadata.Tags;
                txtBGFile.Text = item.EditedMetadata.BGFile;
                txtBGPos.Value = item.EditedMetadata.BGPos;
                txtDifficulty.Text = item.EditedMetadata.Difficulty;
                SetMetadataFieldsEnabled(true);
                ignoreFieldChange = false;
            }
        }

        private void OnMetadataFieldChanged(object sender, EventArgs e)
        {
            if (ignoreFieldChange) return;
            if (chkEnableIndividualEdit.Checked && lstDiffs.SelectedItem is DiffListItem item)
            {
                item.EditedMetadata.Artist = txtArtist.Text;
                item.EditedMetadata.RomanisedArtist = txtRomanisedArtist.Text;
                item.EditedMetadata.Title = txtTitle.Text;
                item.EditedMetadata.RomanisedTitle = txtRomanisedTitle.Text;
                item.EditedMetadata.Creator = txtCreator.Text;
                item.EditedMetadata.Source = txtSource.Text;
                item.EditedMetadata.Tags = txtNewTags.Text;
                item.EditedMetadata.Difficulty = txtDifficulty.Text;
                item.EditedMetadata.BGFile = txtBGFile.Text;
                item.EditedMetadata.BGPos = txtBGPos.Value;

                item.IsEdited = !item.OriginalMetadata.IsSame(item.EditedMetadata);
                lstDiffs.Invalidate();
            }
        }

        private void OnDifficultyChanged(object sender, EventArgs e)
        {
            if (ignoreFieldChange) return;

            // 個別編集モードの場合のみDiffNameを更新
            if (chkFolderMode.Checked && chkEnableIndividualEdit.Checked && lstDiffs.SelectedItem is DiffListItem item)
            {
                item.EditedMetadata.Difficulty = txtDifficulty.Text;
                item.IsEdited = !item.OriginalMetadata.IsSame(item.EditedMetadata);
                // リストボックスの表示テキストを更新するために、アイテムを一度削除して再追加
                int selectedIndex = lstDiffs.SelectedIndex;
                if (selectedIndex != -1)
                {
                    item.DiffName = txtDifficulty.Text;
                    lstDiffs.Items[selectedIndex] = item;
                }
                lstDiffs.Invalidate();
            }
        }

        private void lstDiffs_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var item = lstDiffs.Items[e.Index] as DiffListItem;
            if (item == null) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            e.DrawBackground();

            Color foreColor;
            if (!chkEnableIndividualEdit.Checked)
            {
                foreColor = Color.Gray;
            }
            else if (isSelected)
            {
                foreColor = SystemColors.HighlightText;
            }
            else
            {
                foreColor = item.IsEdited ? Color.Red : Color.Black;
            }

            TextRenderer.DrawText(e.Graphics, item.ToString(), e.Font, e.Bounds, foreColor, TextFormatFlags.Left);
            e.DrawFocusRectangle();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (osuFiles.Length == 0)
            {
                MessageBox.Show(".osuファイルが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ValidateInputs()) return;

            int updateTargetCount = 0;
            if (chkEnableIndividualEdit.Checked && chkFolderMode.Checked)
            {
                updateTargetCount = diffItems.Count(item => item.IsEdited);
            }
            else
            {
                // 一括更新の場合、最低1つのフィールドが変更されているかチェック
                var firstFileMeta = ReadFullMetadata(osuFiles[0]);
                bool hasDiff =
                    firstFileMeta.Artist != txtArtist.Text.Trim() ||
                    firstFileMeta.RomanisedArtist != txtRomanisedArtist.Text.Trim() ||
                    firstFileMeta.Title != txtTitle.Text.Trim() ||
                    firstFileMeta.RomanisedTitle != txtRomanisedTitle.Text.Trim() ||
                    firstFileMeta.Creator != txtCreator.Text.Trim() ||
                    firstFileMeta.Source != txtSource.Text.Trim() ||
                    firstFileMeta.Tags != txtNewTags.Text.Trim();

                // 単体ファイルモードの場合、追加でチェック
                if (!chkFolderMode.Checked)
                {
                    hasDiff |= firstFileMeta.Difficulty != txtDifficulty.Text.Trim() ||
                               firstFileMeta.BGFile != txtBGFile.Text.Trim() ||
                               firstFileMeta.BGPos != txtBGPos.Value;
                }

                if (hasDiff) updateTargetCount = osuFiles.Length;
            }

            if (updateTargetCount == 0)
            {
                MessageBox.Show("ファイルの内容に変更がありません。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"{updateTargetCount} 件の .osu ファイルを更新しますか？", "確認",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (confirmResult != DialogResult.OK) return;

            try
            {
                if (chkBackup.Checked)
                {
                    string backupSubFolder = Path.Combine(backupDir, Path.GetFileName(selectedFolder) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                    foreach (var file in osuFiles)
                    {
                        string relPath = Path.GetRelativePath(selectedFolder, file);
                        string backupPath = Path.Combine(backupSubFolder, relPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                        File.Copy(file, backupPath, true);
                    }
                }

                List<string> updatedFiles = new List<string>();

                if (chkEnableIndividualEdit.Checked && chkFolderMode.Checked)
                {
                    // 個別編集モード
                    foreach (var item in diffItems.Where(i => i.IsEdited).ToList())
                    {
                        UpdateSingleFile(item.FilePath, item.EditedMetadata, item.OriginalMetadata);
                        item.OriginalMetadata = item.EditedMetadata.Clone();
                        item.IsEdited = false;
                    }
                    osuFiles = Directory.GetFiles(selectedFolder, "*.osu", SearchOption.AllDirectories);
                    LoadDiffList(); // リストを再構築
                }
                else
                {
                    // 一括更新モード or 単体ファイルモード
                    Metadata newMeta = new Metadata
                    {
                        Artist = txtArtist.Text.Trim(),
                        RomanisedArtist = txtRomanisedArtist.Text.Trim(),
                        Title = txtTitle.Text.Trim(),
                        RomanisedTitle = txtRomanisedTitle.Text.Trim(),
                        Creator = txtCreator.Text.Trim(),
                        Source = txtSource.Text.Trim(),
                        Tags = txtNewTags.Text.Trim(),
                        // 以下は単体ファイルモードでのみ使用される
                        Difficulty = txtDifficulty.Text.Trim(),
                        BGFile = txtBGFile.Text.Trim(),
                        BGPos = txtBGPos.Value
                    };

                    foreach (var file in osuFiles)
                    {
                        var originalMeta = ReadFullMetadata(file);
                        updatedFiles.Add(UpdateSingleFile(file, newMeta, originalMeta));
                    }
                    osuFiles = updatedFiles.ToArray();
                    LoadMetadata(); // UIを更新
                    if (chkFolderMode.Checked) LoadDiffList();
                }

                MessageBox.Show("更新が完了しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました。\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.AppendAllText("error.log", $"[{DateTime.Now}] {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        // [修正] ファイル更新ロジックを別メソッドに分離
        private string UpdateSingleFile(string filePath, Metadata newMeta, Metadata originalMeta)
        {
            string newRomanisedArtist = newMeta.RomanisedArtist;
            string newRomanisedTitle = newMeta.RomanisedTitle;
            string newCreator = newMeta.Creator;

            // [修正] Difficulty, BGFile, BGPos はモードによって更新するかどうかを決定
            bool canUpdateSpecifics = !chkFolderMode.Checked || chkEnableIndividualEdit.Checked;

            string newDiffName = canUpdateSpecifics ? newMeta.Difficulty : originalMeta.Difficulty;
            string newBgFile = canUpdateSpecifics ? newMeta.BGFile : originalMeta.BGFile;
            decimal newBgPos = canUpdateSpecifics ? newMeta.BGPos : originalMeta.BGPos;

            // ファイル名のリネーム
            string newFilePath = RenameFileIfNeeded(
                filePath,
                originalMeta.RomanisedArtist, originalMeta.RomanisedTitle, originalMeta.Creator, originalMeta.Difficulty,
                newRomanisedArtist, newRomanisedTitle, newCreator, newDiffName
            );

            var lines = File.ReadAllLines(newFilePath).ToList();

            // メタデータ更新
            UpdateLine(lines, "Artist:", newRomanisedArtist);
            UpdateLine(lines, "ArtistUnicode:", newMeta.Artist);
            UpdateLine(lines, "Title:", newRomanisedTitle);
            UpdateLine(lines, "TitleUnicode:", newMeta.Title);
            UpdateLine(lines, "Creator:", newCreator);
            UpdateLine(lines, "Source:", newMeta.Source);
            UpdateLine(lines, "Tags:", newMeta.Tags);
            UpdateLine(lines, "Version:", newDiffName);

            // 背景情報更新
            int bgSectionIndex = lines.FindIndex(l => l.StartsWith("//Background and Video events"));
            if (bgSectionIndex != -1 && bgSectionIndex + 1 < lines.Count)
            {
                var parts = lines[bgSectionIndex + 1].Split(',').ToList();
                if (parts.Count >= 5 && (parts[0] == "0" || parts[0] == "Background"))
                {
                    parts[2] = $"\"{newBgFile}\"";
                    parts[4] = newBgPos.ToString();
                    lines[bgSectionIndex + 1] = string.Join(",", parts);
                }
            }

            File.WriteAllLines(newFilePath, lines, Encoding.UTF8);
            return newFilePath;
        }

        // [修正] 行を効率的に更新するためのヘルパーメソッド
        private void UpdateLine(List<string> lines, string key, string value)
        {
            int index = lines.FindIndex(l => l.StartsWith(key));
            if (index != -1)
            {
                // " " を削除し、キーと値を直接結合する
                lines[index] = key + value;
            }
        }
        private void SwitchLanguage(string cultureName)
        {
            // 設定を保存
            Properties.Settings.Default.Language = cultureName;
            Properties.Settings.Default.Save();

            // 再起動を促すメッセージ (これもリソース化するのが望ましい)
            MessageBox.Show("Language settings will be applied after restarting the application.", "Restart required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // アプリケーションを再起動する
            Application.Restart();
        }

        private void 日本語ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchLanguage("ja-JP");
        }

        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchLanguage("en-US");
        }

        private void TagsEditor_Load(object sender, EventArgs e)
        {

        }
    }
}