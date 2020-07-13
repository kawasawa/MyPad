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
        public IntPtr dwData;
        public int cbData;
        public string lpData;
    }
}
