namespace Bloxstrap.Utility
{
    internal static class Thumbnails
    {
        // TODO: remove requests from list once they're finished or failed
        public static Task<string?[]> GetThumbnailUrlsAsync(List<ThumbnailRequest> requests, CancellationToken token) =>
            GetThumbnailUrlsAsync(requests, token, App.HttpClient);

        internal static async Task<string?[]> GetThumbnailUrlsAsync(List<ThumbnailRequest> requests, CancellationToken token, HttpClient client)
        {
            const string LOG_IDENT = "Thumbnails::GetThumbnailUrlsAsync";
            const int RETRIES = 5;
            const int RETRY_TIME_INCREMENT = 500; // ms

            string?[] urls = new string?[requests.Count];

            // assign unique request ids to each request
            for (int i = 0; i < requests.Count; i++)
                requests[i].RequestId = i.ToString();

            using var payload = new StringContent(JsonSerializer.Serialize(requests), Encoding.UTF8, "application/json");

            ThumbnailResponse[] response = null!;

            for (int i = 1; i <= RETRIES; i++)
            {
                var json = await client.PostFromJsonWithRetriesAsync<ThumbnailBatchResponse>("https://thumbnails.roblox.com/v1/batch", payload, 3, token);
                if (json == null)
                    throw new InvalidHTTPResponseException("Deserialised ThumbnailBatchResponse is null");

                response = json.Data ?? Array.Empty<ThumbnailResponse>();

                bool finished = response.All(x => x.State != "Pending");
                if (finished)
                    break;

                if (i == RETRIES)
                    App.Logger.WriteLine(LOG_IDENT, "Ran out of retries");
                else
                    await Task.Delay(RETRY_TIME_INCREMENT * i, token);
            }

            foreach (var item in response)
            {
                if (item.State == "Pending")
                    App.Logger.WriteLine(LOG_IDENT, $"{item.TargetId} is still pending");
                else if (item.State == "Error")
                    App.Logger.WriteLine(LOG_IDENT, $"{item.TargetId} got error code {item.ErrorCode} ({item.ErrorMessage})");
                else if (item.State != "Completed")
                    App.Logger.WriteLine(LOG_IDENT, $"{item.TargetId} got \"{item.State}\"");

                if (item.State == "Completed" && int.TryParse(item.RequestId, out int index) && index >= 0 && index < urls.Length)
                    urls[index] = item.ImageUrl;
            }

            return urls;
        }

        public static async Task<string?> GetThumbnailUrlAsync(ThumbnailRequest request, CancellationToken token)
        {
            var urls = await GetThumbnailUrlsAsync(new List<ThumbnailRequest> { request }, token);
            return urls[0];
        }
    }
}
