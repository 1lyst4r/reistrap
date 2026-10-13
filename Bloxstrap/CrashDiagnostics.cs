using System.Runtime.InteropServices;

namespace Bloxstrap
{
    public enum CrashFix
    {
        None,
        SafeLaunch,
        RemoveGraphicsFlags,
        EnableMemoryTrimmer,
        Reinstall
    }

    public class CrashCause
    {
        public string Title { get; init; } = "";

        public string Message { get; init; } = "";

        public CrashFix Fix { get; init; }
    }

    public static class CrashDiagnostics
    {
        private static readonly Dictionary<string, string> GraphicsFlags = new()
        {
            ["FFlagDebugGraphicsPreferVulkan"] = "Vulkan",
            ["FFlagDebugGraphicsPreferOpenGL"] = "OpenGL",
            ["FFlagDebugGraphicsPreferD3D11"] = "Direct3D 11"
        };

        private static readonly string[] GpuMarkers =
        {
            "dxgi_error_device",
            "device removed",
            "device_removed",
            "devicelost",
            "device lost",
            "vk_error_device_lost",
            "removing device"
        };

        private static readonly string[] MemoryMarkers =
        {
            "out of memory",
            "bad_alloc",
            "e_outofmemory"
        };

        private static string CrashesFolder => Path.Combine(Paths.LocalAppData, "Roblox", "logs", "crashes");

        public static bool DidCrash(DateTime since)
        {
            try
            {
                if (!Directory.Exists(CrashesFolder))
                    return false;

                return Directory.EnumerateFiles(CrashesFolder, "*.dmp", SearchOption.AllDirectories)
                    .Any(x => File.GetLastWriteTime(x) >= since || File.GetCreationTime(x) >= since);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CrashDiagnostics::DidCrash", ex);
                return false;
            }
        }

        public static CrashCause Diagnose(string? logFile, bool? joinedGame, TimeSpan uptime, bool safeMode)
        {
            string log = ReadLogTail(logFile);

            var forcedGraphics = safeMode ? new List<string>() : GetForcedGraphicsModes();

            if (GpuMarkers.Any(log.Contains))
            {
                if (forcedGraphics.Any())
                    return GraphicsFlagsCause(forcedGraphics);

                return new CrashCause
                {
                    Title = Strings.Dialog_Crash_Gpu_Title,
                    Message = Strings.Dialog_Crash_Gpu_Message
                };
            }

            if (forcedGraphics.Any())
                return GraphicsFlagsCause(forcedGraphics);

            if (MemoryMarkers.Any(log.Contains) || GetMemoryLoad() >= 90)
            {
                bool trimmerOff = !App.Settings.Prop.MemoryTrimmer;

                return new CrashCause
                {
                    Title = Strings.Dialog_Crash_Memory_Title,
                    Message = Strings.Dialog_Crash_Memory_Message + (trimmerOff ? " " + Strings.Dialog_Crash_Memory_TrimmerHint : ""),
                    Fix = trimmerOff ? CrashFix.EnableMemoryTrimmer : CrashFix.None
                };
            }

            bool duringStartup = joinedGame == false || (joinedGame is null && uptime.TotalSeconds < 90);

            if (duringStartup)
            {
                if (!safeMode && HasCustomizations())
                {
                    return new CrashCause
                    {
                        Title = Strings.Dialog_Crash_Startup_Title,
                        Message = Strings.Dialog_Crash_Startup_Mods,
                        Fix = CrashFix.SafeLaunch
                    };
                }

                return new CrashCause
                {
                    Title = Strings.Dialog_Crash_Startup_Title,
                    Message = Strings.Dialog_Crash_Startup_Files,
                    Fix = CrashFix.Reinstall
                };
            }

            return new CrashCause
            {
                Title = Strings.Dialog_Crash_InGame_Title,
                Message = safeMode ? Strings.Dialog_Crash_InGame_Safe : Strings.Dialog_Crash_InGame_Message,
                Fix = safeMode || !HasCustomizations() ? CrashFix.None : CrashFix.SafeLaunch
            };
        }

        public static void RemoveGraphicsFlags()
        {
            foreach (string name in GraphicsFlags.Keys)
                App.FastFlags.SetValue(name, null);

            App.FastFlags.Save();
        }

        private static CrashCause GraphicsFlagsCause(List<string> modes) => new()
        {
            Title = Strings.Dialog_Crash_GraphicsFlags_Title,
            Message = String.Format(Strings.Dialog_Crash_GraphicsFlags_Message, String.Join(", ", modes)),
            Fix = CrashFix.RemoveGraphicsFlags
        };

        private static List<string> GetForcedGraphicsModes() => GraphicsFlags
            .Where(x => String.Equals(App.FastFlags.GetValue(x.Key), "True", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value)
            .ToList();

        private static bool HasCustomizations()
        {
            if (App.FastFlags.Prop.Any())
                return true;

            if (!App.Settings.Prop.ApplyModsToPlayer || !Directory.Exists(Paths.Modifications))
                return false;

            return Directory.EnumerateFiles(Paths.Modifications, "*", SearchOption.AllDirectories)
                .Select(x => Path.GetRelativePath(Paths.Modifications, x))
                .Any(x => !x.StartsWith("ClientSettings", StringComparison.OrdinalIgnoreCase) && !x.EndsWith(".lock"));
        }

        private static string ReadLogTail(string? logFile)
        {
            const int tailBytes = 512 * 1024;

            if (String.IsNullOrEmpty(logFile) || !File.Exists(logFile))
                return "";

            try
            {
                using var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

                if (stream.Length > tailBytes)
                    stream.Seek(-tailBytes, SeekOrigin.End);

                using var reader = new StreamReader(stream);
                return reader.ReadToEnd().ToLowerInvariant();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("CrashDiagnostics::ReadLogTail", ex);
                return "";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        private static uint GetMemoryLoad()
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            return GlobalMemoryStatusEx(ref status) ? status.dwMemoryLoad : 0;
        }
    }
}
