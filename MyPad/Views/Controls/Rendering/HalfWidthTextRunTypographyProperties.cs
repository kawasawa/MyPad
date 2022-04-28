using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;

namespace MyPad.Views.Controls.Rendering;

public class HalfWidthTextRunTypographyProperties : DefaultTextRunTypographyProperties
{
    public bool SetSlashedZero { set; protected get; }
    public override bool SlashedZero => this.SetSlashedZero;
    public override FontEastAsianWidths EastAsianWidths => FontEastAsianWidths.Half;
}
