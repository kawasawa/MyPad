using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Windows.Media;

namespace MyPad.Views.Controls
{
    public class BracketHighlighter : IBackgroundRenderer
    {
        private readonly TextView _textView;
        private readonly Pen _higlightBorderPen;
        private readonly Brush _highlightBackgroundBrush;
        private ISegment _segment;

        KnownLayer IBackgroundRenderer.Layer => KnownLayer.Selection;

        public BracketHighlighter(TextView textView)
        {
            this._textView = textView ?? throw new ArgumentNullException(nameof(textView));
            this._textView.BackgroundRenderers.Add(this);

            this._higlightBorderPen = new(Brushes.Gray, 2);
            this._higlightBorderPen.Freeze();
            this._highlightBackgroundBrush = new SolidColorBrush(Colors.Transparent);
            this._highlightBackgroundBrush.Freeze();
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
                drawingContext.DrawGeometry(this._highlightBackgroundBrush, this._higlightBorderPen, geometry);
        }
    }
}