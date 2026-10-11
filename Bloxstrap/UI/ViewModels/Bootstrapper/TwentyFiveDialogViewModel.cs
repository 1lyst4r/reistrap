namespace Bloxstrap.UI.ViewModels.Bootstrapper
{
    public class TwentyFiveDialogViewModel : BootstrapperDialogViewModel
    {
        public string CancelText => Strings.Common_Cancel;

        public TwentyFiveDialogViewModel(IBootstrapperDialog dialog) : base(dialog)
        {
        }
    }
}
