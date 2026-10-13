using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

using Bloxstrap.UI.Elements.About;

namespace Bloxstrap.UI.ViewModels.Installer
{
    public class RecentGameItem
    {
        public long PlaceId { get; init; }

        public string Name { get; init; } = "";

        public string? IconUrl { get; init; }

        public string UpdatedText { get; init; } = "";

        public Visibility UpdatedVisibility => String.IsNullOrEmpty(UpdatedText) ? Visibility.Collapsed : Visibility.Visible;
    }

    public class LaunchMenuViewModel
    {
        public string Version => string.Format(Strings.Menu_About_Version, App.Version);

        public ICommand LaunchSettingsCommand => new RelayCommand(LaunchSettings);

        public ICommand LaunchRobloxCommand => new RelayCommand(LaunchRoblox);

        public ICommand LaunchRobloxStudioCommand => new RelayCommand(LaunchRobloxStudio);

        public ICommand LaunchRobloxSafeCommand => new RelayCommand(() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRobloxSafe));

        public ICommand LaunchAboutCommand => new RelayCommand(LaunchAbout);

        public ICommand OpenDiscordCommand => new RelayCommand(() => Frontend.ShowMessageBox(Strings.LaunchMenu_DiscordUnavailable, MessageBoxImage.Warning));

        public event EventHandler<NextAction>? CloseWindowRequest;

        private readonly List<GameStatsEntry> _recentEntries = App.Settings.Prop.TrackGameStats ? GameStatsTracker.GetRecent(5).ToList() : new();

        public ObservableCollection<RecentGameItem> RecentGames { get; } = new();

        public LaunchMenuViewModel()
        {
            foreach (var entry in _recentEntries)
                RecentGames.Add(new RecentGameItem { PlaceId = entry.PlaceId, Name = entry.Name, IconUrl = entry.IconUrl });

            _ = ShowGameUpdates();
        }

        private async Task ShowGameUpdates()
        {
            var times = await GameUpdates.GetUpdateTimes(_recentEntries.Select(x => x.UniverseId));

            for (int i = 0; i < RecentGames.Count && i < _recentEntries.Count; i++)
            {
                var entry = _recentEntries[i];

                if (times.TryGetValue(entry.UniverseId, out var updated) && GameUpdates.Describe(updated, entry.LastPlayed) is string text)
                    RecentGames[i] = new RecentGameItem { PlaceId = entry.PlaceId, Name = entry.Name, IconUrl = entry.IconUrl, UpdatedText = text };
            }
        }

        public Visibility RecentGamesVisibility => RecentGames.Any() ? Visibility.Visible : Visibility.Collapsed;

        public ICommand JoinRecentGameCommand => new RelayCommand<long>(JoinRecentGame);

        private void JoinRecentGame(long placeId)
        {
            Process.Start(Paths.Process, $"roblox://experiences/start?placeId={placeId}");
            CloseWindowRequest?.Invoke(this, NextAction.Terminate);
        }

        private void LaunchSettings() => CloseWindowRequest?.Invoke(this, NextAction.LaunchSettings);

        private void LaunchRoblox() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRoblox);

        private void LaunchRobloxStudio() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRobloxStudio);

        private void LaunchAbout() => new MainWindow().ShowDialog();
    }
}
