using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyPad.Models
{
    /// <summary>
    /// 入出力に関わる処理を提供します。
    /// </summary>
    public static class IOHelper
    {
        /// <summary>
        /// 検索パターンに一致するファイルのパスを列挙します。発生した例外は無視されます。
        /// </summary>
        /// <param name="path">検索対象のディレクトリ</param>
        /// <param name="searchPattern">検索パターン</param>
        /// <param name="searchOption">検索オプション</param>
        /// <returns>ファイルのパス</returns>
        public static IEnumerable<string> EnumerateFilesSafe(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            // INFO: EnumerateFiles の実行時に例外が発生する場合がある問題への対応
            // Directory.EnumerateFiles では UnauthorizedAccessException 等が発生すると列挙が中断されてしまう。
            // 例外を握りつぶして列挙を継続する。
            try
            {
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
        /// 指定されたディレクトリ以下に存在するファイルに対して文字列の検索を行います。
        /// </summary>
        /// <param name="targetText">検索する文字列</param>
        /// <param name="rootPath">検索対象のディレクトリ</param>
        /// <param name="defaultEncoding">既定の文字コード (<paramref name="autoDetectEncoding"/> が false であるかまたは自動判別に失敗した場合、この文字コードが使用されます。)</param>
        /// <param name="autoDetectEncoding">文字コードを自動判別するかどうかを示す値</param>
        /// <param name="searchPattern">ファイルの種類</param>
        /// <param name="allDirectories">検索対象にサブディレクトリも含めるかどうかを示す値</param>
        /// <param name="ignoreCase">大文字と小文字を区別するかどうかを示す値</param>
        /// <param name="useRegex">正規表現を使用するかどうかを示す値</param>
        /// <param name="bufferSize">並列実行されるチャンクのサイズ</param>
        /// <returns>検索結果 (一致したファイルのパス, 行番号, 文字コード)</returns>
        public static async IAsyncEnumerable<(string path, int line, Encoding encoding)> Grep(string targetText, string rootPath, Encoding defaultEncoding, bool autoDetectEncoding = true, string searchPattern = "*", bool allDirectories = true, bool ignoreCase = true, bool useRegex = false, int bufferSize = 30)
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
            foreach (var chunk in IOHelper.EnumerateFilesSafe(rootPath, searchPattern, allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Buffer(bufferSize))
            {
                var chunkResult = new ConcurrentBag<(string, int, Encoding)>();
                await Task.WhenAll(chunk.Select(path =>
                    Task.Run(() =>
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var encoding = defaultEncoding;
                            if (autoDetectEncoding)
                            {
                                var buffer = new byte[StringHelper.DETECT_ENCODING_VERIFIED_LENGTH];
                                stream.Read(buffer, 0, buffer.Length);
                                stream.Position = 0;
                                encoding = StringHelper.DetectEncoding(buffer) ?? defaultEncoding;
                            }

                            using (var reader = new StreamReader(stream, encoding))
                            {
                                for (var line = 1; 0 <= reader.Peek(); line++)
                                {
                                    // 制御文字を除外する
                                    var text = new string(reader.ReadLine().Where(c => char.IsControl(c) == false).ToArray());
                                    if (func(text))
                                        chunkResult.Add((path, line, encoding));
                                }
                            }
                        }
                    }))
                    .ToList());

                foreach (var result in chunkResult)
                    yield return result;
            }
        }
    }
}
