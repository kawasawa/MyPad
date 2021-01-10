using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Collections.Generic;
using System.Linq;

namespace MyPad.ViewModels
{
    public static class XshdSyntaxDefinitionExtensions
    {
        public static IList<string> GetCommonExtensions(this XshdSyntaxDefinition self)
            => (self.Extensions?.Select(e =>
            {
                if (e.StartsWith("*."))
                    e = e[1..];
                if (e.StartsWith('.') == false)
                    e = $".{e}";
                return e;
            }) ?? Enumerable.Empty<string>()).ToList();
    }
}
