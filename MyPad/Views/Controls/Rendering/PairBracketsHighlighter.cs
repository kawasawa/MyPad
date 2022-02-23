using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Windows.Media;

namespace MyPad.Views.Controls.Rendering;

/// <summary>
/// 対応する括弧をハイライトするレンダラーを表します。
/// </summary>
public class PairBracketsHighlighter : IBackgroundRenderer
{
    private readonly Pen _highlightBorderPen;
    private readonly Brush _highlightBackgroundBrush;
    private TextView _textView;
    private ISegment _segment;

    KnownLayer IBackgroundRenderer.Layer => KnownLayer.Selection;

    private PairBracketsHighlighter(TextView textView)
    {
        this._textView = textView ?? throw new ArgumentNullException(nameof(textView));
        this._textView.BackgroundRenderers.Add(this);

        this._highlightBorderPen = new(Brushes.Gray, 2);
        this._highlightBorderPen.Freeze();
        this._highlightBackgroundBrush = new SolidColorBrush(Colors.Transparent);
        this._highlightBackgroundBrush.Freeze();
    }

    public static PairBracketsHighlighter Install(TextView textView)
    {
        if (textView == null)
            throw new ArgumentNullException(nameof(textView));
        return new PairBracketsHighlighter(textView);
    }

    public void Uninstall()
    {
        this._segment = null;
        this._textView.BackgroundRenderers.Remove(this);
        this._textView = null;
    }

    public void Highlight(ISegment segment)
    {
        if (this._segment == segment)
            return;

        this._segment = segment;
        this._textView.InvalidateLayer(((IBackgroundRenderer)this).Layer);
    }

    public void ClearHighlight()
    {
        this.Highlight(null);
    }

    void IBackgroundRenderer.Draw(ICSharpCode.AvalonEdit.Rendering.TextView textView, DrawingContext drawingContext)
    {
        if (this._segment == null)
            return;

        var builder = new BackgroundGeometryBuilder { AlignToWholePixels = true };
        builder.AddSegment(textView, new TextSegment() { StartOffset = this._segment.Offset, Length = 1 });
        builder.CloseFigure(); // prevent connecting the two segments
        builder.AddSegment(textView, new TextSegment() { StartOffset = this._segment.EndOffset - 1, Length = 1 });

        var geometry = builder.CreateGeometry();
        if (geometry != null)
            drawingContext.DrawGeometry(this._highlightBackgroundBrush, this._highlightBorderPen, geometry);
    }
}
