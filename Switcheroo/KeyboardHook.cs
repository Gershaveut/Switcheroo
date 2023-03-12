using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Switcheroo
{
    internal class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private IntPtr hookId = IntPtr.Zero;
        private LowLevelKeyboardProc hookProc;

        public event EventHandler<KeyEventArgs> KeyDown;

        public KeyboardHook()
        {
            hookProc = new LowLevelKeyboardProc(KeyboardHookProc);
        }

        public void Start()
        {
            hookId = SetHook(hookProc);
        }

        public void Stop()
        {
            UnhookWindowsHookEx(hookId);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule module = process.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
            }
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Keys key = (Keys)Marshal.ReadInt32(lParam);
                KeyEventArgs e = new KeyEventArgs(key);
                OnKeyDown(e);
                if (e.Handled)
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        protected virtual void OnKeyDown(KeyEventArgs e)
        {
            KeyDown?.Invoke(this, e);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
