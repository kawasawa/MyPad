using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Collections.Generic;
using System.Linq;

namespace MyPad
{
    public static class XshdSyntaxDefinitionExtensions
    {
        public static IList<string> GetExtensions(this XshdSyntaxDefinition self)
            => (self.Extensions?.Select(e =>
            {
                // 拡張子がドット始まりになるように整形する
                if (e.StartsWith("*."))
                    e = e[1..];
                if (e.StartsWith('.') == false)
                    e = $".{e}";
                return e;
            }) ?? Enumerable.Empty<string>()).ToList();
    }
}
