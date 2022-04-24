using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Text.RegularExpressions;

namespace MyPad.Views.Controls.Rendering;

public class OpenTypeVisualLineTransformer : DocumentColorizingTransformer
{
    const string REGEX_PATTERN = @"[ -~]{1}";

    public bool EnabledHalfWidth { get; set; }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (this.EnabledHalfWidth)
        {
            var text = this.CurrentContext.Document.GetText(line);
            foreach (Match m in Regex.Matches(text, REGEX_PATTERN))
                this.ChangeLinePart(
                    line.Offset + m.Index,
                    line.Offset + m.Index + m.Length,
                    e => e.TextRunProperties.SetTypographyProperties(HalfWidthTextRunTypographyProperties.Instance));
        }
    }
}
