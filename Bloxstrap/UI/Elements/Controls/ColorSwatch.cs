using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Bloxstrap.UI.Elements.Controls
{
    public class ColorSwatch : Border
    {
        // Properties

        public static readonly DependencyProperty HexProperty = DependencyProperty.Register(
            nameof(Hex), typeof(string), typeof(ColorSwatch),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((ColorSwatch)d).OnHexChanged()));

        public static readonly DependencyProperty LiveProperty = DependencyProperty.Register(
            nameof(Live), typeof(bool), typeof(ColorSwatch), new PropertyMetadata(true));

        public string Hex
        {
            get => (string)GetValue(HexProperty);
            set => SetValue(HexProperty, value);
        }

        public bool Live
        {
            get => (bool)GetValue(LiveProperty);
            set => SetValue(LiveProperty, value);
        }

        private static readonly string[] Presets =
        {
            "#FFFFFF", "#9AA0A6", "#1E1E2E", "#000000", "#F38BA8", "#FAB387", "#F9E2AF",
            "#A6E3A1", "#94E2D5", "#89DCEB", "#89B4FA", "#B4BEFE", "#CBA6F7", "#F5C2E7"
        };

        private const double PadWidth = 220, PadHeight = 150, BarHeight = 14;

        private Popup? _popup;
        private Border _pad = null!;
        private Ellipse _padThumb = null!;
        private Border _hueThumb = null!;
        private Border _preview = null!;
        private TextBox _hexBox = null!;
        private readonly DispatcherTimer _commitTimer;

        private double _hue, _saturation, _value = 1;
        private string? _pending;
        private bool _updatingText;

        // Functions

        public ColorSwatch()
        {
            Width = 24;
            Height = 24;
            CornerRadius = new CornerRadius(12);
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;
            SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");

            _commitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
            _commitTimer.Tick += (_, _) => Commit();

            MouseLeftButtonUp += (_, e) =>
            {
                Open();
                e.Handled = true;
            };

            OnHexChanged();
        }

        private void OnHexChanged()
        {
            Background = ModGenerator.TryParseColor(Hex ?? "", out var color) ? new SolidColorBrush(color) : Brushes.Transparent;
        }

        private void Open()
        {
            _popup ??= Build();

            if (ModGenerator.TryParseColor(Hex ?? "", out var color))
                (_hue, _saturation, _value) = ToHsv(color);

            Refresh(true);
            _popup.IsOpen = true;
        }

        private Popup Build()
        {
            var popup = new Popup
            {
                PlacementTarget = this,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 6,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            popup.Closed += (_, _) => Commit();

            _pad = new Border { Width = PadWidth, Height = PadHeight, CornerRadius = new CornerRadius(6), ClipToBounds = true, Cursor = Cursors.Cross };

            var whiteLayer = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new LinearGradientBrush(Colors.White, Color.FromArgb(0, 255, 255, 255), new Point(0, 0), new Point(1, 0))
            };

            var blackLayer = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Colors.Black, new Point(0, 0), new Point(0, 1))
            };

            _padThumb = new Ellipse { Width = 14, Height = 14, Stroke = Brushes.White, StrokeThickness = 2, IsHitTestVisible = false };
            _padThumb.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.6 };

            var padCanvas = new Canvas { IsHitTestVisible = false };
            padCanvas.Children.Add(_padThumb);

            var padGrid = new Grid { Width = PadWidth, Height = PadHeight, Background = Brushes.Transparent, Cursor = Cursors.Cross };
            padGrid.Children.Add(_pad);
            padGrid.Children.Add(whiteLayer);
            padGrid.Children.Add(blackLayer);
            padGrid.Children.Add(padCanvas);
            Drag(padGrid, p =>
            {
                _saturation = Math.Clamp(p.X / PadWidth, 0, 1);
                _value = 1 - Math.Clamp(p.Y / PadHeight, 0, 1);
            });

            var hueStops = new GradientStopCollection();
            for (int i = 0; i <= 6; i++)
                hueStops.Add(new GradientStop(FromHsv(i * 60 % 360, 1, 1), i / 6.0));

            _hueThumb = new Border { Width = 6, Height = BarHeight + 4, CornerRadius = new CornerRadius(3), BorderBrush = Brushes.White, BorderThickness = new Thickness(2), IsHitTestVisible = false, Margin = new Thickness(0, -2, 0, 0) };

            var hueCanvas = new Canvas { IsHitTestVisible = false };
            hueCanvas.Children.Add(_hueThumb);

            var hueGrid = new Grid { Width = PadWidth, Height = BarHeight, Margin = new Thickness(0, 10, 0, 0), Cursor = Cursors.Hand };
            hueGrid.Children.Add(new Border { CornerRadius = new CornerRadius(BarHeight / 2), Background = new LinearGradientBrush(hueStops, new Point(0, 0), new Point(1, 0)) });
            hueGrid.Children.Add(hueCanvas);
            Drag(hueGrid, p => _hue = Math.Clamp(p.X / PadWidth, 0, 1) * 359.9);

            _preview = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1) };
            _preview.SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");

            _hexBox = new TextBox { Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(8, 4, 8, 4), VerticalContentAlignment = VerticalAlignment.Center };
            _hexBox.TextChanged += (_, _) =>
            {
                if (_updatingText || !ModGenerator.TryParseColor(_hexBox.Text, out var color))
                    return;

                (_hue, _saturation, _value) = ToHsv(color);
                Refresh(false);
            };
            _hexBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                    popup.IsOpen = false;
            };

            var hexRow = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
            hexRow.Children.Add(_preview);
            hexRow.Children.Add(_hexBox);

            var presets = new WrapPanel { Width = PadWidth, Margin = new Thickness(0, 10, 0, 0) };
            foreach (string preset in Presets)
            {
                var color = (Color)ColorConverter.ConvertFromString(preset);
                var chip = new Border { Width = 22, Height = 22, Margin = new Thickness(0, 0, 9, 6), CornerRadius = new CornerRadius(11), Background = new SolidColorBrush(color), BorderThickness = new Thickness(1), Cursor = Cursors.Hand, ToolTip = preset };
                chip.SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");
                chip.MouseLeftButtonUp += (_, _) =>
                {
                    (_hue, _saturation, _value) = ToHsv(color);
                    Refresh(true);
                };
                presets.Children.Add(chip);
            }

            var stack = new StackPanel();
            stack.Children.Add(padGrid);
            stack.Children.Add(hueGrid);
            stack.Children.Add(hexRow);
            stack.Children.Add(presets);

            var card = new Border
            {
                Padding = new Thickness(12),
                Margin = new Thickness(8),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Child = stack,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = 0.35 }
            };
            card.SetResourceReference(BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
            card.SetResourceReference(BorderBrushProperty, "ControlStrokeColorDefaultBrush");

            popup.Child = card;
            return popup;
        }

        private void Drag(FrameworkElement element, Action<Point> apply)
        {
            void Update(MouseEventArgs e)
            {
                apply(e.GetPosition(element));
                Refresh(true);
            }

            element.MouseLeftButtonDown += (_, e) =>
            {
                element.CaptureMouse();
                Update(e);
                e.Handled = true;
            };
            element.MouseMove += (_, e) =>
            {
                if (element.IsMouseCaptured)
                    Update(e);
            };
            element.MouseLeftButtonUp += (_, e) =>
            {
                element.ReleaseMouseCapture();
                Commit();
                e.Handled = true;
            };
        }

        private void Refresh(bool updateText)
        {
            var color = FromHsv(_hue, _saturation, _value);
            string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            _pad.Background = new SolidColorBrush(FromHsv(_hue, 1, 1));
            Canvas.SetLeft(_padThumb, _saturation * PadWidth - 7);
            Canvas.SetTop(_padThumb, (1 - _value) * PadHeight - 7);
            Canvas.SetLeft(_hueThumb, _hue / 360 * PadWidth - 3);
            _preview.Background = new SolidColorBrush(color);

            if (updateText)
            {
                _updatingText = true;
                _hexBox.Text = hex;
                _hexBox.CaretIndex = hex.Length;
                _updatingText = false;
            }

            _pending = hex;

            if (Live)
            {
                _commitTimer.Stop();
                _commitTimer.Start();
            }
        }

        private void Commit()
        {
            _commitTimer.Stop();

            if (_pending is null || String.Equals(_pending, Hex, StringComparison.OrdinalIgnoreCase))
                return;

            Hex = _pending;
            _pending = null;
        }

        private static Color FromHsv(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            double m = v - c;

            var (r, g, b) = (h % 360) switch
            {
                < 60 => (c, x, 0d),
                < 120 => (x, c, 0d),
                < 180 => (0d, c, x),
                < 240 => (0d, x, c),
                < 300 => (x, 0d, c),
                _ => (c, 0d, x)
            };

            return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        private (double, double, double) ToHsv(Color color)
        {
            double r = color.R / 255d, g = color.G / 255d, b = color.B / 255d;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = _hue;

            if (delta > 0)
            {
                if (max == r)
                    h = 60 * ((g - b) / delta % 6);
                else if (max == g)
                    h = 60 * ((b - r) / delta + 2);
                else
                    h = 60 * ((r - g) / delta + 4);

                if (h < 0)
                    h += 360;
            }

            return (h, max == 0 ? 0 : delta / max, max);
        }
    }
}
