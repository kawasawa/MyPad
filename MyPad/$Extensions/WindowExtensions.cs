using System.Windows;
using System.Windows.Interop;
using Vanara.PInvoke;

namespace MyPad;

/// <summary>
/// <see cref="Window"/> クラスの拡張メソッドを提供します。
/// </summary>
public static class WindowExtensions
{
    /// <summary>
    /// <see cref="HwndSource"/> を取得します。
    /// </summary>
    /// <param name="self"><see cref="Window"/> クラスのインスタンス</param>
    /// <returns><see cref="HwndSource"/> クラスのインスタンス</returns>
    public static HwndSource GetHwndSource(this Window self)
    {
        var interop = new WindowInteropHelper(self);
        return HwndSource.FromHwnd(interop.EnsureHandle());
    }

    /// <summary>
    /// ウィンドウを最前面に表示します。
    /// </summary>
    /// <param name="self"><see cref="Window"/> クラスのインスタンス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    public static bool SetForegroundWindow(this Window self)
    {
        var source = self.GetHwndSource();
        if (source == null)
            return false;

        var hWnd = source.Handle;
        if (User32.IsIconic(hWnd))
            User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
        return User32.SetForegroundWindow(hWnd);
    }
}
