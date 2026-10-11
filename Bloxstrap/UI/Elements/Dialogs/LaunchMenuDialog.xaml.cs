using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Bloxstrap.UI.Elements.Controls;
using Bloxstrap.UI.ViewModels.Installer;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class LaunchMenuDialog
    {
        public NextAction CloseAction = NextAction.Terminate;

        protected override bool AllowCustomBackground => true;

        private bool _recentGamesOpen;

        public LaunchMenuDialog()
        {
            var viewModel = new LaunchMenuViewModel();
            viewModel.CloseWindowRequest += (_, closeAction) =>
            {
                CloseAction = closeAction;
                Close();
            };

            DataContext = viewModel;

            InitializeComponent();
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = MoreButton.ContextMenu;
            menu.PlacementTarget = MoreButton;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void RecentGamesButton_Click(object sender, RoutedEventArgs e)
        {
            _recentGamesOpen = !_recentGamesOpen;

            if (App.Settings.Prop.DisableAnimations)
                Reveal.Apply(RecentGamesList, _recentGamesOpen);
            else
                Reveal.Animate(RecentGamesList, _recentGamesOpen);

            var rotate = (RotateTransform)RecentGamesChevron.RenderTransform;
            var angle = new DoubleAnimation(_recentGamesOpen ? 180 : 0, App.Settings.Prop.DisableAnimations ? TimeSpan.Zero : TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            rotate.BeginAnimation(RotateTransform.AngleProperty, angle);
        }
    }
}
