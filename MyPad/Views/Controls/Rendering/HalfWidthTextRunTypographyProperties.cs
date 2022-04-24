using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;

namespace MyPad.Views.Controls.Rendering;

public class HalfWidthTextRunTypographyProperties : DefaultTextRunTypographyProperties
{
    public static readonly HalfWidthTextRunTypographyProperties Instance = new();

    public override FontEastAsianWidths EastAsianWidths => FontEastAsianWidths.Half;
}
