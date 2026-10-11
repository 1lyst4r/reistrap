using System.Windows;

using Wpf.Ui.Mvvm.Contracts;

using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class ModsPage
    {
        public ModsPage()
        {
            DataContext = new ModsViewModel();
            InitializeComponent();
        }

        private void OpenModGenerator_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
                window.Navigate(typeof(ModGeneratorPage));
        }
    }
}
