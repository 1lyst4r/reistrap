using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Bloxstrap.Integrations
{
    public sealed class GlobalHotkeys : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;

        private const uint MOD_NOREPEAT = 0x4000;

        private readonly HwndSource _source;

        private readonly Dictionary<int, Action> _actions = new();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public GlobalHotkeys()
        {
            _source = new HwndSource(new HwndSourceParameters("reistrap hotkeys") { ParentWindow = new IntPtr(-3), Width = 0, Height = 0, WindowStyle = 0 });
            _source.AddHook(WndProc);
        }

        public bool Register(string? gesture, Action action)
        {
            const string LOG_IDENT = "GlobalHotkeys::Register";

            if (!TryParse(gesture, out var modifiers, out var key))
                return false;

            int id = _actions.Count + 1;

            if (!RegisterHotKey(_source.Handle, id, (uint)modifiers | MOD_NOREPEAT, (uint)KeyInterop.VirtualKeyFromKey(key)))
            {
                App.Logger.WriteLine(LOG_IDENT, $"Could not register {gesture} (error {Marshal.GetLastWin32Error()})");
                return false;
            }

            _actions[id] = action;
            return true;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
            {
                handled = true;

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("GlobalHotkeys::WndProc", ex);
                }
            }

            return IntPtr.Zero;
        }

        public static bool IsAllowed(ModifierKeys modifiers, Key key) =>
            key is not (Key.None or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            && (modifiers != ModifierKeys.None || key is >= Key.F1 and <= Key.F24);

        public static string Format(ModifierKeys modifiers, Key key)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");

            if (modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");

            if (modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");

            if (modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(key.ToString());

            return String.Join("+", parts);
        }

        public static bool TryParse(string? gesture, out ModifierKeys modifiers, out Key key)
        {
            modifiers = ModifierKeys.None;
            key = Key.None;

            if (String.IsNullOrWhiteSpace(gesture))
                return false;

            foreach (string part in gesture.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                switch (part.ToLowerInvariant())
                {
                    case "ctrl": modifiers |= ModifierKeys.Control; break;
                    case "alt": modifiers |= ModifierKeys.Alt; break;
                    case "shift": modifiers |= ModifierKeys.Shift; break;
                    case "win": modifiers |= ModifierKeys.Windows; break;
                    default:
                        if (!Enum.TryParse(part, true, out key))
                            return false;
                        break;
                }
            }

            return IsAllowed(modifiers, key);
        }

        public void Dispose()
        {
            foreach (int id in _actions.Keys)
                UnregisterHotKey(_source.Handle, id);

            _source.Dispose();
        }
    }
}
