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

        public static async Task Install(GithubRelease release, params string[] args)
        {
            const string LOG_IDENT = "Updater::Install";

            var asset = GetAsset(release) ?? throw new InvalidDataException("The release has no reistrap executable.");
            string downloadLocation = Path.Combine(Paths.TempUpdates, asset.Name);

            Directory.CreateDirectory(Paths.TempUpdates);

            App.Logger.WriteLine(LOG_IDENT, $"Downloading {release.TagName}");

            if (!File.Exists(downloadLocation))
            {
                string partial = downloadLocation + ".part";

                using (var response = await App.HttpClient.GetAsync(asset.BrowserDownloadUrl))
                {
                    response.EnsureSuccessStatusCode();

                    await using var fileStream = new FileStream(partial, FileMode.Create, FileAccess.Write);
                    await response.Content.CopyToAsync(fileStream);
                }

                File.Move(partial, downloadLocation, true);
            }

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
