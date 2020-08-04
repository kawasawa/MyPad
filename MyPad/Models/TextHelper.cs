using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ude;

namespace MyPad.Models
{
    public static class TextHelper
    {
        /// <summary>
        /// 指定されたバイト配列の文字コードを推定します。
        /// </summary>
        /// <param name="bytes">バイト配列</param>
        /// <param name="verifiedLength">検証される先頭からのバイト長</param>
        /// <returns>文字コード</returns>
        public static Encoding DetectEncoding(byte[] bytes, int verifiedLength)
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
        /// 指定されたバイト配列の文字コードを推定します。検証は配列の先頭から既定のバイト数分の要素を取り出して行われます。
        /// </summary>
        /// <param name="bytes">バイト配列</param>
        /// <returns>文字コード</returns>
        public static Encoding DetectEncodingSimple(byte[] bytes)
            => DetectEncoding(bytes, 10240);

        /// <summary>
        /// 指定されたバイト配列の文字コードを推定します。検証は配列内の全要素を走査して行われます。
        /// </summary>
        /// <param name="bytes">バイト配列</param>
        /// <returns>文字コード</returns>
        public static Encoding DetectEncodingFull(byte[] bytes)
            => DetectEncoding(bytes, 0);

        /// <summary>
        /// 指定されたディレクトリ以下に存在するファイルに対して文字列の検索を行います。
        /// </summary>
        /// <param name="targetText">検索する文字列</param>
        /// <param name="rootPath">検索対象のディレクトリ</param>
        /// <param name="defaultEncoding">既定の文字コード (<paramref name="autoDetectEncoding"/> が false の場合は常にこの文字コードが使用されます。)</param>
        /// <param name="searchPattern">ファイルの種類</param>
        /// <param name="allDirectories">検索対象にサブディレクトリも含めるかどうかを示す値</param>
        /// <param name="ignoreCase">大文字と小文字を区別するかどうかを示す値</param>
        /// <param name="useRegex">正規表現を使用するかどうかを示す値</param>
        /// <param name="autoDetectEncoding">文字コードを自動判別するかどうかを示す値</param>
        /// <param name="bufferSize">並列実行されるチャンクのサイズ</param>
        /// <returns>検索結果</returns>
        public static async IAsyncEnumerable<(string path, int line, string text, Encoding encoding)> Grep(string targetText, string rootPath, Encoding defaultEncoding, string searchPattern = "*", bool allDirectories = true, bool ignoreCase = true, bool useRegex = false, bool autoDetectEncoding = true, int bufferSize = 30)
        {
            if (defaultEncoding == null)
                throw new ArgumentNullException(nameof(defaultEncoding));

            // 検索パターンを調整する
            if (string.IsNullOrEmpty(searchPattern))
                searchPattern = "*";

            // 比較用のメソッドを選択する
            Func<string, bool> func;
            if (useRegex)
                func = text => Regex.IsMatch(text, targetText);
            else if (ignoreCase)
                func = text => 0 <= text.IndexOf(targetText, StringComparison.OrdinalIgnoreCase);
            else
                func = text => 0 <= text.IndexOf(targetText, StringComparison.Ordinal);

            // テキストを検索する
            foreach (var chunk in EnumerateFilesSafe(rootPath, searchPattern, allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Buffer(bufferSize))
            {
                var chunkResult = new ConcurrentBag<(string, int, string, Encoding)>();
                await Task.WhenAll(chunk.Select(path =>
                    Task.Run(() =>
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var encoding = defaultEncoding;
                            if (autoDetectEncoding)
                            {
                                const int VERIFIED_LENGTH = 10240;
                                var buffer = new byte[VERIFIED_LENGTH];
                                stream.Read(buffer, 0, buffer.Length);
                                stream.Position = 0;
                                encoding = DetectEncoding(buffer, VERIFIED_LENGTH) ?? defaultEncoding;
                            }

                            using (var reader = new StreamReader(stream, encoding))
                            {
                                for (var line = 1; 0 <= reader.Peek(); line++)
                                {
                                    // 制御文字を除外する
                                    var text = new string(reader.ReadLine().Where(c => char.IsControl(c) == false).ToArray());
                                    if (func(text))
                                        chunkResult.Add((path, line, text, encoding));
                                }
                            }
                        }
                    }))
                    .ToList());

                foreach (var result in chunkResult)
                    yield return result;
            }
        }

        /// <summary>
        /// 検索パターンに一致するファイルのパスを列挙します。発生した例外は無視されます。
        /// </summary>
        /// <param name="path">検索対象のディレクトリ</param>
        /// <param name="searchPattern">検索パターン</param>
        /// <param name="searchOption">検索オプション</param>
        /// <returns>ファイルのパス</returns>
        private static IEnumerable<string> EnumerateFilesSafe(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            try
            {
                // NOTE: EnumerateFiles による列挙時の例外
                // Directory.EnumerateFiles では UnauthorizedAccessException 等が発生すると列挙が中断されてしまう。
                // 例外を握りつぶして列挙を継続する。

                var children =
                    searchOption == SearchOption.AllDirectories ?
                    Directory.EnumerateDirectories(path).SelectMany(p => EnumerateFilesSafe(p, searchPattern, searchOption)) :
                    Enumerable.Empty<string>();
                return children.Concat(Directory.EnumerateFiles(path, searchPattern));
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
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
        /// <returns>解答された文字列</returns>
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
