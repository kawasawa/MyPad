using System;
using System.Runtime.InteropServices;

namespace MyPad;

/// <summary>
/// Win32 が公開する機能を定義します。
/// </summary>
public static class Win32
{
    /// <summary>
    /// SendMessage によって授受されるデータを表します。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        /// <summary>
        /// 送信するデータの値を表します。
        /// </summary>
        public IntPtr dwData;

        /// <summary>
        /// <see cref="lpData"/> のデータサイズを表します。
        /// </summary>
        public int cbData;

        /// <summary>
        /// 送信するデータのポインタを表します。
        /// </summary>
        public string lpData;
    }
}