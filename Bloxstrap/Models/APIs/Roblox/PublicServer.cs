namespace Bloxstrap.Models.APIs.Roblox
{
    internal class PublicServer
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("playing")]
        public int Playing { get; set; } = -1;

        [JsonPropertyName("maxPlayers")]
        public int MaxPlayers { get; set; }

        [JsonPropertyName("ping")]
        public int? Ping { get; set; }
    }
}
