using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TagsEditor
{
    public partial class TagsEditor : Form
    {
        private readonly OsuFileService _osuService = new OsuFileService();
        private string selectedFolder = string.Empty;
        private string[] osuFiles = new string[0];
        private readonly List<DiffListItem> diffItems = new List<DiffListItem>();
        private bool ignoreFieldChange = false;

        public TagsEditor()
        {
            InitializeComponent();
            InitializeEventHandlers();
            UpdateControlStates();
        }

        private void InitializeEventHandlers()
        {
            this.Load += new System.EventHandler(this.TagsEditor_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TagsEditor_FormClosing);
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

            日本語ToolStripMenuItem.Click += 日本語ToolStripMenuItem_Click;
            englishToolStripMenuItem.Click += englishToolStripMenuItem_Click;
        }

        #region UI State Management & Events
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

        private void TagsEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.WindowLocation = this.Location;
            Properties.Settings.Default.Save();
        }

        private void chkFolderMode_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkFolderMode.Checked)
            {
                chkEnableIndividualEdit.Checked = false;
            }
            txtFolderPath.Text = "";
            ClearAllFields();
            SetMetadataFieldsEnabled(false);
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

        private void lstDiffs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkEnableIndividualEdit.Checked && lstDiffs.SelectedItem is DiffListItem item)
            {
                PopulateFieldsWithMetadata(item.EditedMetadata);
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

                item.DiffName = txtDifficulty.Text;
                int selectedIndex = lstDiffs.SelectedIndex;
                if (selectedIndex != -1)
                {
                    lstDiffs.Items[selectedIndex] = item;
                }
                lstDiffs.Invalidate();
            }
        }

        private void lstDiffs_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            if (!(lstDiffs.Items[e.Index] is DiffListItem item)) return;

            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color foreColor;
            if (!lstDiffs.Enabled)
            {
                foreColor = SystemColors.GrayText;
            }
            else if (isSelected)
            {
                foreColor = SystemColors.HighlightText;
            }
            else
            {
                foreColor = item.IsEdited ? Color.Red : SystemColors.WindowText;
            }

            TextRenderer.DrawText(e.Graphics, item.ToString(), e.Font, e.Bounds, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            e.DrawFocusRectangle();
        }

        #endregion

        #region Button Click Handlers
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (chkFolderMode.Checked)
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
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
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        osuFiles = new string[] { dialog.FileName };
                        selectedFolder = Path.GetDirectoryName(dialog.FileName) ?? "";
                        txtFolderPath.Text = osuFiles[0];
                        LoadMetadata();
                        diffItems.Clear();
                        lstDiffs.Items.Clear();
                    }
                }
            }
            UpdateControlStates();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var resources = new ComponentResourceManager(typeof(TagsEditor));
            if (osuFiles.Length == 0)
            {
                MessageBox.Show(resources.GetString("osuFNE"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ValidateInputs()) return;

            if (!chkEnableIndividualEdit.Checked)
            {
                int updateTargetCount = CalculateUpdateTargetCount();
                if (updateTargetCount == 0)
                {
                    MessageBox.Show(resources.GetString("notUpdateError"), resources.GetString("InfoTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string message = string.Format(resources.GetString("osuFUpdate"), updateTargetCount);
                var confirmResult = MessageBox.Show(message, resources.GetString("updateTitle"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (confirmResult != DialogResult.OK) return;
            }
            try
            {
                var newMeta = new Metadata
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

                _osuService.ProcessBeatmapUpdate(
                    selectedFolder,
                    osuFiles,
                    newMeta,
                    chkFolderMode.Checked,
                    chkEnableIndividualEdit.Checked,
                    btnuseUpdate.Checked   
                );

                MessageBox.Show(resources.GetString("UpdateOK"), resources.GetString("OKTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (Directory.Exists(selectedFolder))
                {
                    osuFiles = Directory.GetFiles(selectedFolder, "*.osu", SearchOption.AllDirectories);
                }
                LoadMetadata();
                LoadDiffList();
            }
            catch (IOException ex) when (ex.Message == "Exists")
            {
                MessageBox.Show(resources.GetString("Exists"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{resources.GetString("Error")}\n\n{ex.Message}", resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.AppendAllText("error.log", $"[{DateTime.Now}] {ex.ToString()}\n");
            }
        }

        private void btnOpenExeFolder_Click(object sender, EventArgs e)
        {
            var resources = new ComponentResourceManager(typeof(TagsEditor));
            try
            {
                _osuService.OpenFolder(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception)
            {
                MessageBox.Show(resources.GetString("exeOpenError"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenBackupFolder_Click(object sender, EventArgs e)
        {
            var resources = new ComponentResourceManager(typeof(TagsEditor));
            try
            {
                string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
                _osuService.OpenFolder(backupDir);
            }
            catch (Exception)
            {
                MessageBox.Show(resources.GetString("BackupOpenError"), resources.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Helper Methods
        private void LoadMetadata()
        {
            if (osuFiles.Length == 0)
            {
                ClearAllFields();
                SetMetadataFieldsEnabled(false);
                return;
            }
            var firstFileMeta = _osuService.ReadFullMetadata(osuFiles[0]);
            PopulateFieldsWithMetadata(firstFileMeta);
            SetMetadataFieldsEnabled(true);
        }

        private void LoadDiffList()
        {
            lstDiffs.Items.Clear();
            diffItems.Clear();
            if (!chkFolderMode.Checked || osuFiles.Length == 0) return;

            foreach (var file in osuFiles)
            {
                var metadata = _osuService.ReadFullMetadata(file);
                var item = new DiffListItem
                {
                    DiffName = metadata.Difficulty,
                    FilePath = file,
                    IsEdited = false,
                    OriginalMetadata = metadata.Clone(),
                    EditedMetadata = metadata.Clone()
                };
                diffItems.Add(item);
            }
            lstDiffs.Items.AddRange(diffItems.ToArray());
            lstDiffs.DisplayMember = "DiffName";
        }

        private void PopulateFieldsWithMetadata(Metadata meta)
        {
            ignoreFieldChange = true;
            txtArtist.Text = meta.Artist;
            txtRomanisedArtist.Text = meta.RomanisedArtist;
            txtTitle.Text = meta.Title;
            txtRomanisedTitle.Text = meta.RomanisedTitle;
            txtCreator.Text = meta.Creator;
            txtSource.Text = meta.Source;
            txtNewTags.Text = meta.Tags;
            txtBGFile.Text = meta.BGFile;
            txtBGPos.Value = meta.BGPos;
            txtDifficulty.Text = meta.Difficulty;
            ignoreFieldChange = false;
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

        private int CalculateUpdateTargetCount()
        {
            if (chkFolderMode.Checked && chkEnableIndividualEdit.Checked)
            {
                return diffItems.Count(item => item.IsEdited);
            }
            else
            {
                if (osuFiles.Length == 0) return 0;
                var firstFileMeta = _osuService.ReadFullMetadata(osuFiles[0]);
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
                return hasDiff ? osuFiles.Length : 0;
            }
        }

        private void ExecuteUpdate()
        {
            if (chkFolderMode.Checked && chkEnableIndividualEdit.Checked)
            {
                foreach (var item in diffItems.Where(i => i.IsEdited).ToList())
                {
                    _osuService.UpdateSingleFile(item.FilePath, item.EditedMetadata, item.OriginalMetadata, chkFolderMode.Checked, chkEnableIndividualEdit.Checked);
                    item.OriginalMetadata = item.EditedMetadata.Clone();
                    item.IsEdited = false;
                }
                osuFiles = Directory.GetFiles(selectedFolder, "*.osu", SearchOption.AllDirectories);
                LoadDiffList();
            }
            else
            {
                var newMeta = new Metadata
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

                var updatedFiles = new List<string>();
                foreach (var file in osuFiles)
                {
                    var originalMeta = _osuService.ReadFullMetadata(file);
                    string newFilePath = _osuService.UpdateSingleFile(file, newMeta, originalMeta, chkFolderMode.Checked, chkEnableIndividualEdit.Checked);
                    updatedFiles.Add(newFilePath);
                }
                osuFiles = updatedFiles.ToArray();
                LoadMetadata();
                if (chkFolderMode.Checked) LoadDiffList();
            }
        }

        #endregion

        #region Validation (UI-related)
        private bool ValidateInputs()
        {
            var resources = new ComponentResourceManager(typeof(TagsEditor));
            if (!IsAsciiAlphaNum(txtRomanisedArtist.Text))
            {
                var result = MessageBox.Show(resources.GetString("RomArtWarn"), resources.GetString("WarnTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) return false;
            }
            if (!IsAsciiAlphaNum(txtRomanisedTitle.Text))
            {
                var result = MessageBox.Show(resources.GetString("RomTitWarn"), resources.GetString("WarnTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) return false;
            }

            string[] allFields = {
                txtArtist.Text, txtTitle.Text, txtCreator.Text, txtSource.Text, txtDifficulty.Text,
                txtNewTags.Text, txtBGFile.Text, txtBGPos.Value.ToString(),
                txtRomanisedArtist.Text, txtRomanisedTitle.Text
            };

            foreach (var field in allFields)
            {
                if (ContainsForbiddenUnicode(field, out string _))
                {
                    var warnResult = MessageBox.Show(resources.GetString("ForbWarn"), resources.GetString("WarnTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (warnResult != DialogResult.Yes) return false;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(txtBGFile.Text))
            {
                var warnResult = MessageBox.Show(resources.GetString("BGNullWarn"), resources.GetString("WarnTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (warnResult != DialogResult.Yes) return false;
            }
            else
            {
                string lower = txtBGFile.Text.ToLower();
                if (!(lower.EndsWith(".jpg") || lower.EndsWith(".png") || lower.EndsWith(".jpeg")))
                {
                    var extResult = MessageBox.Show(resources.GetString("BGTextWarn"), resources.GetString("WarnTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (extResult != DialogResult.Yes) return false;
                }
            }
            return true;
        }
        private bool ContainsForbiddenUnicode(string input, out string foundChar)
        {
            foreach (char c in input)
            {
                if (char.IsControl(c) || (c >= '\u2070' && c <= '\u209F') || (c >= '\u1D2C' && c <= '\u1D6A') ||
                    (c >= '\uFB00' && c <= '\uFB06') || (c >= '\uE000' && c <= '\uF8FF'))
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
        #endregion

        #region Language Switch

        private void SwitchLanguage(string cultureName)
        {
            Properties.Settings.Default.Language = cultureName;
            Properties.Settings.Default.Save();

            MessageBox.Show("Language settings will be applied after restarting the application.",
                            "Restart required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

            Application.Restart();
            Environment.Exit(0);



            string executablePath = Application.ExecutablePath;

            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restarting application: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void 日本語ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchLanguage("ja-JP");
        }
        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchLanguage("en-US");
        }
        #endregion

        private void TagsEditor_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.WindowLocation != System.Drawing.Point.Empty)
            {
                bool isVisible = false;
                foreach (var screen in Screen.AllScreens)
                {
                    if (screen.WorkingArea.Contains(Properties.Settings.Default.WindowLocation))
                    {
                        isVisible = true;
                        break;
                    }
                }

                if (isVisible)
                {
                    this.Location = Properties.Settings.Default.WindowLocation;
                }
                else
                {
                    this.StartPosition = FormStartPosition.CenterScreen;
                }
            }
        }

        private void btnuseUpdate_Click(object sender, EventArgs e)
        {
            if (btnuseUpdate.Checked)
            {
                chkBackup.Checked = true;

                chkBackup.Enabled = false;
            }
            else
            {
                chkBackup.Enabled = true;
            }
        }
    }
}
