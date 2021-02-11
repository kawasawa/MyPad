using System;
using System.Windows;
using System.Windows.Interop;
using Vanara.PInvoke;

namespace MyPad
{
    public static class WindowExtensions
    {
        public static IntPtr GetHandle(this Window self)
            => ((HwndSource)PresentationSource.FromVisual(self)).Handle;

        public static bool SetForegroundWindow(this Window self)
        {
            var hWnd = self.GetHandle();
            if (User32.IsIconic(hWnd))
                User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
            return User32.SetForegroundWindow(hWnd);
        }
    }
}
