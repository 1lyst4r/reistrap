using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Bloxstrap.UI.ViewModels.Settings;

using Button = System.Windows.Controls.Button;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class SetupDialog : Window
    {
        private class Feature
        {
            public string Title = "";
            public string Description = "";
            public Func<bool> Get = null!;
            public Action<bool> Set = null!;
            public ToggleButton? Row;
        }

        private readonly Dictionary<string, string> _importSources = SettingsImporter.FindInstalls();

        private readonly List<(string Label, FrameworkElement Element)> _steps = new();

        private readonly List<Feature> _features;

        private readonly bool _animate = SystemParameters.ClientAreaAnimation && !App.Settings.Prop.DisableAnimations;

        private readonly List<RadioButton> _importChoices = new();

        private readonly List<(Theme Theme, RadioButton Button)> _themeChoices = new();

        private readonly List<(string Color, RadioButton Button)> _accentChoices = new();

        private RadioButton _balanced = null!, _performance = null!, _quality = null!;

        private int _step;

        private bool _imported;

        public SetupDialog()
        {
            InitializeComponent();

            var settings = App.Settings.Prop;

            _features = new()
            {
                new() { Title = Strings.Setup_Feature_Discord_Title, Description = Strings.Setup_Feature_Discord_Description, Get = () => settings.UseDiscordRichPresence, Set = x => settings.UseDiscordRichPresence = x },
                new() { Title = Strings.Setup_Feature_Location_Title, Description = Strings.Setup_Feature_Location_Description, Get = () => settings.ShowServerDetails, Set = x => settings.ShowServerDetails = x },
                new() { Title = Strings.Setup_Feature_Playtime_Title, Description = Strings.Setup_Feature_Playtime_Description, Get = () => settings.TrackGameStats, Set = x => settings.TrackGameStats = x },
                new() { Title = Strings.Setup_Feature_Rejoin_Title, Description = Strings.Setup_Feature_Rejoin_Description, Get = () => settings.AutoRejoin, Set = x => settings.AutoRejoin = x },
                new() { Title = Strings.Setup_Feature_MultiInstance_Title, Description = Strings.Setup_Feature_MultiInstance_Description, Get = () => settings.MultiInstanceLaunching, Set = x => settings.MultiInstanceLaunching = x },
                new() { Title = Strings.Setup_Feature_Socd_Title, Description = Strings.Setup_Feature_Socd_Description, Get = () => settings.SocdEnabled, Set = x => settings.SocdEnabled = x }
            };

            if (_importSources.Any())
                _steps.Add((Strings.Setup_Rail_Import, ImportStep));
            else
                ImportStep.Visibility = Visibility.Collapsed;

            _steps.Add((Strings.Setup_Rail_Look, LookStep));
            _steps.Add((Strings.Setup_Rail_Features, FeaturesStep));
            _steps.Add((Strings.Setup_Rail_Performance, PerformanceStep));

            WelcomeBody.Text = String.Format(Strings.Setup_Welcome_Subheading, _steps.Count);

            BuildImportChoices();
            BuildThemeChoices();
            BuildAccentChoices();
            BuildFeatureChoices();
            BuildPerformanceChoices();
            LoadChoices();
        }

        // Building

        private static TextBlock Text(string text, string style, FrameworkElement owner) => new() { Text = text, Style = (Style)owner.FindResource(style) };

        private StackPanel RowContent(string title, string description)
        {
            var content = new StackPanel();
            content.Children.Add(Text(title, "RowTitle", this));
            content.Children.Add(Text(description, "RowText", this));
            return content;
        }

        private RadioButton OptionRow(string group, string title, string description, Panel parent)
        {
            var button = new RadioButton { GroupName = group, Style = (Style)FindResource("OptionRow"), Content = RowContent(title, description) };
            parent.Children.Add(button);
            return button;
        }

        private void BuildImportChoices()
        {
            foreach (var source in _importSources.Keys)
            {
                var button = OptionRow("Import", String.Format(Strings.Setup_Import_From, source), Strings.Setup_Import_From_Description, ImportChoices);
                button.Tag = source;
                button.IsChecked = _importChoices.Count == 0;
                _importChoices.Add(button);
            }

            _importChoices.Add(OptionRow("Import", Strings.Setup_Import_Fresh_Title, Strings.Setup_Import_Fresh_Description, ImportChoices));
        }

        private static Border WindowPreview(Brush background, Brush bar, Brush line)
        {
            var stack = new StackPanel();
            stack.Children.Add(new Border { Height = 10, Background = bar });

            var body = new StackPanel { Margin = new Thickness(10, 9, 10, 0) };
            body.Children.Add(new Border { Height = 6, Width = 54, HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3), Background = line });
            body.Children.Add(new Border { Height = 6, Width = 38, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3), Background = line });
            stack.Children.Add(body);

            return new Border { Height = 64, CornerRadius = new CornerRadius(5), Background = background, Child = stack, ClipToBounds = true };
        }

        private void BuildThemeChoices()
        {
            var light = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
            var lightBar = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xE2));
            var lightLine = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8));
            var dark = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x28));
            var darkBar = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x38));
            var darkLine = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x52));

            var split = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
            split.GradientStops.Add(new GradientStop(light.Color, 0.5));
            split.GradientStops.Add(new GradientStop(dark.Color, 0.5));

            var splitBar = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
            splitBar.GradientStops.Add(new GradientStop(lightBar.Color, 0.5));
            splitBar.GradientStops.Add(new GradientStop(darkBar.Color, 0.5));

            var options = new (Theme Theme, string Label, Border Preview)[]
            {
                (Theme.Default, Strings.Common_SystemDefault, WindowPreview(split, splitBar, darkLine)),
                (Theme.Light, Strings.Setup_Theme_Light, WindowPreview(light, lightBar, lightLine)),
                (Theme.Dark, Strings.Setup_Theme_Dark, WindowPreview(dark, darkBar, darkLine))
            };

            foreach (var option in options)
            {
                var button = new RadioButton { GroupName = "Theme", Style = (Style)FindResource("ThemeOption"), Content = option.Preview, Tag = option.Label };
                _themeChoices.Add((option.Theme, button));
                ThemeChoices.Children.Add(button);
            }
        }

        private void BuildAccentChoices()
        {
            foreach (var preset in AppearanceViewModel.AccentPresets)
            {
                Color color = String.IsNullOrEmpty(preset.Value) ? Wpf.Ui.Appearance.Accent.GetColorizationColor() : (Color)ColorConverter.ConvertFromString(preset.Value);

                var button = new RadioButton { GroupName = "Accent", Style = (Style)FindResource("Swatch"), Background = new SolidColorBrush(color), ToolTip = preset.Key };
                button.Checked += (_, _) => SetAccent(color);

                _accentChoices.Add((preset.Value, button));
                AccentChoices.Children.Add(button);
            }
        }

        private void BuildFeatureChoices()
        {
            foreach (var feature in _features)
            {
                var row = new ToggleButton { Style = (Style)FindResource("SwitchRow"), Content = RowContent(feature.Title, feature.Description) };
                feature.Row = row;
                FeatureChoices.Children.Add(row);
            }
        }

        private void BuildPerformanceChoices()
        {
            _balanced = OptionRow("Performance", Strings.Setup_Performance_Default_Title, Strings.Setup_Performance_Default_Description, PerformanceChoices);
            _performance = OptionRow("Performance", Strings.Setup_Performance_Fps_Title, Strings.Setup_Performance_Fps_Description, PerformanceChoices);
            _quality = OptionRow("Performance", Strings.Setup_Performance_Quality_Title, Strings.Setup_Performance_Quality_Description, PerformanceChoices);
            _balanced.IsChecked = true;
        }

        private void LoadChoices()
        {
            var settings = App.Settings.Prop;

            foreach (var choice in _themeChoices)
                choice.Button.IsChecked = choice.Theme == settings.Theme;

            var accent = _accentChoices.FirstOrDefault(x => String.Equals(x.Color, settings.AccentColor, StringComparison.OrdinalIgnoreCase));
            (accent.Button ?? _accentChoices[0].Button).IsChecked = true;

            foreach (var feature in _features)
                feature.Row!.IsChecked = feature.Get();
        }

        // Accent

        private void SetAccent(Color color)
        {
            double luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;

            Resources["AccentBrush"] = new SolidColorBrush(color);
            Resources["OnAccentBrush"] = new SolidColorBrush(luminance > 0.45 ? Color.FromRgb(0x0B, 0x0C, 0x0E) : Colors.White);
        }

        // Steps

        private void BuildRail()
        {
            StepList.Children.Clear();

            for (int i = 0; i < _steps.Count; i++)
            {
                bool current = i == _step;

                StepList.Children.Add(new TextBlock
                {
                    Text = _steps[i].Label,
                    Margin = new Thickness(0, 0, 0, 16),
                    FontSize = 14,
                    FontWeight = current ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = current ? new SolidColorBrush(Color.FromRgb(0xED, 0xEE, 0xF0)) : (Brush)FindResource(i < _step ? "MutedBrush" : "DimBrush")
                });
            }
        }

        private void ShowStep(int direction)
        {
            for (int i = 0; i < _steps.Count; i++)
                _steps[i].Element.Visibility = i == _step ? Visibility.Visible : Visibility.Collapsed;

            bool last = _step == _steps.Count - 1;

            BackButton.Visibility = _step == 0 ? Visibility.Hidden : Visibility.Visible;
            NextButton.Content = last ? Strings.Setup_Finish : Strings.Setup_Next;
            StepCounter.Text = String.Format(Strings.Setup_Step, _step + 1, _steps.Count);

            BuildRail();

            if (!_animate)
                return;

            var translate = (TranslateTransform)StepHost.RenderTransform;
            var ease = new QuinticEase { EasingMode = EasingMode.EaseOut };

            translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(16 * direction, 0, TimeSpan.FromMilliseconds(280)) { EasingFunction = ease });
            StepHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
        }

        // Animation

        private static void Animate(IAnimatable target, DependencyProperty property, double from, double to, int delayMs, int durationMs)
        {
            target.BeginAnimation(property, new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMs))
            {
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            });
        }

        private void PlayIntro()
        {
            var tiles = new (Rectangle Tile, double X, double Y)[]
            {
                (TileTL, -24, -24),
                (TileTR, 24, -24),
                (TileBL, -24, 24),
                (TileBR, 24, 24)
            };

            if (!_animate)
            {
                foreach (var tile in tiles)
                    tile.Tile.Opacity = 1;

                Chip.Opacity = 1;
                WelcomeText.Opacity = 1;
                return;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                var translate = (TranslateTransform)tiles[i].Tile.RenderTransform;
                int delay = 100 + i * 60;

                Animate(tiles[i].Tile, OpacityProperty, 0, 1, delay, 300);
                Animate(translate, TranslateTransform.XProperty, tiles[i].X, 0, delay, 600);
                Animate(translate, TranslateTransform.YProperty, tiles[i].Y, 0, delay, 600);
            }

            Animate(Chip, OpacityProperty, 0, 1, 420, 300);
            Animate(WelcomeText, OpacityProperty, 0, 1, 520, 400);
        }

        // Saving

        private void Import()
        {
            const string LOG_IDENT = "SetupDialog::Import";

            if (_imported || _importChoices.FirstOrDefault(x => x.IsChecked == true)?.Tag is not string source || !_importSources.TryGetValue(source, out string? folder))
                return;

            try
            {
                SettingsImporter.ImportSettings(folder);
                SettingsImporter.ImportFastFlags(folder);
                SettingsImporter.ImportMods(folder);
                _imported = true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(ex.Message, MessageBoxImage.Error);
            }

            LoadChoices();
        }

        private void ApplyChoices()
        {
            var settings = App.Settings.Prop;

            var theme = _themeChoices.FirstOrDefault(x => x.Button.IsChecked == true);

            if (theme.Button is not null)
                settings.Theme = theme.Theme;

            var accent = _accentChoices.FirstOrDefault(x => x.Button.IsChecked == true);

            if (accent.Button is not null)
                settings.AccentColor = accent.Color;

            foreach (var feature in _features)
                feature.Set(feature.Row!.IsChecked == true);

            if (_performance.IsChecked == true)
            {
                App.FastFlags.SetPreset("Rendering.MSAA", "1");
                App.FastFlags.SetPreset("Geometry.Grass", "0");
                App.FastFlags.SetPresetEnum("Rendering.Mode", "D3D11", "True");
            }
            else if (_quality.IsChecked == true)
            {
                App.FastFlags.SetPreset("Rendering.MSAA", "4");
                App.FastFlags.SetPreset("Rendering.TextureQuality.OverrideEnabled", "True");
                App.FastFlags.SetPreset("Rendering.TextureQuality.Level", "3");
                App.FastFlags.SetPreset("Rendering.FRMQuality", "21");
            }
        }

        private void Finish(bool apply)
        {
            if (apply)
                ApplyChoices();

            if (apply || _imported)
            {
                App.Settings.Save();
                App.FastFlags.Save();
            }

            App.State.Prop.ShowWelcome = false;
            App.State.Save();

            Close();
        }

        // Events

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int round = 2;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref round, sizeof(int));

            PlayIntro();
            Activate();
        }

        private void Background_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not ButtonBase)
                DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Finish(false);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StepsView.Visibility = Visibility.Visible;
            ShowStep(1);

            if (!_animate)
            {
                WelcomeView.Visibility = Visibility.Collapsed;
                StepsView.Opacity = 1;
                return;
            }

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160));
            fadeOut.Completed += (_, _) => WelcomeView.Visibility = Visibility.Collapsed;
            WelcomeView.BeginAnimation(OpacityProperty, fadeOut);

            Animate(StepsView, OpacityProperty, 0, 1, 80, 240);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_steps[_step].Element == ImportStep)
                Import();

            if (_step == _steps.Count - 1)
            {
                Finish(true);
                return;
            }

            _step++;
            ShowStep(1);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_step == 0)
                return;

            _step--;
            ShowStep(-1);
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e) => Finish(false);
    }
}
