using System.Windows;
using System.Windows.Interop;
using Vanara.PInvoke;

namespace MyPad
{
    public static class WindowExtensions
    {
        public static bool SetForegroundWindow(this Window self)
        {
            var hWnd = ((HwndSource)PresentationSource.FromVisual(self)).Handle;
            if (User32.IsIconic(hWnd))
                User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
            return User32.SetForegroundWindow(hWnd);
        }

        public static bool IsIconic(this Window self)
        {
            var hWnd = ((HwndSource)PresentationSource.FromVisual(self)).Handle;
            return User32.IsIconic(hWnd);
        }
    }
}
