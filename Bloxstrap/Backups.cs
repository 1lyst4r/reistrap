using System.IO.Compression;

using Bloxstrap.Models.SettingTasks;

namespace Bloxstrap
{
    public static class Backups
    {
        private const string ModPackFolder = "mods/";

        private const string AppFolder = "reistrap/";

        private const string RobloxSettingsEntry = "roblox/GlobalBasicSettings_13.xml";

        private static readonly string[] BackupFiles = { "Settings.json", "GameStats.json" };

        private static readonly string[] BackupFolders = { "Modifications", "FastFlagProfiles", "CursorSets", "CustomThemes" };

        private static int AddFolder(ZipArchive archive, string folder, string entryPrefix, string destination, string temporary, Func<string, bool>? include = null)
        {
            int added = 0;

            if (!Directory.Exists(folder))
                return added;

            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                string path = Path.GetFullPath(file);
                if (path.Equals(destination, StringComparison.OrdinalIgnoreCase) || path.Equals(temporary, StringComparison.OrdinalIgnoreCase))
                    continue;

                string relative = Path.GetRelativePath(folder, file).Replace('\\', '/');

                if (include is not null && !include(relative))
                    continue;

                archive.CreateEntryFromFile(file, entryPrefix + relative);
                added++;
            }

            return added;
        }

        private static string? GetSafeTarget(string root, string relative)
        {
            string target = Path.GetFullPath(Path.Combine(root, relative));
            string fullRoot = Path.GetFullPath(root).TrimEnd('\\') + '\\';

            return target.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) ? target : null;
        }

        private static void ExtractEntry(ZipArchiveEntry entry, string target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            Filesystem.AssertReadOnly(target);
            entry.ExtractToFile(target, true);
        }

        private static void WriteArchive(string destination, Action<ZipArchive, string> populate)
        {
            string temporary = Path.Combine(Path.GetDirectoryName(destination)!, $"{Guid.NewGuid():N}.tmp");
            try
            {
                using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
                    populate(archive, temporary);

                File.Move(temporary, destination, true);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        // Mod Packs

        public static int ExportModPack(string zipPath)
        {
            string destination = Path.GetFullPath(zipPath);
            int count = 0;
            WriteArchive(destination, (archive, temporary) =>
                count = AddFolder(archive, Paths.Modifications, ModPackFolder, destination, temporary,
                    x => !x.StartsWith("ClientSettings/", StringComparison.OrdinalIgnoreCase)));
            return count;
        }

        public static int ImportModPack(string zipPath)
        {
            using var archive = ZipFile.OpenRead(zipPath);

            var entries = archive.Entries.Where(x => x.FullName.StartsWith(ModPackFolder, StringComparison.OrdinalIgnoreCase) && !String.IsNullOrEmpty(x.Name)).ToList();

            if (!entries.Any())
                throw new InvalidDataException(Strings.Menu_Mods_ModPack_Invalid);

            int imported = 0;

            foreach (var entry in entries)
            {
                string relative = entry.FullName[ModPackFolder.Length..];

                if (relative.StartsWith("ClientSettings/", StringComparison.OrdinalIgnoreCase))
                    continue;

                string? target = GetSafeTarget(Paths.Modifications, relative);

                if (target is null)
                    continue;

                ExtractEntry(entry, target);
                imported++;
            }

            return imported;
        }

        // Full Backups

        public static void CreateBackup(string zipPath)
        {
            string destination = Path.GetFullPath(zipPath);
            WriteArchive(destination, (archive, temporary) =>
            {
                foreach (string name in BackupFiles)
                {
                    string file = Path.Combine(Paths.Base, name);
                    if (File.Exists(file))
                        archive.CreateEntryFromFile(file, AppFolder + name);
                    else if (name == "Settings.json")
                    {
                        using var stream = archive.CreateEntry(AppFolder + name).Open();
                        JsonSerializer.Serialize(stream, App.Settings.Prop);
                    }
                }

                foreach (string folder in BackupFolders)
                    AddFolder(archive, Path.Combine(Paths.Base, folder), $"{AppFolder}{folder}/", destination, temporary);

                if (File.Exists(RobloxSettingsTask.FileLocation))
                    archive.CreateEntryFromFile(RobloxSettingsTask.FileLocation, RobloxSettingsEntry);
            });
        }

        public static void RestoreBackup(string zipPath)
        {
            using var archive = ZipFile.OpenRead(zipPath);

            if (!archive.Entries.Any(x => x.FullName == AppFolder + "Settings.json"))
                throw new InvalidDataException(Strings.Menu_Bloxstrap_Backup_Invalid);

            foreach (var entry in archive.Entries.Where(x => !String.IsNullOrEmpty(x.Name)))
            {
                string? target = null;

                if (entry.FullName.StartsWith(AppFolder, StringComparison.Ordinal))
                {
                    string relative = entry.FullName[AppFolder.Length..];
                    string top = relative.Split('/')[0];

                    if (BackupFiles.Contains(relative) || BackupFolders.Contains(top))
                        target = GetSafeTarget(Paths.Base, relative);
                }
                else if (entry.FullName == RobloxSettingsEntry && !Process.GetProcessesByName(App.RobloxPlayerAppName).Any())
                {
                    if (File.Exists(RobloxSettingsTask.FileLocation))
                        File.SetAttributes(RobloxSettingsTask.FileLocation, FileAttributes.Normal);

                    target = RobloxSettingsTask.FileLocation;
                }

                if (target is not null)
                    ExtractEntry(entry, target);
            }
        }
    }
}
