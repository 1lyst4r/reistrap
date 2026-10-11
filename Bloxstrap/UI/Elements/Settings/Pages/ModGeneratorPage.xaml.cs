using System.Windows;

using Wpf.Ui.Mvvm.Contracts;

using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class ModGeneratorPage
    {
        public ModGeneratorPage()
        {
            var viewModel = new ModsViewModel();
            DataContext = viewModel;
            InitializeComponent();
            viewModel.QueuePreview();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
                window.Navigate(typeof(ModsPage));
        }
    }
}
