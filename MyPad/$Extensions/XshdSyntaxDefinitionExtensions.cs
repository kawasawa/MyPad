using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Collections.Generic;
using System.Linq;

namespace MyPad;

/// <summary>
/// <see cref="XshdSyntaxDefinition"/> クラスの拡張メソッドを提供します。
/// </summary>
public static class XshdSyntaxDefinitionExtensions
{
    /// <summary>
    /// 定義ファイルに記載された拡張子を取得します。
    /// </summary>
    /// <param name="self"><see cref="XshdSyntaxDefinition"/> クラスのインスタンス</param>
    /// <returns>拡張子</returns>
    public static IEnumerable<string> GetExtensions(this XshdSyntaxDefinition self)
        // 拡張子がドット始まりになるように整形する
        => self.Extensions?.Select(e => e.StartsWith("*.") ? e[1..] : e.StartsWith('.') == false ? $".{e}" : e) ?? Enumerable.Empty<string>();
}
