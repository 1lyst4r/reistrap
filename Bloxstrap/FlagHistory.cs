namespace Bloxstrap
{
    public record FlagSnapshot(string Path, DateTime Time, int Count);

    public static class FlagHistory
    {
        private const int MaxSnapshots = 20;

        public static string Folder => Path.Combine(Paths.Base, "FlagHistory");

        public static void Snapshot(IDictionary<string, object>? previous, IDictionary<string, object> current)
        {
            const string LOG_IDENT = "FlagHistory::Snapshot";

            if (previous is null || !App.FastFlags.IsSaved)
                return;

            var before = previous.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? "");

            if (before.Count == current.Count && before.All(x => current.TryGetValue(x.Key, out var value) && value?.ToString() == x.Value))
                return;

            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Path.Combine(Folder, $"{DateTime.Now:yyyyMMdd-HHmmss}.json"), JsonSerializer.Serialize(before, new JsonSerializerOptions { WriteIndented = true }));

                foreach (var old in List().Skip(MaxSnapshots))
                    File.Delete(old.Path);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static List<FlagSnapshot> List()
        {
            if (!Directory.Exists(Folder))
                return new();

            var snapshots = new List<FlagSnapshot>();

            foreach (string file in Directory.GetFiles(Folder, "*.json"))
            {
                if (!DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                    continue;

                int count = 0;

                try
                {
                    count = Load(file)?.Count ?? 0;
                }
                catch
                {
                    continue;
                }

                snapshots.Add(new FlagSnapshot(file, time, count));
            }

            return snapshots.OrderByDescending(x => x.Time).ToList();
        }

        public static Dictionary<string, string>? Load(string path) =>
            JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));

        public static void Restore(FlagSnapshot snapshot)
        {
            var flags = Load(snapshot.Path) ?? new();

            foreach (string name in App.FastFlags.Prop.Keys.ToList())
                App.FastFlags.SetValue(name, null);

            foreach (var flag in flags)
                App.FastFlags.SetValue(flag.Key, flag.Value);
        }
    }
}
