using System.Web;

namespace Bloxstrap.Integrations
{
    internal sealed class MatchmakingLink
    {
        // Variables
        private const string LauncherField = "placelauncherurl:";
        private static readonly string[] TargetKeys =
        {
            "gameId", "gameInstanceId", "jobId", "accessCode", "linkCode", "privateServerLinkCode",
            "userId", "followUserId", "reservedServerAccessCode"
        };

        private readonly string[]? _fields;
        private readonly int _index;
        private readonly string _url;

        public long PlaceId { get; }

        private MatchmakingLink(long placeId, string url, string[]? fields, int index)
        {
            PlaceId = placeId;
            _url = url;
            _fields = fields;
            _index = index;
        }

        // Functions
        public static MatchmakingLink? Parse(string args)
        {
            string[]? fields = null;
            int index = -1;
            string url = args;

            if (args.StartsWith("roblox-player:", StringComparison.OrdinalIgnoreCase))
            {
                fields = args.Split('+');
                index = Array.FindIndex(fields, IsLauncherField);
                if (index < 0 || Array.FindIndex(fields, index + 1, IsLauncherField) >= 0)
                    return null;

                url = HttpUtility.UrlDecode(fields[index][LauncherField.Length..]);
                if (url.Contains("friend", StringComparison.OrdinalIgnoreCase)
                    || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
                    || !uri.Host.EndsWith(".roblox.com", StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            int start = url.IndexOf('?');
            if (start < 0 || url.Contains('#'))
                return null;

            string baseUrl = url[..start];
            if (fields is null && !baseUrl.Equals("roblox://experiences/start", StringComparison.OrdinalIgnoreCase))
                return null;

            var query = url[(start + 1)..].Split('&');
            long placeId = 0;
            int request = -1;

            for (int i = 0; i < query.Length; i++)
            {
                string[] pair = query[i].Split('=', 2);
                string key = HttpUtility.UrlDecode(pair[0]);
                if (TargetKeys.Contains(key, StringComparer.OrdinalIgnoreCase) || key.Contains("friend", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (key.Equals("placeId", StringComparison.OrdinalIgnoreCase))
                {
                    if (placeId != 0 || pair.Length != 2 || !long.TryParse(HttpUtility.UrlDecode(pair[1]), NumberStyles.None, CultureInfo.InvariantCulture, out placeId) || placeId <= 0)
                        return null;
                }
                else if (key.Equals("request", StringComparison.OrdinalIgnoreCase))
                {
                    if (request >= 0 || fields is null || pair.Length != 2 || HttpUtility.UrlDecode(pair[1]) != "RequestGame")
                        return null;

                    request = i;
                }
            }

            if (placeId == 0 || (fields is not null && request < 0))
                return null;

            if (fields is null)
                return new MatchmakingLink(placeId, url, null, -1);

            query[request] = "request=RequestGameJob";
            return new MatchmakingLink(placeId, $"{baseUrl}?{String.Join("&", query)}", fields, index);
        }

        private static bool IsLauncherField(string field) => field.StartsWith(LauncherField, StringComparison.OrdinalIgnoreCase);

        public string ForServer(Guid serverId)
        {
            if (_fields is null)
                return $"{_url}&gameInstanceId={serverId}";

            string url = $"{_url}&gameId={serverId}";
            var fields = (string[])_fields.Clone();
            fields[_index] = LauncherField + HttpUtility.UrlEncode(url);
            return String.Join("+", fields);
        }
    }
}
