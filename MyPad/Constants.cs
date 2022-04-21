using ControlzEx.Theming;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace MyPad;

/// <summary>
/// アプリケーションで使用する定数を定義します。
/// </summary>
public static class Constants
{
    /// <summary>
    /// 初期化処理を行います。
    /// </summary>
    [ModuleInitializer]
    public static void Init()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ENCODINGS = Encoding.GetEncodings()
                .Select(info => info.Name)
                .Concat(new[] { "shift-jis", "euc-jp", "iso-2022-jp" })
                .Select(name => Encoding.GetEncoding(name))
                .OrderBy(e => e.CodePage)
                .ToList();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"ERROR: 文字コードの初期設定に失敗しました。: Message={e.Message}");
            throw;
        }
    }

    /// <summary>
    /// カルチャー一覧
    /// </summary>
    public static ICollection<object> CULTURES { get; }
        = new[] {
            new { Description = "日本語", Name = "ja-JP" },
            new { Description = "English", Name = "en-US" },
        };

    /// <summary>
    /// 文字コード一覧
    /// </summary>
    public static ICollection<Encoding> ENCODINGS { get; private set; }

    /// <summary>
    /// フォントファミリー一覧
    /// </summary>
    public static ICollection<FontFamily> FONT_FAMILIES { get; }
        = Fonts.SystemFontFamilies;

    /// <summary>
    /// フォントウエイト一覧
    /// </summary>
    public static ICollection<FontWeight> FONT_WEIGHTS { get; }
        = new[] { FontWeights.Thin, FontWeights.Normal, FontWeights.Bold };

    /// <summary>
    /// フォントスタイル一覧
    /// </summary>
    public static ICollection<FontStyle> FONT_STYLES { get; }
        = new[] { FontStyles.Normal, FontStyles.Italic };

    /// <summary>
    /// フォントサイズ一覧
    /// </summary>
    public static ICollection<double> FONT_SIZES { get; }
        = new[] { 6, 7, 8, 9, 10, 10.5, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 32, 36, 42, 48, 60, 72, 96 };


    private static ICollection<Theme> _accentColors;

    /// <summary>
    /// アクセントカラー一覧
    /// </summary>
    public static ICollection<Theme> ACCENT_COLORS
        => _accentColors ??= ThemeManager.Current.Themes.GroupBy(t => t.ColorScheme).OrderBy(g => g.Key).Select(g => g.First()).ToList();
}
