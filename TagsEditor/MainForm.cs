using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MetroFramework.Properties;

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

            UpdateControlStates();
        }

        private void UpdateControlStates()
        {
            bool isFolderMode = chkFolderMode.Checked;
            bool isIndividualEdit = chkEnableIndividualEdit.Checked;

            chkEnableIndividualEdit.Enabled = isFolderMode;
            lstDiffs.Enabled = isFolderMode && isIndividualEdit;

            bool canEditSpecificFields = !isFolderMode || isIndividualEdit;
            txtDifficulty.Enabled = canEditSpecificFields;
            txtBGFile.Enabled = canEditSpecificFields;
            txtBGPos.Enabled = canEditSpecificFields;

            lstDiffs.Invalidate();  
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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(TagsEditor));
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
                MessageBox.Show(resources.GetString("exeOpenError"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenBackupFolder_Click(object sender, EventArgs e)
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(TagsEditor));
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
                MessageBox.Show(resources.GetString("BackupOpenError"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateInputs()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(TagsEditor));
            if (!IsAsciiAlphaNum(txtRomanisedArtist.Text))
            {
                var result = MessageBox.Show(
                    resources.GetString("RomArtWarn"),
                    resources.GetString("WarnTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (result != DialogResult.Yes) return false;
            }
            if (!IsAsciiAlphaNum(txtRomanisedTitle.Text))
            {
                var result = MessageBox.Show(
                    resources.GetString("RomTitWarn"),
                    resources.GetString("WarnTitle"),
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
                    resources.GetString("ForbWarn"),
                    resources.GetString("WarnTitle"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    if (warnResult != DialogResult.Yes) return false;
                }
            }

            if (string.IsNullOrWhiteSpace(txtBGFile.Text))
            {
                var warnResult = MessageBox.Show(
                    resources.GetString("BGNullWarn"),
                    resources.GetString("WarnTitle"),
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
                    resources.GetString("BGTextWarn"),
                    resources.GetString("WarnTitle"),
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
                    DiffName = metadata.Difficulty,   
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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(TagsEditor));
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
                    MessageBox.Show(resources.GetString("Exists"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            if (chkFolderMode.Checked && chkEnableIndividualEdit.Checked && lstDiffs.SelectedItem is DiffListItem item)
            {
                item.EditedMetadata.Difficulty = txtDifficulty.Text;
                item.IsEdited = !item.OriginalMetadata.IsSame(item.EditedMetadata);
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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(TagsEditor));
            if (osuFiles.Length == 0)
            {
                MessageBox.Show(resources.GetString("osuFNE"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                var firstFileMeta = ReadFullMetadata(osuFiles[0]);
                bool hasDiff =
                    firstFileMeta.Artist != txtArtist.Text.Trim() ||
                    firstFileMeta.RomanisedArtist != txtRomanisedArtist.Text.Trim() ||
                    firstFileMeta.Title != txtTitle.Text.Trim() ||
                    firstFileMeta.RomanisedTitle != txtRomanisedTitle.Text.Trim() ||
                    firstFileMeta.Creator != txtCreator.Text.Trim() ||
                    firstFileMeta.Source != txtSource.Text.Trim() ||
                    firstFileMeta.Tags != txtNewTags.Text.Trim();

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
                MessageBox.Show(resources.GetString("notUpdateError"), resources.GetString("InfoTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string messageFormat = resources.GetString("osuFUpdate");
            string message = string.Format(messageFormat, updateTargetCount);
            string title = resources.GetString("updateTitle");

            var confirmResult = MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

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
                    foreach (var item in diffItems.Where(i => i.IsEdited).ToList())
                    {
                        updatedFiles.Add(UpdateSingleFile(item.FilePath, item.EditedMetadata, item.OriginalMetadata));
                        item.OriginalMetadata = item.EditedMetadata.Clone();
                        item.IsEdited = false;
                    }
                    osuFiles = Directory.GetFiles(selectedFolder, "*.osu", SearchOption.AllDirectories);
                    LoadDiffList();
                }
                else
                {
                    Metadata newMeta = new Metadata
                    {
                        Artist = txtArtist.Text.Trim(),
                        RomanisedArtist = txtRomanisedArtist.Text.Trim(),
                        Title = txtTitle.Text.Trim(),
                        RomanisedTitle = txtRomanisedTitle.Text.Trim(),
                        Creator = txtCreator.Text.Trim(),
                        Source = txtSource.Text.Trim(),
                        Tags = txtNewTags.Text.Trim(),
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
                    LoadMetadata();
                    if (chkFolderMode.Checked) LoadDiffList();
                }

                MessageBox.Show(resources.GetString("UpdateOK"), resources.GetString("OKTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                try
                {
                    if (Directory.Exists(selectedFolder))
                    {
                        TriggerOsuFileRefresh(selectedFolder);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(resources.GetString("osuUpdateError"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(resources.GetString("Error"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.AppendAllText("error.log", $"[{DateTime.Now}] {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private string UpdateSingleFile(string filePath, Metadata newMeta, Metadata originalMeta)
        {
            string newRomanisedArtist = newMeta.RomanisedArtist;
            string newRomanisedTitle = newMeta.RomanisedTitle;
            string newCreator = newMeta.Creator;

            bool canUpdateSpecifics = !chkFolderMode.Checked || chkEnableIndividualEdit.Checked;

            string newDiffName = canUpdateSpecifics ? newMeta.Difficulty : originalMeta.Difficulty;
            string newBgFile = canUpdateSpecifics ? newMeta.BGFile : originalMeta.BGFile;
            decimal newBgPos = canUpdateSpecifics ? newMeta.BGPos : originalMeta.BGPos;

            string newFilePath = RenameFileIfNeeded(
                filePath,
                originalMeta.RomanisedArtist, originalMeta.RomanisedTitle, originalMeta.Creator, originalMeta.Difficulty,
                newRomanisedArtist, newRomanisedTitle, newCreator, newDiffName
            );

            var lines = File.ReadAllLines(newFilePath).ToList();

            UpdateLine(lines, "Artist:", newRomanisedArtist);
            UpdateLine(lines, "ArtistUnicode:", newMeta.Artist);
            UpdateLine(lines, "Title:", newRomanisedTitle);
            UpdateLine(lines, "TitleUnicode:", newMeta.Title);
            UpdateLine(lines, "Creator:", newCreator);
            UpdateLine(lines, "Source:", newMeta.Source);
            UpdateLine(lines, "Tags:", newMeta.Tags);
            UpdateLine(lines, "Version:", newDiffName);

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

        private void UpdateLine(List<string> lines, string key, string value)
        {
            int index = lines.FindIndex(l => l.StartsWith(key));
            if (index != -1)
            {
                lines[index] = key + value;
            }
        }
        private void SwitchLanguage(string cultureName)
        {
            Properties.Settings.Default.Language = cultureName;
            Properties.Settings.Default.Save();

            MessageBox.Show("Language settings will be applied after restarting the application.", "Restart required", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        private void TriggerOsuFileRefresh(string beatmapDirectory)
        {
            string tempFileName = "tmp.tmp";
            string tempFilePath = Path.Combine(beatmapDirectory, tempFileName);

            try
            {
                File.Create(tempFilePath).Close();
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }
}