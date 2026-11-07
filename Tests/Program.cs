using Bloxstrap;
using Bloxstrap.Models.APIs.GitHub;

// Variables
var release = new GithubRelease { TagName = "v1.3.9", Assets = new() };
var exe = new GithubReleaseAsset { Name = "reistrap-v1.3.9.exe" };

// Tests
release.Assets.Add(new GithubReleaseAsset { Name = "checksums.txt" });
release.Assets.Add(exe);
if (Updater.GetAsset(release) != exe) throw new Exception("Executable selection failed");
release.Assets.Remove(exe);
if (Updater.GetAsset(release) != null) throw new Exception("Non-executable accepted");
release.Assets.Add(new GithubReleaseAsset { Name = "../reistrap.exe" });
if (Updater.GetAsset(release) != null) throw new Exception("Unsafe filename accepted");
release.Assets.Add(new GithubReleaseAsset { Name = "reistrap.exe" });
if (Updater.GetAsset(release)?.Name != "reistrap.exe") throw new Exception("Legacy filename rejected");
release.Assets = null;
if (Updater.GetAsset(release) != null) throw new Exception("Empty release accepted");
Console.WriteLine("5 updater regression checks passed.");
