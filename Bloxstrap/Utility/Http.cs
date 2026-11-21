namespace Bloxstrap.Utility
{
    internal static class Http
    {
        public static async Task<T> GetJson<T>(string url, CancellationToken cancellationToken = default)
        {
            using var request = await App.HttpClient.GetAsync(url, cancellationToken);

            request.EnsureSuccessStatusCode();

            string json = await request.Content.ReadAsStringAsync(cancellationToken);
            
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }
}
