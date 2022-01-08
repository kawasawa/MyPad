using System;
using System.Windows;
using System.Windows.Interop;
using Vanara.PInvoke;

namespace MyPad
{
    /// <summary>
    /// <see cref="Window"/> クラスの拡張メソッドを提供します。
    /// </summary>
    public static class WindowExtensions
    {
        /// <summary>
        /// ウィンドウハンドルを取得します。
        /// </summary>
        /// <param name="self"><see cref="Window"/> クラスのインスタンス</param>
        /// <returns>ウィンドウハンドル</returns>
        public static IntPtr GetHandle(this Window self)
            => ((HwndSource)PresentationSource.FromVisual(self)).Handle;

        /// <summary>
        /// ウィンドウを最前面に表示します。
        /// </summary>
        /// <param name="self"><see cref="Window"/> クラスのインスタンス</param>
        /// <returns>正常に処理されたかどうかを示す値</returns>
        public static bool SetForegroundWindow(this Window self)
        {
            var hWnd = self.GetHandle();
            if (User32.IsIconic(hWnd))
                User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
            return User32.SetForegroundWindow(hWnd);
        }
    }
}
