using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        private readonly IThemeService _themeService = new ThemeService();

        protected virtual bool AllowCustomBackground => false;

        private BackgroundType _backdrop = BackgroundType.None;

        private readonly RectangleGeometry _backgroundArea = new(new Rect(0, 0, 1, 1));

        public WpfUiWindow()
        {
            ApplyTheme();

            SizeChanged += (_, e) => _backgroundArea.Rect = new Rect(0, 0, Math.Max(e.NewSize.Width, 1), Math.Max(e.NewSize.Height, 1));
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2; // index for CustomTheme merged dictionary

            var themeType = App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Dark ? ThemeType.Dark : ThemeType.Light;

            _themeService.SetTheme(themeType);

            if (ModGenerator.TryParseColor(App.Settings.Prop.AccentColor, out var accent))
                Accent.Apply(accent, themeType);
            else
                _themeService.SetSystemAccent();

            // there doesn't seem to be a way to query the name for merged dictionaries
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(App.Settings.Prop.Theme.GetFinal())}.xaml") };
            Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

            if (!String.IsNullOrWhiteSpace(App.Settings.Prop.AppFont))
                FontFamily = new System.Windows.Media.FontFamily(App.Settings.Prop.AppFont);
            else
                ClearValue(FontFamilyProperty);

            if (IsInitialized)
                ApplyCustomBackground();

#if QA_BUILD
            this.BorderBrush = System.Windows.Media.Brushes.Red;
            this.BorderThickness = new Thickness(4);
#endif
        }

        private Brush? GetCustomBackground()
        {
            const string LOG_IDENT = "WpfUiWindow::GetCustomBackground";

            var settings = App.Settings.Prop;
            string image = settings.BackgroundImage;

            if (settings.BackgroundMode == BackgroundMode.Color)
                return ModGenerator.TryParseColor(settings.BackgroundColor, out var color) ? new SolidColorBrush(color) { Opacity = settings.BackgroundOpacity } : null;

            if (settings.BackgroundMode == BackgroundMode.Gradient)
                return GetGradient(settings);

            if (settings.BackgroundMode == BackgroundMode.Image && !String.IsNullOrEmpty(image) && File.Exists(image))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(image);
                    bitmap.EndInit();

                    var stretch = App.Settings.Prop.BackgroundStretch switch
                    {
                        BackgroundStretch.Fill => Stretch.Fill,
                        BackgroundStretch.Uniform => Stretch.Uniform,
                        BackgroundStretch.None => Stretch.None,
                        _ => Stretch.UniformToFill
                    };

                    return new ImageBrush(bitmap) { Stretch = stretch, Opacity = App.Settings.Prop.BackgroundOpacity };
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to load background image {image}");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            return null;
        }

        public static List<(string Hex, double Offset)> ParseGradient(string value)
        {
            var stops = new List<(string, double)>();
            string[] entries = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < entries.Length; i++)
            {
                string[] parts = entries[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                double offset = entries.Length == 1 ? 0 : i / (double)(entries.Length - 1);

                if (parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                    offset = Math.Clamp(parsed, 0, 1);

                stops.Add((parts[0], offset));
            }

            return stops;
        }

        private static Brush? GetGradient(Models.Persistable.Settings settings)
        {
            var stops = new GradientStopCollection();

            foreach (var (hex, offset) in ParseGradient(settings.BackgroundGradient))
            {
                if (ModGenerator.TryParseColor(hex, out var color))
                    stops.Add(new GradientStop(color, offset));
            }

            if (stops.Count == 0)
                return null;

            if (settings.BackgroundGradientRadial)
                return new RadialGradientBrush(stops) { Opacity = settings.BackgroundOpacity };

            double radians = settings.BackgroundGradientAngle * Math.PI / 180;
            double x = Math.Cos(radians) / 2, y = Math.Sin(radians) / 2;

            return new LinearGradientBrush(stops, new Point(0.5 - x, 0.5 - y), new Point(0.5 + x, 0.5 + y)) { Opacity = settings.BackgroundOpacity };
        }

        private void ApplyCustomBackground()
        {
            if (AllowCustomBackground)
                SetCustomBackground(GetCustomBackground());
        }

        private void SetCustomBackground(Brush? custom)
        {
            if (custom is null)
            {
                if (_backdrop != BackgroundType.None && new WindowInteropHelper(this).Handle != IntPtr.Zero)
                    Wpf.Ui.Appearance.Background.Apply(this, _backdrop, true);
                else
                    SetResourceReference(BackgroundProperty, "ApplicationBackgroundBrush");

                return;
            }

            var themeBackground = TryFindResource("ApplicationBackgroundBrush") as Brush;

            var group = new DrawingBrush
            {
                Stretch = Stretch.Fill,
                Drawing = new DrawingGroup
                {
                    Children =
                    {
                        new GeometryDrawing(themeBackground ?? Brushes.Black, null, _backgroundArea),
                        new GeometryDrawing(custom, null, _backgroundArea)
                    }
                }
            };

            WindowBackdropType = BackgroundType.None;

            if (new WindowInteropHelper(this).Handle != IntPtr.Zero)
                Wpf.Ui.Appearance.Background.Remove(this);

            Background = group;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            if (WindowBackdropType == BackgroundType.Mica)
            {
                _backdrop = App.Settings.Prop.WindowBackdrop switch
                {
                    Enums.WindowBackdrop.Acrylic => BackgroundType.Acrylic,
                    Enums.WindowBackdrop.Aero => BackgroundType.Aero,
                    Enums.WindowBackdrop.None => BackgroundType.None,
                    _ => BackgroundType.Mica
                };

                WindowBackdropType = _backdrop == BackgroundType.Aero ? BackgroundType.None : _backdrop;
            }
            else
            {
                _backdrop = WindowBackdropType;
            }

            var custom = AllowCustomBackground ? GetCustomBackground() : null;

            if (AllowCustomBackground)
                SetCustomBackground(custom);

            base.OnSourceInitialized(e);

            if (custom is not null)
                SetCustomBackground(custom);
            else if (_backdrop == BackgroundType.Aero)
                Wpf.Ui.Appearance.Background.Apply(this, BackgroundType.Aero, true);
        }
    }
}
