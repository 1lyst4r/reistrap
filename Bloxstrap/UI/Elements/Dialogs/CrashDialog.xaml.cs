using System.Media;
using System.Windows;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class CrashDialog
    {
        private readonly CrashCause _cause;

        private readonly long _placeId;

        public CrashDialog(CrashCause cause, long placeId)
        {
            _cause = cause;
            _placeId = placeId;

            InitializeComponent();

            TitleTextBlock.Text = cause.Title;
            MessageTextBlock.Text = cause.Message;

            FixButton.Content = cause.Fix switch
            {
                CrashFix.SafeLaunch => Strings.Dialog_Crash_Fix_SafeLaunch,
                CrashFix.RemoveGraphicsFlags => Strings.Dialog_Crash_Fix_RemoveFlags,
                CrashFix.EnableMemoryTrimmer => Strings.Dialog_Crash_Fix_MemoryTrimmer,
                CrashFix.Reinstall => Strings.Dialog_Crash_Fix_Reinstall,
                _ => ""
            };

            FixButton.Visibility = cause.Fix == CrashFix.None ? Visibility.Collapsed : Visibility.Visible;

            SystemSounds.Exclamation.Play();
        }

        private void Relaunch(bool safe)
        {
            string args = _placeId != 0 ? $"roblox://experiences/start?placeId={_placeId}" : "-player";

            if (safe)
                args += " -safemode";

            Process.Start(Paths.Process, args);
        }

        private void ShowApplied()
        {
            MessageTextBlock.Text = Strings.Dialog_Crash_FixApplied;
            FixButton.Visibility = Visibility.Collapsed;
        }

        private void FixButton_Click(object sender, RoutedEventArgs e)
        {
            const string LOG_IDENT = "CrashDialog::FixButton_Click";

            App.Logger.WriteLine(LOG_IDENT, $"Applying fix {_cause.Fix}");

            try
            {
                switch (_cause.Fix)
                {
                    case CrashFix.SafeLaunch:
                        Relaunch(true);
                        Close();
                        break;

                    case CrashFix.RemoveGraphicsFlags:
                        CrashDiagnostics.RemoveGraphicsFlags();
                        ShowApplied();
                        break;

                    case CrashFix.EnableMemoryTrimmer:
                        App.Settings.Prop.MemoryTrimmer = true;
                        App.Settings.Save();
                        ShowApplied();
                        break;

                    case CrashFix.Reinstall:
                        App.State.Prop.ForceReinstall = true;
                        App.State.Save();
                        Relaunch(false);
                        Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(ex.Message, MessageBoxImage.Error);
            }
        }

        private void OpenLogsButton_Click(object sender, RoutedEventArgs e) => Utilities.ShellExecute(Path.Combine(Paths.LocalAppData, "Roblox", "logs"));

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
