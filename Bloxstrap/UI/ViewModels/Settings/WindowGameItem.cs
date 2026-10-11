namespace Bloxstrap.UI.ViewModels.Settings
{
    public class WindowGameItem : NotifyPropertyChangedViewModel
    {
        // Properties

        public static readonly string[] Features = { "Move", "Title", "Transparency", "Color", "Wallpaper", "Desktop" };

        private readonly Action<WindowGameItem> _changed;

        private string _name;

        private string? _iconUrl;

        private int _status;

        private readonly HashSet<string> _disabled;

        public long UniverseId { get; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string? IconUrl
        {
            get => _iconUrl;
            set
            {
                _iconUrl = value;
                OnPropertyChanged(nameof(IconUrl));
            }
        }

        public string IdText => UniverseId.ToString();

        public int Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(FeaturesEnabled));
                _changed(this);
            }
        }

        public bool FeaturesEnabled => _status != 2;

        public IReadOnlyCollection<string> Disabled => _disabled;

        public bool Move { get => Get("Move"); set => Set("Move", value); }

        public bool Title { get => Get("Title"); set => Set("Title", value); }

        public bool Transparency { get => Get("Transparency"); set => Set("Transparency", value); }

        public bool Color { get => Get("Color"); set => Set("Color", value); }

        public bool Wallpaper { get => Get("Wallpaper"); set => Set("Wallpaper", value); }

        public bool Desktop { get => Get("Desktop"); set => Set("Desktop", value); }

        // Functions

        public WindowGameItem(long universeId, int status, IEnumerable<string> disabled, Action<WindowGameItem> changed)
        {
            UniverseId = universeId;
            _name = universeId.ToString();
            _status = status;
            _disabled = new HashSet<string>(disabled);
            _changed = changed;
        }

        private bool Get(string feature) => !_disabled.Contains(feature);

        private void Set(string feature, bool enabled)
        {
            if (enabled)
                _disabled.Remove(feature);
            else
                _disabled.Add(feature);

            OnPropertyChanged(feature);
            _changed(this);
        }
    }
}
