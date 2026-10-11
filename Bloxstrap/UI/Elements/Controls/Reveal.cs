using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Bloxstrap.UI.Elements.Controls
{
    public static class Reveal
    {
        // Properties

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(Reveal), new PropertyMetadata(false, OnEnabledChanged));

        private static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
            "Source", typeof(FrameworkElement), typeof(Reveal));

        private static readonly DependencyProperty AttachedProperty = DependencyProperty.RegisterAttached(
            "Attached", typeof(bool), typeof(Reveal));

        private static readonly DependencyProperty ShownProperty = DependencyProperty.RegisterAttached(
            "Shown", typeof(bool?), typeof(Reveal));

        private static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
            "Target", typeof(bool?), typeof(Reveal));

        private static readonly Duration Length = new(TimeSpan.FromMilliseconds(220));

        public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

        public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);

        // Setup

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element || e.NewValue is not true)
                return;

            element.Loaded += (_, _) => Attach(element);
            element.IsEnabledChanged += (_, _) => Update(element, true);
        }

        private static void Attach(FrameworkElement element)
        {
            if (!(bool)element.GetValue(AttachedProperty))
            {
                element.SetValue(AttachedProperty, true);

                string? name = BindingOperations.GetBinding(element, UIElement.IsEnabledProperty)?.ElementName;

                if (name is not null && ((element.Parent as FrameworkElement)?.FindName(name) ?? element.FindName(name)) is FrameworkElement source)
                {
                    element.SetValue(SourceProperty, source);
                    source.IsEnabledChanged += (_, _) => Update(element, true);
                    DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement)).AddValueChanged(source, (_, _) => Update(element, true));
                }
            }

            Update(element, false);
        }

        // Animation

        private static void Update(FrameworkElement element, bool animate)
        {
            var source = element.GetValue(SourceProperty) as FrameworkElement;
            bool show = element.IsEnabled && (source is null || source.IsEnabled && source.Visibility == Visibility.Visible);

            if (element.GetValue(ShownProperty) is bool shown && shown == show)
                return;

            element.SetValue(ShownProperty, show);

            if (!animate || !element.IsLoaded || App.Settings.Prop.DisableAnimations)
            {
                element.BeginAnimation(FrameworkElement.HeightProperty, null);
                element.BeginAnimation(UIElement.OpacityProperty, null);
                element.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            Animate(element, show);
        }

        public static void Animate(FrameworkElement element, bool show)
        {
            double from = element.Visibility == Visibility.Visible ? element.ActualHeight : 0;
            double to = 0;

            element.SetValue(TargetProperty, show);
            element.ClipToBounds = true;

            if (show)
            {
                element.BeginAnimation(FrameworkElement.HeightProperty, null);
                element.Visibility = Visibility.Visible;

                double width = element.Parent is FrameworkElement parent && parent.ActualWidth > 0 ? parent.ActualWidth : element.ActualWidth;
                element.Measure(new Size(width, double.PositiveInfinity));
                to = Math.Max(element.DesiredSize.Height - element.Margin.Top - element.Margin.Bottom, 0);
            }

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var height = new DoubleAnimation(from, to, Length) { EasingFunction = ease };

            height.Completed += (_, _) =>
            {
                if (element.GetValue(TargetProperty) is not bool current || current != show)
                    return;

                element.BeginAnimation(FrameworkElement.HeightProperty, null);
                element.BeginAnimation(UIElement.OpacityProperty, null);
                element.ClipToBounds = false;

                if (!show)
                    element.Visibility = Visibility.Collapsed;
            };

            element.BeginAnimation(FrameworkElement.HeightProperty, height);
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(show ? 0 : element.Opacity, show ? 1 : 0, Length) { EasingFunction = ease });
        }
    }
}
