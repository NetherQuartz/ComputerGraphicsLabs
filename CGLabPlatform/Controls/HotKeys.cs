using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace CGLabPlatform
{
    [Flags]
    public enum HotkeyType : byte {
        /// <summary>
        /// Сочетание клавиш на уровне операционной системы <para/>
        /// события обрабатываются всегда, пока работает приложение
        /// </summary>
        System  = 0x00,
        /// <summary>
        /// Сочетание клавиш на уровне приложения <para/>
        /// События обрабатывается если приложение активно и в фокусе
        /// </summary>
        Window  = 0x01,
        /// <summary>
        /// Сочетание клавиш на уровне элемента управления<para/>
        /// события обрабатывается если фокус установлен на конкретном<para/>
        /// элементе управления или на одном из его дочерних элементов.
        /// </summary>
        Control = 0x02
    }

    [Flags]
    public enum KeyMod : byte {
        None    = 0x00,
        Alt     = 0x01,
        Shift   = 0x02,
        Control = 0x04,
        Windows = 0x10
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    public class Hotkey : IMessageFilter
    {
        public static void Register(HotkeyType type, Control control, KeyMod mod, Keys key, HandledEventHandler handler)
        {
            Hotkey hk   = new Hotkey();
            hk.WindowControl = control;
            hk.type     = type;
            hk.keyCode  = key;
            hk.alt      = mod.HasFlag(KeyMod.Alt);
            hk.shift    = mod.HasFlag(KeyMod.Shift);
            hk.control  = mod.HasFlag(KeyMod.Control);
            hk.windows  = mod.HasFlag(KeyMod.Windows);
            hk.Pressed  += handler;
            if (type == HotkeyType.System && !hk.Register())
                throw new Exception("Ошибочка вышла - не удалось зарегестрировать клавишу");
        }

        #region Interop

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, Keys vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessageA(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessageW(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr SendMessageA(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessageW(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_KEYDOWN    = 0x0100;
        private const uint WM_KEYUP      = 0x0101;
        private const uint WM_SYSKEYDOWN = 0x0104;
        private const uint WM_SYSKEYUP   = 0x0105;
        private const uint WM_HOTKEY     = 0x0312;
        private const int  VK_SHIFT      = 0x10;
        private const int  VK_CONTROL    = 0x11;
        private const int  VK_MENU       = 0x5B;
        private const int  VK_LWIN       = 0x5B;
        private const int  VK_RWIN       = 0x5C;
        private const uint MOD_ALT       = 0x1;
        private const uint MOD_CONTROL   = 0x2;
        private const uint MOD_SHIFT     = 0x4;
        private const uint MOD_WIN       = 0x8;
        private const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

        #endregion

        private static int currentID;
        private const  int maximumID = 0xBFFF;

        private HotkeyType type;
        private Keys keyCode;
        private bool shift;
        private bool control;
        private bool alt;
        private bool windows;

        [XmlIgnore]
        private int  id;
        [XmlIgnore]
        private bool enabled;
        [XmlIgnore]
        private bool registered;
        [XmlIgnore]
        private Control windowControl;

        public event HandledEventHandler Pressed;

        public Hotkey() : this(HotkeyType.System, null, Keys.None, false, false, false, false) { }

        public Hotkey(HotkeyType type, Control owner, Keys keyCode, bool shift, bool control, bool alt, bool windows)
        {
            this.enabled = true;
            this.type    = type;
            this.keyCode = keyCode;
            this.shift   = shift;
            this.control = control;
            this.alt     = alt;
            this.windows = windows;
            this.WindowControl = owner;

            Application.AddMessageFilter(this);
        }

        ~Hotkey() 
        {
            if (registered)
                Unregister();
        }

        public Hotkey Clone() 
        {
            return new Hotkey(type, WindowControl, keyCode, shift, control, alt, windows);
        }

        public bool GetCanRegister() 
        {
            try {
                if (!Register())
                    return false;

                Unregister();
                return true;
            } catch (Win32Exception) {
                return false;
            } catch (NotSupportedException) {
                return false;
            }
        }

        public bool Register() 
        {
            if (registered)
                throw new NotSupportedException("Невозможно зарегистрировать - горячая клавина уже использованна");

            if (Empty)
                throw new NotSupportedException("Невозможно зарегистрировать - горячая клавиша не заданна");

            id = Hotkey.currentID;
            Hotkey.currentID = Hotkey.currentID + 1 % Hotkey.maximumID;

            uint modifiers = (Alt ? Hotkey.MOD_ALT : 0) | (Control ? Hotkey.MOD_CONTROL : 0) |
                             (Shift ? Hotkey.MOD_SHIFT : 0) | (Windows ? Hotkey.MOD_WIN : 0);

            if (Hotkey.RegisterHotKey(windowControl.Handle, id, modifiers, keyCode) == 0) {
                if (Marshal.GetLastWin32Error() == ERROR_HOTKEY_ALREADY_REGISTERED) {
                    return false;
                } else
                    throw new Win32Exception();
            }

            registered = true;
            return true;
        }

        public void Unregister() 
        {
            if (!registered)
                throw new NotSupportedException("Невозможно удалить не зарегистрированную горячую клавишу");

            if (!windowControl.IsDisposed) {
                if (Hotkey.UnregisterHotKey(windowControl.Handle, id) == 0)
                    throw new Win32Exception();
            }

            registered = false;
        }

        private void Reregister() 
        {
            if (!registered)
                return;

            Unregister();
            Register();
        }

        public bool PreFilterMessage(ref Message message)
        {
            if (!enabled)
                return false;

            switch (type) {

                case HotkeyType.System:
                    return (message.Msg == Hotkey.WM_HOTKEY) &&
                        registered && (message.WParam.ToInt32() == id)
                        && OnPressed();

                case HotkeyType.Window:
                    return ((message.Msg == Hotkey.WM_KEYDOWN) || (message.Msg == Hotkey.WM_SYSKEYDOWN))
                        && (message.WParam.ToInt32() == (int)keyCode) && (alt == (GetKeyState(VK_MENU)>>15!=0))
                        && (control == (GetKeyState(VK_CONTROL)>>15!=0)) && (shift == (GetKeyState(VK_SHIFT)>>15!=0))
                        && (windows == (GetKeyState(VK_LWIN)>>15!=0 || GetKeyState(VK_RWIN)>>15!=0))
                        && OnPressed();

                case HotkeyType.Control:
                    if (((message.Msg != Hotkey.WM_KEYDOWN) && (message.Msg != Hotkey.WM_SYSKEYDOWN))
                        || (message.WParam.ToInt32() != (int) keyCode) || (alt != (GetKeyState(VK_MENU)>>15!=0))
                        || (control != (GetKeyState(VK_CONTROL)>>15!=0)) || (shift != (GetKeyState(VK_SHIFT)>>15!=0))
                        || (windows != (GetKeyState(VK_LWIN)>>15!=0 || GetKeyState(VK_RWIN)>>15!=0)))
                        return false;
 
                    if (windowControl == null || !windowControl.ContainsFocus)
                        return false;
                    var wnd = System.Windows.Forms.Control.FromHandle(message.HWnd);
                    while (wnd != null) {
                        if (wnd == windowControl)
                            return OnPressed();
                        wnd = wnd.Parent;
                    }
                    return false;
            }
            return false;
        }

        private bool OnPressed() 
        {
            HandledEventArgs handledEventArgs = new HandledEventArgs(false);
            if (Pressed != null)
                Pressed(this, handledEventArgs);

            return handledEventArgs.Handled;
        }

        public override string ToString()
        {
            if (Empty)
                return "(none)";

            string keyName = Enum.GetName(typeof(Keys), keyCode); ;
            switch (keyCode) {
                case Keys.D0:
                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.D7:
                case Keys.D8:
                case Keys.D9: keyName = keyName.Substring(1);
                              break;
            }

            string modifiers = "";
            if (shift)
                modifiers += "Shift+";
            if (control)
                modifiers += "Control+";
            if (alt)
                modifiers += "Alt+";
            if (windows)
                modifiers += "Windows+";

            return modifiers + keyName;
        }

        #region Свойства
        
        public bool Empty {
            get { return keyCode == Keys.None; }
        }

        public bool Registered {
            get { return registered; }
        }

        public Keys KeyCode {
            get { return keyCode; }
            set { keyCode = value;
                  Reregister();
            }
        }

        public bool Shift {
            get { return shift; }
            set { shift = value;
                  Reregister();
            }
        }

        public bool Control {
            get { return control; }
            set { control = value;
                  Reregister();
            }
        }

        public bool Alt {
            get { return alt; }
            set { alt = value;
                  Reregister();
            }
        }

        public bool Windows {
            get { return windows; }
            set { windows = value;
                  Reregister();
            }
        }

        public Control WindowControl {
            get { return windowControl; }
            set { windowControl = value;
                  Reregister();
            }
        }

        public HotkeyType Type {
            get { return type; }
            set { type = value;
                  Reregister();
            }
        }

        public bool Enabled {
            get { return enabled; }
            set { enabled = value; }
        }

        #endregion
    }
}
