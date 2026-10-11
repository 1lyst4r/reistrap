using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace Bloxstrap.Integrations
{
    public static class WallpaperControl
    {
        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private const string DesktopKey = @"Control Panel\Desktop";
        private const string HistoryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Wallpapers";
        private const int HistoryEntries = 5;

        private static readonly string[] WallpaperApps = { "wallpaper32", "wallpaper64", "Lively", "LivelyUI", "Livelywpf" };

        private static readonly Dictionary<string, (string Style, string Tile)> Styles = new()
        {
            ["fill"] = ("10", "0"),
            ["fit"] = ("6", "0"),
            ["stretch"] = ("2", "0"),
            ["tile"] = ("0", "1"),
            ["center"] = ("0", "0"),
            ["span"] = ("22", "0")
        };

        private class SavedState
        {
            public string Wallpaper = "";
            public string Style = "10";
            public string Tile = "0";
            public string[] History = new string[HistoryEntries];
        }

        private static SavedState? _saved;

        private static readonly List<string> _closedApps = new();

        private static readonly List<string> _stoppedEngines = new();

        public static void Set(string path, string? style)
        {
            const string LOG_IDENT = "WallpaperControl::Set";

            try
            {
                if (_saved is null)
                {
                    Backup();
                    CloseWallpaperApps();
                }

                Apply(path, (style ?? "fill").ToLowerInvariant());
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static void Reset()
        {
            const string LOG_IDENT = "WallpaperControl::Reset";

            if (_saved is null)
                return;

            try
            {
                var saved = _saved;

                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, saved.Wallpaper, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                using (var key = Registry.CurrentUser.OpenSubKey(DesktopKey, true))
                {
                    key?.SetValue("WallpaperStyle", saved.Style);
                    key?.SetValue("TileWallpaper", saved.Tile);
                }

                RestoreHistory();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                _saved = null;
                RestartWallpaperApps();
            }
        }

        private static void Apply(string path, string style)
        {
            if (!Styles.TryGetValue(style, out var values))
                values = Styles["fill"];

            using (var key = Registry.CurrentUser.OpenSubKey(DesktopKey, true))
            {
                key?.SetValue("WallpaperStyle", values.Style);
                key?.SetValue("TileWallpaper", values.Tile);
            }

            if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
                App.Logger.WriteLine("WallpaperControl::Apply", $"SystemParametersInfo failed ({Marshal.GetLastWin32Error()})");

            RestoreHistory();
        }

        private static void Backup()
        {
            var state = new SavedState();

            using (var history = Registry.CurrentUser.OpenSubKey(HistoryKey))
            {
                for (int i = 0; i < HistoryEntries; i++)
                    state.History[i] = history?.GetValue($"BackgroundHistoryPath{i}") as string ?? "";
            }

            using (var desktop = Registry.CurrentUser.OpenSubKey(DesktopKey))
            {
                state.Wallpaper = desktop?.GetValue("WallPaper") as string ?? state.History[0];
                state.Style = desktop?.GetValue("WallpaperStyle") as string ?? "10";
                state.Tile = desktop?.GetValue("TileWallpaper") as string ?? "0";
            }

            _saved = state;
        }

        private static void RestoreHistory()
        {
            if (_saved is null)
                return;

            using var history = Registry.CurrentUser.OpenSubKey(HistoryKey, true);

            for (int i = 0; i < HistoryEntries; i++)
            {
                if (!String.IsNullOrEmpty(_saved.History[i]))
                    history?.SetValue($"BackgroundHistoryPath{i}", _saved.History[i], RegistryValueKind.String);
            }
        }

        private static void CloseWallpaperApps()
        {
            foreach (string name in WallpaperApps)
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    string? exe = GetPath(process);

                    try
                    {
                        process.Kill();
                        process.WaitForExit(2000);

                        if (!String.IsNullOrEmpty(exe) && !_closedApps.Contains(exe))
                            _closedApps.Add(exe);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteException("WallpaperControl::CloseWallpaperApps", ex);

                        if (IsWallpaperEngine(name) && !String.IsNullOrEmpty(exe) && !_stoppedEngines.Contains(exe))
                        {
                            RunControl(exe, "stop");
                            _stoppedEngines.Add(exe);
                        }
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
        }

        private static void RestartWallpaperApps()
        {
            foreach (string exe in _closedApps.Where(File.Exists))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exe) });
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("WallpaperControl::RestartWallpaperApps", ex);
                }
            }

            foreach (string exe in _stoppedEngines)
                RunControl(exe, "play");

            _closedApps.Clear();
            _stoppedEngines.Clear();
        }

        private static bool IsWallpaperEngine(string name) => name.StartsWith("wallpaper", StringComparison.OrdinalIgnoreCase);

        private static void RunControl(string exe, string command)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo(exe, $"-control {command}") { UseShellExecute = false, CreateNoWindow = true });
                process?.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("WallpaperControl::RunControl", ex);
            }
        }

        private static string? GetPath(Process process)
        {
            IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, process.Id);

            if (handle == IntPtr.Zero)
                return null;

            try
            {
                var builder = new StringBuilder(1024);
                int size = builder.Capacity;

                return QueryFullProcessImageName(handle, 0, builder, ref size) ? builder.ToString() : null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
    }
}
