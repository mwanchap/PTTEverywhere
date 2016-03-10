using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsMicrophoneMuteLibrary;

namespace PTTEverywhere
{
    public partial class Form1 : Form
    {
        //TODO: put all the stuff that isn't about the form into a separate class!

        private static WindowsMicMute theMic; //stay off the mic
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private string hotkey = "NumPad0";
        private bool muted = false;
        private Int64 lastPress; //100 nanoseconds (div by 10000 for ms)
        private bool toggleMode = false; //toggles mute/unmute ONLY on double-tap of hotkey

        private const string RegPath = @"Software\Push-To-Talk Everywhere\";
        private RegistryKey SettingsKey;

        public Form1()
        {
            //AllocConsole();
            InitializeComponent();

            LoadSettings();
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            theMic = new WindowsMicMute();
            Console.WriteLine("Muting mic");
            Mute();
            lastPress = DateTime.Now.Ticks;
        }

        /*[DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();*/

        /// <summary>
        /// Loads the settings from the registry
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                SettingsKey = Registry.CurrentUser.OpenSubKey(RegPath, true);
                var regKey = SettingsKey.GetValue("hotkey").ToString();

                if (regKey != null && Enum.GetNames(typeof(Keys)).Contains(regKey))
                {
                    txtHotkey.Text = hotkey = regKey;
                }
            }
            catch (Exception e) //can't load settings - use defaults
            {
                txtHotkey.Text = hotkey = "CapsLock";
            }
        }

        private void SaveSettings()
        {
            SettingsKey.SetValue("hotkey", hotkey, RegistryValueKind.String);
        }

        private void configToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UnMute();
            UnhookWindowsHookEx(_hookID);
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //let the app exit if the user wasn't clicking the close button
            if (e.CloseReason != CloseReason.UserClosing)
            {
                UnMute();
                UnhookWindowsHookEx(_hookID);
                return;
            }

            //otherwise just hide the form and prevent exiting
            e.Cancel = true;
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            hotkey = txtHotkey.Text;
            SaveSettings();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtHotkey.Text = hotkey;
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Mute();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            UnMute();
        }

        //from keyboard hook class

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);
        
        private IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int keyCode = Marshal.ReadInt32(lParam);
                var keyName = Enum.GetName(typeof(Keys), keyCode);
                Console.WriteLine("Key Pressed: " + keyName);
                
                //check if the textbox has focus
                if (this.Visible && txtHotkey.ContainsFocus)
                {
                    txtHotkey.Text = keyName;
                }
                else if(keyName == hotkey && !toggleMode) //check if it's the hotkey
                {
                    Console.WriteLine("Unmuting mic");
                    UnMute();
                }
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                int keyCode = Marshal.ReadInt32(lParam);
                var keyName = Enum.GetName(typeof(Keys), keyCode);
                Console.WriteLine("Key Released: " + keyName);

                if (keyName == hotkey) //check if it's the hotkey
                {
                    if (!toggleMode)
                    {
                        Console.WriteLine("Muting mic");
                        Mute();
                    }

                    if(chkDoubleTap.Checked && (DateTime.Now.Ticks - lastPress) / 10000 < 200) //less than 200ms
                    {
                        toggleMode = !toggleMode;
                        ToggleMute();
                    }

                    lastPress = DateTime.Now.Ticks;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private void button3_Click_1(object sender, EventArgs e)
        {
            UnMute();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show("Hotkeys won't work anymore. Are you sure?",
                "Confirm Release of Keyboard Hook", MessageBoxButtons.OKCancel);

            if (confirm == DialogResult.OK)
            {
                UnhookWindowsHookEx(_hookID);
            }
        }

        private void Mute()
        {
            if (!muted)
            {
                muted = true;
                theMic.MuteMic();
                notifyIcon1.Icon = Properties.Resources.muted;
            }
        }

        private void UnMute()
        {
            if (muted)
            {
                muted = false;
                theMic.UnMuteMic();
                notifyIcon1.Icon = Properties.Resources.mic;
            }
        }

        private void ToggleMute()
        {
            System.Media.SystemSounds.Beep.Play();

            if (muted)
            {
                UnMute();
            }
            else
            {
                Mute();
            }
        }
    }
}
