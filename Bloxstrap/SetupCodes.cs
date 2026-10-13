using System.Net;
using System.Text.Json.Nodes;

namespace Bloxstrap
{
    public class SetupCodeException : Exception
    {
        public SetupCodeException(string message) : base(message) { }
    }

    public static class SetupCodes
    {
        private static readonly HashSet<string> ExcludedSettings = new(StringComparer.Ordinal)
        {
            "BootstrapperIconCustomLocation", "BackgroundImage", "RobloxIconPath", "StudioIconPath",
            "PlayerVersionOverride", "StudioVersionOverride", "GameProfiles", "CustomIntegrations",
            "Locale", "SelectedCustomTheme", "WebEnvironment", "DeveloperMode", "CheckForUpdates",
            "UpdateToPreReleases", "BlockRobloxUpdates", "UseStaticVersionFolder", "EnableAnalytics",
            "DebugDisableVersionPackageCleanup", "ShowAccountOnRichPresence", "CleanerAge",
            "CleanRobloxLogs", "CleanRobloxCache", "CleanAppLogs", "CleanAppDownloads"
        };

        private static string BaseUrl => $"https://{App.AnalyticsHost}/setup";

        public static string Normalize(string code) => new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        public static string Format(string code) => code.Length == 8 ? $"{code[..4]}-{code[4..]}" : code;

        public static async Task<string> Create()
        {
            var settings = JsonSerializer.SerializeToNode(App.Settings.Prop)!.AsObject();

            foreach (string name in ExcludedSettings)
                settings.Remove(name);

            var payload = new JsonObject
            {
                ["settings"] = settings,
                ["flags"] = JsonSerializer.SerializeToNode(App.FastFlags.Prop)
            };

            using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            using var response = await App.HttpClient.PostAsync(BaseUrl, content);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new SetupCodeException(Strings.Menu_Bloxstrap_ShareSetup_TooMany);

            response.EnsureSuccessStatusCode();

            var result = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            return Format(result?["code"]?.GetValue<string>() ?? throw new InvalidDataException("No code returned"));
        }

        public static async Task<JsonDocument> Fetch(string code)
        {
            string normalized = Normalize(code);

            if (normalized.Length != 8)
                throw new SetupCodeException(Strings.Menu_Bloxstrap_ShareSetup_Invalid);

            using var response = await App.HttpClient.GetAsync($"{BaseUrl}/{normalized}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new SetupCodeException(Strings.Menu_Bloxstrap_ShareSetup_NotFound);

            response.EnsureSuccessStatusCode();

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        public static void Apply(JsonElement root)
        {
            if (root.TryGetProperty("settings", out var settings) && settings.ValueKind == JsonValueKind.Object)
                SettingsImporter.ImportSettings(settings, ExcludedSettings);

            if (!root.TryGetProperty("flags", out var flags) || flags.ValueKind != JsonValueKind.Object)
                return;

            foreach (string name in App.FastFlags.Prop.Keys.ToList())
                App.FastFlags.SetValue(name, null);

            foreach (var flag in flags.EnumerateObject())
            {
                if (flag.Name.Length <= 128 && flag.Name.All(x => char.IsLetterOrDigit(x) || x == '_'))
                    App.FastFlags.SetValue(flag.Name, flag.Value.ToString());
            }
        }
    }
}
