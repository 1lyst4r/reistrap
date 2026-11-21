namespace Bloxstrap
{
    public static class Updater
    {
        // Functions

        public static GithubReleaseAsset? GetAsset(GithubRelease release) =>
            release.Assets?.FirstOrDefault(asset =>
                !String.IsNullOrEmpty(asset.Name) &&
                asset.Name == Path.GetFileName(asset.Name) &&
                (asset.Name.Equals("reistrap.exe", StringComparison.OrdinalIgnoreCase) ||
                 asset.Name.Equals($"reistrap-{release.TagName}.exe", StringComparison.OrdinalIgnoreCase)));

        public static async Task<GithubRelease?> FindUpdate()
        {
            var release = await App.GetLatestRelease();

            if (release is null || GetAsset(release) is null)
                return null;

            var comparison = Utilities.CompareVersions(App.Version, release.TagName);

            if (comparison == VersionComparison.GreaterThan || (App.IsProductionBuild && comparison == VersionComparison.Equal))
                return null;

            return release;
        }

        internal static Task<string> Download(GithubRelease release) => Download(release, App.HttpClient, Paths.TempUpdates);

        internal static async Task<string> Download(GithubRelease release, HttpClient client, string updateFolder)
        {
            const string LOG_IDENT = "Updater::Download";

            var asset = GetAsset(release) ?? throw new InvalidDataException("The release has no reistrap executable.");
            string folder = Path.Combine(updateFolder, MD5Hash.FromString(release.TagName));
            string downloadLocation = Path.Combine(folder, asset.Name);

            Directory.CreateDirectory(folder);

            App.Logger.WriteLine(LOG_IDENT, $"Downloading {release.TagName}");

            string partial = downloadLocation + $".{Guid.NewGuid():N}.part";
            try
            {
                using var response = await client.GetAsync(asset.BrowserDownloadUrl);
                response.EnsureSuccessStatusCode();
                await using (var stream = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await response.Content.CopyToAsync(stream);

                File.Move(partial, downloadLocation, true);
                return downloadLocation;
            }
            finally
            {
                File.Delete(partial);
            }
        }

        public static async Task Install(GithubRelease release, params string[] args)
        {
            const string LOG_IDENT = "Updater::Install";
            string downloadLocation = await Download(release);
            var startInfo = new ProcessStartInfo { FileName = downloadLocation };
            startInfo.ArgumentList.Add("-upgrade");

            foreach (string arg in args)
                startInfo.ArgumentList.Add(arg);

            App.Logger.WriteLine(LOG_IDENT, $"Starting {release.TagName}");

            new InterProcessLock("AutoUpdater");

            Process.Start(startInfo);
        }
    }
}
