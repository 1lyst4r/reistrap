using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Bloxstrap.UI.Elements.Controls
{
    public static class Collapsible
    {
        // Properties

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(Collapsible), new PropertyMetadata(false, OnEnabledChanged));

        private static readonly DependencyProperty BodyProperty = DependencyProperty.RegisterAttached(
            "Body", typeof(StackPanel), typeof(Collapsible));

        private static readonly Duration Length = new(TimeSpan.FromMilliseconds(220));

        public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

        public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);

        // Setup

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock title && e.NewValue is true)
                title.Loaded += (_, _) => title.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => Build(title));
        }

        private static void Build(TextBlock title)
        {
            if (title.GetValue(BodyProperty) is not null || title.Parent is not StackPanel parent || parent.Orientation != Orientation.Vertical)
                return;

            var separator = Application.Current.TryFindResource("SectionSeparator") as Style;
            var sectionTitle = Application.Current.TryFindResource("SectionTitle") as Style;
            var description = Application.Current.TryFindResource("SectionDescription") as Style;

            int index = parent.Children.IndexOf(title);
            int start = index + 1;

            if (start < parent.Children.Count && parent.Children[start] is FrameworkElement { Style: var style } && style == description)
                start++;

            var content = new List<UIElement>();

            for (int i = start; i < parent.Children.Count; i++)
            {
                if (parent.Children[i] is FrameworkElement element && (element.Style == separator || element.Style == sectionTitle))
                    break;

                content.Add(parent.Children[i]);
            }

            if (content.Count == 0)
                return;

            var body = new StackPanel();
            title.SetValue(BodyProperty, body);

            foreach (var element in content)
            {
                parent.Children.Remove(element);
                body.Children.Add(element);
            }

            parent.Children.Insert(start, body);

            var chevron = new TextBlock
            {
                Text = "",
                FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                Opacity = 0.7,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform()
            };

            chevron.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

            var header = new DockPanel { Background = Brushes.Transparent, Cursor = Cursors.Hand, Margin = title.Margin };
            parent.Children.Remove(title);
            title.Margin = new Thickness(0);

            DockPanel.SetDock(chevron, Dock.Right);
            header.Children.Add(chevron);
            header.Children.Add(title);
            parent.Children.Insert(index, header);

            string key = $"{parent.FindAncestorPage()}/{title.Text}";
            bool collapsed = App.Settings.Prop.CollapsedSections.Contains(key);

            Set(body, chevron, !collapsed, false);

            header.MouseLeftButtonUp += (_, _) =>
            {
                collapsed = !collapsed;

                if (collapsed)
                    App.Settings.Prop.CollapsedSections.Add(key);
                else
                    App.Settings.Prop.CollapsedSections.Remove(key);

                Set(body, chevron, !collapsed, true);
            };

            body.Tag = (Action)(() =>
            {
                if (!collapsed)
                    return;

                collapsed = false;
                App.Settings.Prop.CollapsedSections.Remove(key);
                Set(body, chevron, true, false);
            });
        }

        private static string FindAncestorPage(this DependencyObject element)
        {
            for (var current = element; current is not null; current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(current))
            {
                if (current is Page page)
                    return page.GetType().Name;
            }

            return "";
        }

        public static void ExpandTo(DependencyObject element)
        {
            for (var current = element; current is not null; current = LogicalTreeHelper.GetParent(current))
            {
                if (current is StackPanel { Tag: Action expand })
                    expand();
            }
        }

        // Animation

        private static void Set(StackPanel body, TextBlock chevron, bool show, bool animate)
        {
            var rotation = (RotateTransform)chevron.RenderTransform;
            double angle = show ? 0 : -90;

            if (!animate || App.Settings.Prop.DisableAnimations)
            {
                rotation.BeginAnimation(RotateTransform.AngleProperty, null);
                rotation.Angle = angle;
                body.BeginAnimation(FrameworkElement.HeightProperty, null);
                body.BeginAnimation(UIElement.OpacityProperty, null);
                body.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            rotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(angle, Length) { EasingFunction = ease });

            Reveal.Animate(body, show);
        }
    }
}
