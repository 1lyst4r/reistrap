using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Bloxstrap.Integrations;

namespace Bloxstrap.UI.Elements.Controls
{
    public class HotkeyBox : TextBox
    {
        public HotkeyBox()
        {
            IsReadOnly = true;
            IsReadOnlyCaretVisible = false;
            MinWidth = 160;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            ToolTip = Strings.Menu_Hotkeys_BoxTip;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var modifiers = Keyboard.Modifiers;

            if (modifiers == ModifierKeys.None && key is Key.Back or Key.Delete or Key.Escape)
            {
                SetText("");
                return;
            }

            if (key == Key.Tab && modifiers == ModifierKeys.None)
            {
                e.Handled = false;
                return;
            }

            if (GlobalHotkeys.IsAllowed(modifiers, key))
                SetText(GlobalHotkeys.Format(modifiers, key));
        }

        private void SetText(string text)
        {
            Text = text;
            GetBindingExpression(TextProperty)?.UpdateSource();
        }
    }
}
