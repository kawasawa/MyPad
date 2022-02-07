using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Ude;

namespace MyPad.Models
{
    /// <summary>
    /// 文字列の操作に関わる処理を提供します。
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// <see cref="DetectEncoding"/> にて既定で検証されるバイト配列の先頭からの長さ
        /// </summary>
        public const int DETECT_ENCODING_VERIFIED_LENGTH = 10240;

        /// <summary>
        /// 指定されたバイト配列の文字コードを推定します。
        /// </summary>
        /// <param name="bytes">バイト配列</param>
        /// <param name="verifiedLength">
        /// 検証される先頭からのバイト長
        /// 高速化のために既定では配列の先頭から 10240 バイト数分の要素を取り出して検証を行います。
        /// 配列内の全要素を走査する場合は、この値に 0 を渡してください。
        /// </param>
        /// <returns>文字コード</returns>
        public static Encoding DetectEncoding(byte[] bytes, int verifiedLength = DETECT_ENCODING_VERIFIED_LENGTH)
        {
            const int MIN_LENGTH = 256;

            static Encoding detectByBom(byte[] bytes)
            {
                if (4 <= bytes.Length)
                {
                    // UTF-32 BE
                    if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
                        return new UTF32Encoding(true, true);
                    // UTF-32 LE
                    if (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
                        return new UTF32Encoding(false, true);
                }
                if (3 <= bytes.Length)
                {
                    // UTF-8
                    if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                        return new UTF8Encoding(true);
                }
                if (2 <= bytes.Length)
                {
                    // UTF-16 BE
                    if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                        return new UnicodeEncoding(true, true);
                    // UTF-16 LE
                    if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                        return new UnicodeEncoding(false, true);
                }
                return null;
            }

            // 空の場合は null
            if (bytes.Length == 0)
                return null;

            // BOM による判定
            var bom = detectByBom(bytes);
            if (bom != null)
                return bom;

            // バイト配列の整形
            if (bytes.Length < MIN_LENGTH)
            {
                // バイト長が短すぎる場合は複製して補う
                var count = MIN_LENGTH / bytes.Length + 1;
                var buffer = new byte[count * bytes.Length];
                for (var i = 0; i < count; i++)
                    Array.Copy(bytes, 0, buffer, bytes.Length * i, bytes.Length);
                bytes = buffer;
            }
            else if (MIN_LENGTH <= verifiedLength && verifiedLength < bytes.Length)
            {
                // バイト長が指定されている場合は切り出す
                var buffer = new byte[verifiedLength];
                Array.Copy(bytes, 0, buffer, 0, verifiedLength);
                bytes = buffer;
            }

            // Mozilla Universal Charset Detector による判定
            var ude = new CharsetDetector();
            ude.Feed(bytes, 0, bytes.Length);
            ude.DataEnd();
            if (string.IsNullOrEmpty(ude.Charset) == false)
                return Encoding.GetEncoding(ude.Charset);

            // いずれによっても判定できない場合は null
            return null;
        }

        /// <summary>
        /// 指定された文字列を圧縮された Base64 形式の文字列に変換します。
        /// </summary>
        /// <param name="str">変換する文字列</param>
        /// <returns>圧縮された Base64 形式の文字列</returns>
        public static string ConvertToCompressedBase64(string str)
        {
            using (var memory = new MemoryStream())
            {
                using (var deflate = new DeflateStream(memory, CompressionMode.Compress, true))
                {
                    var bytes = Encoding.UTF8.GetBytes(str);
                    deflate.Write(bytes, 0, bytes.Length);
                }
                return Convert.ToBase64String(memory.ToArray());
            }
        }

        /// <summary>
        /// 圧縮された Base64 形式の文字列を解凍します。
        /// </summary>
        /// <param name="base64">圧縮された Base64 形式の文字列</param>
        /// <returns>解凍された文字列</returns>
        public static string ConvertFromCompressedBase64(string base64)
        {
            using (var memory = new MemoryStream(Convert.FromBase64String(base64)))
            using (var buffer = new MemoryStream())
            {
                using (var deflate = new DeflateStream(memory, CompressionMode.Decompress))
                {
                    while (true)
                    {
                        var value = deflate.ReadByte();
                        if (value == -1)
                            break;
                        buffer.WriteByte((byte)value);
                    }
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }
    }
}
