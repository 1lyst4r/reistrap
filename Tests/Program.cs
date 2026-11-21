using System.Web;
using Bloxstrap;
using Bloxstrap.Integrations;
using Bloxstrap.Models.APIs.Roblox;
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

var openId = Guid.NewGuid();
var fullId = Guid.NewGuid();
var missingId = Guid.NewGuid();
var available = ServerRegions.GetAvailable(new[]
{
    new PublicServer { Id = openId.ToString(), Playing = 1, MaxPlayers = 2 },
    new PublicServer { Id = fullId.ToString(), Playing = 2, MaxPlayers = 2 },
    new PublicServer { Id = missingId.ToString(), MaxPlayers = 2 },
    new PublicServer { Id = "invalid", Playing = 1, MaxPlayers = 2 }
});
if (!available.SetEquals(new[] { openId })) throw new Exception("Invalid public server availability");

var stale = new RegionServer { ServerId = Guid.NewGuid().ToString() };
var full = new RegionServer { ServerId = fullId.ToString() };
var open = new RegionServer { ServerId = openId.ToString() };
foreach (bool randomize in new[] { false, true })
{
    if (ServerRegions.PickServer(new[] { stale, full, open }, available, randomize) != openId)
        throw new Exception("Failed to select the available regional server");
    if (ServerRegions.PickServer(new[] { stale, full }, available, randomize) != null)
        throw new Exception("Expected normal Roblox matchmaking");
}

const string deepLink = "roblox://experiences/start?";
const string browserUrl = "https://assetgame.roblox.com/game/PlaceLauncher.ashx?";
var browser = BrowserLink("placeId=123&launchData=a%2Bb%26c&request=RequestGame");
var expected = BrowserLink($"placeId=123&launchData=a%2Bb%26c&request=RequestGameJob&gameId={openId}");
var parsed = MatchmakingLink.Parse(browser);
if (parsed?.PlaceId != 123 || parsed.ForServer(openId) != expected) throw new Exception("Browser link was corrupted");
var deep = deepLink + "placeId=123&launchData=a%2Bb%26c";
if (MatchmakingLink.Parse(deep)?.ForServer(openId) != deep + "&gameInstanceId=" + openId)
    throw new Exception("Deep link was corrupted");

foreach (var query in new[] { "placeId=0", "placeId=123x", "placeId=123&PlaceId=456",
    "placeId=12345678901234567890123456", "otherPlaceId=123", "placeId=123&linkCode=private",
    "placeId=123&gameInstanceId=existing", "placeId=123&userId=42", "placeId=123&friendId=42" })
{
    if (MatchmakingLink.Parse(deepLink + query) != null || MatchmakingLink.Parse(BrowserLink("request=RequestGame&" + query)) != null)
        throw new Exception("Unexpected matchmaking override for " + query);
}
if (MatchmakingLink.Parse(BrowserLink("request=RequestGameJob&placeId=123")) != null ||
    MatchmakingLink.Parse(BrowserLink("request=RequestGame&placeId=123&request=RequestGame")) != null ||
    MatchmakingLink.Parse(BrowserLink("request=RequestGame&placeId=123&referralPage=friends")) != null ||
    MatchmakingLink.Parse(browser + "+placelauncherurl:another") != null)
    throw new Exception("Unexpected matchmaking override for an explicit or ambiguous browser request");
Console.WriteLine("Browser join and server selection regression checks passed.");

RepositoryChecks.Run();

// Functions
static string BrowserLink(string query) =>
    "roblox-player:1+launchmode:play+placelauncherurl:" + HttpUtility.UrlEncode(browserUrl + query) + "+gameLocale:en_us";
