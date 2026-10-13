using Bloxstrap.Models.Entities;

namespace Bloxstrap
{
    public static class GameUpdates
    {
        public static async Task<Dictionary<long, DateTime>> GetUpdateTimes(IEnumerable<long> universeIds)
        {
            const string LOG_IDENT = "GameUpdates::GetUpdateTimes";

            var ids = universeIds.Where(x => x != 0).Distinct().ToList();
            var times = new Dictionary<long, DateTime>();

            if (!ids.Any())
                return times;

            try
            {
                await UniverseDetails.FetchBulk(String.Join(',', ids));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                return times;
            }

            foreach (long id in ids)
            {
                if (UniverseDetails.LoadFromCache(id) is { } details && details.Data.Updated != default)
                    times[id] = details.Data.Updated.ToLocalTime();
            }

            return times;
        }

        public static string? Describe(DateTime updated, DateTime lastPlayed)
        {
            if (updated <= lastPlayed)
                return null;

            var ago = DateTime.Now - updated;

            string when = ago.TotalHours < 1
                ? String.Format(Strings.Common_TimeAgo_Minutes, Math.Max(1, (int)ago.TotalMinutes))
                : ago.TotalDays < 1
                    ? String.Format(Strings.Common_TimeAgo_Hours, (int)ago.TotalHours)
                    : String.Format(Strings.Common_TimeAgo_Days, (int)ago.TotalDays);

            return String.Format(Strings.Common_GameUpdated, when);
        }
    }
}
