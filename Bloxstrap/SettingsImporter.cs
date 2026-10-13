using System.Reflection;

using Microsoft.Win32;

namespace Bloxstrap
{
    public static class SettingsImporter
    {
        private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        public static Dictionary<string, string> FindInstalls()
        {
            const string LOG_IDENT = "SettingsImporter::FindInstalls";

            var candidates = new List<(string Name, string Folder)>();

            try
            {
                using var uninstall = Registry.CurrentUser.OpenSubKey(UninstallKey);

                foreach (string keyName in uninstall?.GetSubKeyNames() ?? Array.Empty<string>())
                {
                    using var key = uninstall!.OpenSubKey(keyName);

                    if (key?.GetValue("InstallLocation") is not string location || String.IsNullOrWhiteSpace(location))
                        continue;

                    string name = key.GetValue("DisplayName") as string ?? keyName;
                    candidates.Add((name, location.Trim().Trim('"')));
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            try
            {
                foreach (string folder in Directory.GetDirectories(Paths.LocalAppData))
                    candidates.Add((Path.GetFileName(folder), folder));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            var installs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { NormalizePath(Paths.Base) };

            foreach (var (name, folder) in candidates)
            {
                string normalized = NormalizePath(folder);

                if (String.IsNullOrEmpty(normalized) || !seen.Add(normalized) || !IsBootstrapperFolder(folder))
                    continue;

                string label = name;

                for (int i = 2; installs.ContainsKey(label); i++)
                    label = $"{name} ({i})";

                installs[label] = folder;
            }

            return installs.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsBootstrapperFolder(string folder)
        {
            string path = Path.Combine(folder, "Settings.json");

            if (!File.Exists(path))
                return false;

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("BootstrapperStyle", out _);
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path).TrimEnd('\\');
            }
            catch
            {
                return "";
            }
        }

        public static int ImportSettings(string folder)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "Settings.json")));

            return ImportSettings(document.RootElement, null);
        }

        public static int ImportSettings(JsonElement root, IReadOnlySet<string>? excluded)
        {
            const string LOG_IDENT = "SettingsImporter::ImportSettings";

            int imported = 0;

            foreach (var property in typeof(Models.Persistable.Settings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name == nameof(Models.Persistable.Settings.SelectedCustomTheme) || excluded?.Contains(property.Name) == true)
                    continue;

                if (!property.CanWrite || !root.TryGetProperty(property.Name, out var element))
                    continue;

                try
                {
                    object? value = element.Deserialize(property.PropertyType);

                    if (value is null || (property.PropertyType.IsEnum && !Enum.IsDefined(property.PropertyType, value)))
                        continue;

                    property.SetValue(App.Settings.Prop, value);
                    imported++;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Skipped {property.Name} ({ex.Message})");
                }
            }

            if (App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.CustomDialog && App.Settings.Prop.SelectedCustomTheme is null)
                App.Settings.Prop.BootstrapperStyle = BootstrapperStyle.FluentDialog;

            return imported;
        }

        public static int ImportFastFlags(string folder)
        {
            string path = Path.Combine(folder, "Modifications", "ClientSettings", "ClientAppSettings.json");

            if (!File.Exists(path))
                path = Path.Combine(folder, "ClientSettings", "ClientAppSettings.json");

            if (!File.Exists(path))
                return 0;

            var flags = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));

            if (flags is null)
                return 0;

            foreach (var pair in flags)
                App.FastFlags.SetValue(pair.Key, pair.Value);

            return flags.Count;
        }

        public static int ImportMods(string folder)
        {
            string modsFolder = Path.Combine(folder, "Modifications");

            if (!Directory.Exists(modsFolder))
                return 0;

            int copied = 0;

            foreach (string file in Directory.GetFiles(modsFolder, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(modsFolder, file);

                if (relativePath.StartsWith("ClientSettings", StringComparison.OrdinalIgnoreCase))
                    continue;

                string target = Path.Combine(Paths.Modifications, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                Filesystem.AssertReadOnly(target);
                File.Copy(file, target, true);

                copied++;
            }

            return copied;
        }
    }
}
