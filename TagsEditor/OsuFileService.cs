using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;   
using System.Linq;
using System.Text;

namespace TagsEditor
{
    public class OsuFileService
    {
        private readonly string _backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

        public void ProcessBeatmapUpdate(string selectedFolder, string[] osuFiles, Metadata newMeta, bool isFolderMode, bool isIndividualEdit, bool useAutoUpdate)
        {
            CreateBackup(selectedFolder, osuFiles);

            if (useAutoUpdate && !string.IsNullOrEmpty(selectedFolder))
            {
                CreateAndExecuteOsz(selectedFolder, newMeta, isFolderMode, isIndividualEdit);
            }
            else
            {
                foreach (var file in osuFiles)
                {
                    var originalMeta = ReadFullMetadata(file);
                    UpdateSingleFile(file, newMeta, originalMeta, isFolderMode, isIndividualEdit);
                }

                string refreshDir = !string.IsNullOrEmpty(selectedFolder)
                    ? selectedFolder
                    : (osuFiles.Length > 0 ? Path.GetDirectoryName(osuFiles[0]) : null);

                if (refreshDir != null)
                {
                    TriggerOsuFileRefresh(refreshDir);
                }
            }
        }

        private void CreateAndExecuteOsz(string selectedFolder, Metadata newMeta, bool isFolderMode, bool isIndividualEdit)
        {
            string tempWorkingDir = Path.Combine(Path.GetTempPath(), "TagsEditor_" + Guid.NewGuid().ToString());
            string tempBeatmapDir = Path.Combine(tempWorkingDir, Path.GetFileName(selectedFolder));
            Directory.CreateDirectory(tempBeatmapDir);

            var originalOsuFiles = Directory.GetFiles(selectedFolder, "*.osu");
            var originalMetadataMap = new Dictionary<string, Metadata>();
            foreach (var file in originalOsuFiles)
            {
                originalMetadataMap[Path.GetFileName(file)] = ReadFullMetadata(file);
            }

            try
            {
                foreach (var filePath in Directory.GetFiles(selectedFolder))
                {
                    var fileName = Path.GetFileName(filePath);
                    File.Copy(filePath, Path.Combine(tempBeatmapDir, fileName));
                }

                foreach (var osuFile in originalOsuFiles)
                {
                    File.Delete(osuFile);
                }

                var tempOsuFiles = Directory.GetFiles(tempBeatmapDir, "*.osu");
                foreach (var tempOsuFile in tempOsuFiles)
                {
                    string fileName = Path.GetFileName(tempOsuFile);
                    if (originalMetadataMap.TryGetValue(fileName, out Metadata originalMeta))
                    {
                        UpdateSingleFile(tempOsuFile, newMeta, originalMeta, isFolderMode, isIndividualEdit);
                    }
                }

                string oszPath = Path.Combine(tempWorkingDir, Path.GetFileName(selectedFolder) + ".osz");
                ZipFile.CreateFromDirectory(tempBeatmapDir, oszPath);

                Process.Start(new ProcessStartInfo(oszPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create and execute .osz file: {ex.Message}");
                throw;  
            }
        }


        public Metadata ReadFullMetadata(string filePath)
        {
            var meta = new Metadata();
            if (!File.Exists(filePath)) return meta;

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (line.StartsWith("ArtistUnicode:")) meta.Artist = GetValue(line, "ArtistUnicode:");
                else if (line.StartsWith("Artist:")) meta.RomanisedArtist = GetValue(line, "Artist:");
                else if (line.StartsWith("TitleUnicode:")) meta.Title = GetValue(line, "TitleUnicode:");
                else if (line.StartsWith("Title:")) meta.RomanisedTitle = GetValue(line, "Title:");
                else if (line.StartsWith("Creator:")) meta.Creator = GetValue(line, "Creator:");
                else if (line.StartsWith("Source:")) meta.Source = GetValue(line, "Source:");
                else if (line.StartsWith("Version:")) meta.Difficulty = GetValue(line, "Version:");
                else if (line.StartsWith("Tags:")) meta.Tags = GetValue(line, "Tags:");
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

        public string UpdateSingleFile(string filePath, Metadata newMeta, Metadata originalMeta, bool isFolderMode, bool isIndividualEdit)
        {
            string newFilePath = RenameFileIfNeeded(filePath, originalMeta, newMeta, isFolderMode, isIndividualEdit);
            var lines = File.ReadAllLines(newFilePath).ToList();

            UpdateLine(lines, "Artist:", newMeta.RomanisedArtist);
            UpdateLine(lines, "ArtistUnicode:", newMeta.Artist);
            UpdateLine(lines, "Title:", newMeta.RomanisedTitle);
            UpdateLine(lines, "TitleUnicode:", newMeta.Title);
            UpdateLine(lines, "Creator:", newMeta.Creator);
            UpdateLine(lines, "Source:", newMeta.Source);
            UpdateLine(lines, "Tags:", newMeta.Tags);

            bool canUpdateSpecifics = !isFolderMode || isIndividualEdit;
            string newDiffName = canUpdateSpecifics ? newMeta.Difficulty : originalMeta.Difficulty;
            string newBgFile = canUpdateSpecifics ? newMeta.BGFile : originalMeta.BGFile;
            decimal newBgPos = canUpdateSpecifics ? newMeta.BGPos : originalMeta.BGPos;

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

        public void CreateBackup(string selectedFolder, string[] osuFiles)
        {
            if (string.IsNullOrEmpty(selectedFolder) || osuFiles == null || osuFiles.Length == 0) return;

            string backupSubFolder = Path.Combine(_backupDir, Path.GetFileName(selectedFolder) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            foreach (var file in osuFiles)
            {
                if (file.StartsWith(selectedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    string relPath = Path.GetRelativePath(selectedFolder, file);
                    string backupPath = Path.Combine(backupSubFolder, relPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    File.Copy(file, backupPath, true);
                }
            }
        }

        public void OpenFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public void TriggerOsuFileRefresh(string beatmapDirectory)
        {
            if (!Directory.Exists(beatmapDirectory)) return;

            string tempFilePath = Path.Combine(beatmapDirectory, "tagseditor.tmp");
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

        private string GetValue(string line, string key) => line.Substring(key.Length).Trim();

        private void UpdateLine(List<string> lines, string key, string value)
        {
            int index = lines.FindIndex(l => l.StartsWith(key));
            if (index != -1)
            {
                lines[index] = key + " " + value;
            }
        }

        private string RenameFileIfNeeded(string filePath, Metadata oldMeta, Metadata newMeta, bool isFolderMode, bool isIndividualEdit)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string Sanitize(string input) => string.Concat(input.Split(invalidChars));

            string newRomanisedArtist = Sanitize(newMeta.RomanisedArtist);
            string newRomanisedTitle = Sanitize(newMeta.RomanisedTitle);
            string newCreator = Sanitize(newMeta.Creator);

            bool canUpdateSpecifics = !isFolderMode || isIndividualEdit;
            string newDiffName = Sanitize(canUpdateSpecifics ? newMeta.Difficulty : oldMeta.Difficulty);

            if (oldMeta.RomanisedArtist == newRomanisedArtist &&
                oldMeta.RomanisedTitle == newRomanisedTitle &&
                oldMeta.Creator == newCreator &&
                oldMeta.Difficulty == newDiffName)
            {
                return filePath;
            }

            string dir = Path.GetDirectoryName(filePath)!;
            string ext = Path.GetExtension(filePath);
            string newFileName = $"{newRomanisedArtist} - {newRomanisedTitle} ({newCreator}) [{newDiffName}]{ext}";
            string newFilePath = Path.Combine(dir, newFileName);

            if (!string.Equals(filePath, newFilePath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(newFilePath))
                {
                    throw new IOException("Exists");
                }
                File.Move(filePath, newFilePath);
                return newFilePath;
            }
            return filePath;
        }
    }
}