using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Shell;

using Bloxstrap.UI.Elements.Bootstrapper.Base;
using Bloxstrap.UI.ViewModels.Bootstrapper;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    public partial class TwentyFiveDialog : IBootstrapperDialog
    {
        private readonly TwentyFiveDialogViewModel _viewModel;

        public Bloxstrap.Bootstrapper? Bootstrapper { get; set; }

        private bool _isClosing;

                public string Message
        {
            get => _viewModel.Message;
            set
            {
                string message = value;
                if (message.EndsWith("..."))
                    message = message[..^3];

                _viewModel.Message = message;
                _viewModel.OnPropertyChanged(nameof(_viewModel.Message));
            }
        }

        public ProgressBarStyle ProgressStyle
        {
            get => _viewModel.ProgressIndeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            set
            {
                _viewModel.ProgressIndeterminate = (value == ProgressBarStyle.Marquee);
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressIndeterminate));
            }
        }

        public int ProgressMaximum
        {
            get => _viewModel.ProgressMaximum;
            set
            {
                _viewModel.ProgressMaximum = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressMaximum));
            }
        }

        public int ProgressValue
        {
            get => _viewModel.ProgressValue;
            set
            {
                _viewModel.ProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressValue));
            }
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _viewModel.TaskbarProgressState;
            set
            {
                _viewModel.TaskbarProgressState = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressState));
            }
        }

        public double TaskbarProgressValue
        {
            get => _viewModel.TaskbarProgressValue;
            set
            {
                _viewModel.TaskbarProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressValue));
            }
        }

        public bool CancelEnabled
        {
            get => _viewModel.CancelEnabled;
            set
            {
                _viewModel.CancelEnabled = value;

                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelEnabled));
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelButtonVisibility));
                            }
        }

        public TwentyFiveDialog()
        {
            _viewModel = new TwentyFiveDialogViewModel(this);
            DataContext = _viewModel;
            Title = App.Settings.Prop.BootstrapperTitle;
            Icon = App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();

            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
                Bootstrapper?.Cancel();
        }

        public void ShowBootstrapper() => ShowDialog();

        public void CloseBootstrapper()
        {
            _isClosing = true;
            Dispatcher.BeginInvoke(Close);
        }

        public void ShowSuccess(string message, Action? callback) => BaseFunctions.ShowSuccess(message, callback);
    }
}
