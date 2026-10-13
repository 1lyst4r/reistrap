using Bloxstrap.Integrations;
using Bloxstrap.Models.APIs.Roblox;
using Bloxstrap.Models.Entities;

namespace Bloxstrap
{
    public class ScreenshotSorter : IDisposable
    {
        private static readonly Regex FileNamePattern = new(@"^\d+_(\d+)_\d+\.png$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly ActivityWatcher? _activity;

        private readonly FileSystemWatcher _watcher;

        public static string GetGameFolder(string gameName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            string name = new string(gameName.Where(x => !invalid.Contains(x)).ToArray()).Trim().TrimEnd('.');

            if (name.Length > 80)
                name = name[..80].Trim();

            return Path.Combine(RobloxCaptureBlocker.ScreenshotsFolder, String.IsNullOrEmpty(name) ? Strings.Common_Unknown : name);
        }

        public ScreenshotSorter(ActivityWatcher? activity)
        {
            _activity = activity;

            Directory.CreateDirectory(RobloxCaptureBlocker.ScreenshotsFolder);

            _watcher = new FileSystemWatcher(RobloxCaptureBlocker.ScreenshotsFolder, "*.png")
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += (_, e) => Task.Run(() => Sort(e.FullPath));
        }

        private async Task<string?> GetGameName(string path)
        {
            long universeId = 0;

            var match = FileNamePattern.Match(Path.GetFileName(path));

            if (match.Success && long.TryParse(match.Groups[1].Value, out long placeId))
            {
                if (_activity is { InGame: true } && _activity.Data.PlaceId == placeId)
                    universeId = _activity.Data.UniverseId;
                else
                    universeId = (await Http.GetJson<UniverseIdResponse>($"https://apis.roblox.com/universes/v1/places/{placeId}/universe")).UniverseId;
            }
            else if (_activity is { InGame: true })
            {
                universeId = _activity.Data.UniverseId;
            }

            if (universeId == 0)
                return null;

            await UniverseDetails.FetchSingle(universeId);

            return UniverseDetails.LoadFromCache(universeId)?.Data.Name;
        }

        private async Task Sort(string path)
        {
            const string LOG_IDENT = "ScreenshotSorter::Sort";

            string? gameName;

            try
            {
                gameName = await GetGameName(path);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Could not find the game for {path}");
                App.Logger.WriteException(LOG_IDENT, ex);
                return;
            }

            if (String.IsNullOrEmpty(gameName))
                return;

            string gameFolder = GetGameFolder(gameName);

            for (int attempt = 1; ; attempt++)
            {
                await Task.Delay(500);

                try
                {
                    Directory.CreateDirectory(gameFolder);

                    string target = Path.Combine(gameFolder, Path.GetFileName(path));

                    for (int i = 2; File.Exists(target); i++)
                        target = Path.Combine(gameFolder, $"{Path.GetFileNameWithoutExtension(path)} ({i}){Path.GetExtension(path)}");

                    File.Move(path, target);
                    App.Logger.WriteLine(LOG_IDENT, $"Moved {Path.GetFileName(path)} to {gameFolder}");
                    return;
                }
                catch (IOException) when (attempt < 20 && File.Exists(path))
                {
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Could not move {path}");
                    App.Logger.WriteException(LOG_IDENT, ex);
                    return;
                }
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
