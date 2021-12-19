using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Collections.Generic;
using System.Linq;

namespace MyPad
{
    public static class XshdSyntaxDefinitionExtensions
    {
        public static IEnumerable<string> GetExtensions(this XshdSyntaxDefinition self)
            // 拡張子がドット始まりになるように整形する
            => self.Extensions?.Select(e => e.StartsWith("*.") ? e[1..] : e.StartsWith('.') == false ? $".{e}" : e) ?? Enumerable.Empty<string>();
    }
}
