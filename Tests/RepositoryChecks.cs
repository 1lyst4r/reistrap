using System.IO.Compression;
using System.Net;
using Bloxstrap.Utility;
using Bloxstrap.Models.APIs.Roblox;
using Bloxstrap.Models.APIs.GitHub;
using System.Text;
using Bloxstrap;
using Bloxstrap.Models.Manifest;
using Bloxstrap.Integrations;

internal static class RepositoryChecks
{
    // Functions
    internal static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "reistrap-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Paths.Initialize(root);

        try
        {
            var failures = new List<string>();
            foreach (Action check in new Action[] { StateChanges, ModExports, FailedExport, PackageList, FlagSnapshots, FullBackup, ClientLogs, NullFlags, Downloads, Thumbnails, LockCancellation, GameNames })
            {
                try { check(); }
                catch (Exception ex) { failures.Add($"{check.Method.Name}: {ex.Message}"); }
            }
            if (failures.Count != 0)
                throw new Exception(String.Join(Environment.NewLine, failures));
        }
        finally
        {
            Directory.Delete(root, true);
        }
        Console.WriteLine("Repository regression checks passed.");
    }

    private static void StateChanges()
    {
        var state = new JsonManager<Dictionary<string, int>>("TestState");
        if (state.HasFileOnDiskChanged()) throw new Exception("Missing state reported as changed");
        File.WriteAllText(state.FileLocation, "{\"a\":1}", new UTF8Encoding(true));
        if (!state.HasFileOnDiskChanged() || !state.Load(false) || state.HasFileOnDiskChanged())
            throw new Exception("Unchanged UTF-8 state reported as changed");
        File.Delete(state.FileLocation);
        if (!state.HasFileOnDiskChanged()) throw new Exception("Deleted state reported as unchanged");
        File.WriteAllText(state.FileLocation, "{truncated");
        if (state.Load(false) || File.ReadAllText(state.FileLocation + ".bak") != "{truncated")
            throw new Exception("Silent recovery lost the original settings");
    }

    private static void ModExports()
    {
        Directory.CreateDirectory(Paths.Modifications);
        File.WriteAllText(Path.Combine(Paths.Modifications, "cursor.png"), "cursor");
        string zip = Path.Combine(Paths.Modifications, "pack.zip");
        for (int i = 0; i < 2; i++)
        {
            if (Backups.ExportModPack(zip) != 1) throw new Exception("Export included its own archive");
            using var archive = ZipFile.OpenRead(zip);
            if (archive.Entries.Single().FullName != "mods/cursor.png") throw new Exception("Wrong archive contents");
        }
        File.Delete(Path.Combine(Paths.Modifications, "cursor.png"));
        if (Backups.ImportModPack(zip) != 1 || File.ReadAllText(Path.Combine(Paths.Modifications, "cursor.png")) != "cursor")
            throw new Exception("Mod pack did not round trip");
        File.Delete(zip);
    }

    private static void FailedExport()
    {
        Directory.CreateDirectory(Paths.Modifications);
        string zip = Path.Combine(Paths.Base, "previous.zip");
        File.WriteAllText(zip, "original");
        using var locked = File.Open(Path.Combine(Paths.Modifications, "locked.txt"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        try { Backups.ExportModPack(zip); }
        catch (IOException) { }
        if (File.ReadAllText(zip) != "original") throw new Exception("Failed export replaced the previous backup");
    }

    private static void PackageList()
    {
        const string manifest = "v0\nRobloxPlayerLauncher.exe\nhash1\n1\n1\ncontent.zip\nhash2\n100\n3000000000";
        var package = new PackageManifest(manifest).Single();
        if (package.Name != "content.zip" || (long)package.Size != 3000000000L)
            throw new Exception("Package skipped or size truncated");
        try { _ = new PackageManifest("v0\ncontent.zip\nhash"); }
        catch (InvalidDataException) { return; }
        throw new Exception("Partial package accepted");
    }

    private static void ClientLogs()
    {
        if (!ActivityWatcher.IsClientLog("version_Player_123_last.log", false)
            || !ActivityWatcher.IsClientLog("version_Studio_456_last.log", true)
            || ActivityWatcher.IsClientLog("version_Player_123_CrashHandler_last.log", false)
            || ActivityWatcher.IsClientLog("version_Studio_456_last.log", false))
            throw new Exception("Wrong activity log selected");
    }

    private static void FullBackup()
    {
        string file = Path.Combine(Paths.Modifications, "cursor.png");
        string backup = Path.Combine(Paths.Modifications, "backup.zip");
        Backups.CreateBackup(backup);
        using (var archive = ZipFile.Open(backup, ZipArchiveMode.Update))
        {
            if (archive.GetEntry("reistrap/Settings.json") is null || archive.GetEntry("reistrap/Modifications/cursor.png") is null
                || archive.Entries.Any(entry => entry.FullName.EndsWith("backup.zip") || entry.FullName.EndsWith(".tmp")))
                throw new Exception("Invalid full backup");
            archive.GetEntry("roblox/GlobalBasicSettings_13.xml")?.Delete();
        }
        File.WriteAllText(file, "changed");
        Backups.RestoreBackup(backup);
        if (File.ReadAllText(file) != "cursor" || !File.Exists(Path.Combine(Paths.Base, "Settings.json")))
            throw new Exception("Full backup did not restore settings and mods");
    }

    private static void NullFlags()
    {
        File.WriteAllText(App.FastFlags.FileLocation, "{\"enabled\":true,\"removed\":null}");
        App.FastFlags.Load(false);
        App.FastFlags.Save();
        if (App.FastFlags.Prop.ContainsKey("removed") || App.FastFlags.GetValue("enabled") != "True")
            throw new Exception("Failed to save a fast flag file containing null");
    }

    private static void Downloads()
    {
        var release = new GithubRelease { TagName = "v1.2.3", Assets = new()
        {
            new() { Name = "checksums.txt", BrowserDownloadUrl = "https://example.test/checksums" },
            new() { Name = "reistrap.exe", BrowserDownloadUrl = "https://example.test/reistrap.exe" }
        }};
        using var client = new HttpClient(new Reply(request =>
        {
            if (request.RequestUri!.AbsolutePath != "/reistrap.exe") throw new Exception("Wrong release attachment");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("fixture") };
        }));
        string file = Updater.Download(release, client, Paths.Base).GetAwaiter().GetResult();
        if (File.ReadAllText(file) != "fixture") throw new Exception("Update download incomplete");

        using var offline = new HttpClient(new Reply(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        try { Updater.Download(release, offline, Paths.Base).GetAwaiter().GetResult(); }
        catch (HttpRequestException)
        {
            if (File.ReadAllText(file) == "fixture" && Directory.GetFiles(Path.GetDirectoryName(file)!, "*.part").Length == 0)
                return;
        }
        throw new Exception("Failed download did not preserve the completed update");
    }

    private static void Thumbnails()
    {
        var requests = new List<ThumbnailRequest> { new(), new() };
        using var client = new HttpClient(new Reply(request =>
        {
            if (request.Content?.Headers.ContentType?.MediaType != "application/json")
                throw new Exception("Missing JSON content type");
            const string json = "{\"data\":[{\"state\":\"Completed\",\"requestId\":\"1\",\"imageUrl\":\"icon\"},{\"state\":\"Error\",\"requestId\":\"bad\"}]}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }));
        var images = Bloxstrap.Utility.Thumbnails.GetThumbnailUrlsAsync(requests, CancellationToken.None, client).GetAwaiter().GetResult();
        if (images[0] != null || images[1] != "icon") throw new Exception("Thumbnail response mapped to the wrong asset");
    }

    private static void LockCancellation() => CheckCancellation().GetAwaiter().GetResult();

    private static async Task CheckCancellation()
    {
        string name = "reistrap-test-" + Guid.NewGuid().ToString("N");
        await using var owner = new AsyncMutex(false, name);
        await owner.AcquireAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        await using var waiting = new AsyncMutex(false, name);
        using var cancel = new CancellationTokenSource();
        var acquisition = waiting.AcquireAsync(cancel.Token);
        cancel.Cancel();
        try { await acquisition.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (OperationCanceledException) { return; }
        throw new Exception("Cancelled lock request succeeded");
    }

    private sealed class Reply : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public Reply(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(_respond(request));
    }

    private static void GameNames()
    {
        if (PathValidator.GameFileName("CON.map.v2", "123") != "_CON.map.v2"
            || PathValidator.GameFileName("?<>...", "123") != "123"
            || PathValidator.GameFileName("My game!", "123") != "My game!"
            || PathValidator.GameFileName(new string('a', 79) + "\U0001F600", "123") != new string('a', 79))
            throw new Exception("Invalid game filename for shortcuts or screenshots");
    }

    private static void FlagSnapshots()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(App.FastFlags.FileLocation)!);
        File.WriteAllText(App.FastFlags.FileLocation, "{}");
        Directory.CreateDirectory(FlagHistory.Folder);
        File.WriteAllText(Path.Combine(FlagHistory.Folder, "20200101-010101.json"), "{}");
        for (int i = 0; i < 3; i++)
            FlagHistory.Snapshot(new Dictionary<string, object> { ["n"] = i }, new Dictionary<string, object>());
        if (FlagHistory.List().Count != 4) throw new Exception("Fast flag snapshots were lost");
    }
}
