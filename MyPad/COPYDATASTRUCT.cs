using System;
using System.Runtime.InteropServices;

namespace MyPad
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
