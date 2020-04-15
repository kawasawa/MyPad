using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace MyPad
{
    public static class Constants
    {
        public static readonly IEnumerable<object> CULTURES
            = new[] {
                new { Description = "English", Name = "en-US" },
                new { Description = "日本語", Name = "ja-JP" },
            };

        private static IEnumerable<Encoding> _ENCODINGS;
        public static IEnumerable<Encoding> ENCODINGS
            => _ENCODINGS ??= Encoding.GetEncodings()
                .Select(info => info.Name)
                .Concat(new[] { "shift-jis", "euc-jp", "iso-2022-jp" })
                .Select(name => Encoding.GetEncoding(name))
                .OrderBy(e => e.CodePage)
                .ToList();

        public static readonly IEnumerable<FontFamily> FONT_FAMILIES
            = Fonts.SystemFontFamilies;

        public static readonly IEnumerable<double> FONT_SIZES
            = new[] { 6, 7, 8, 9, 10, 10.5, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 32, 36, 42, 48, 60, 72, 96 };
    }
}
