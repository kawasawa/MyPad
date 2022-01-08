using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MyPad.Views.Controls.ChangeMarker
{
    /// <summary>
    /// 行単位の変更状態を示すマーカーが描画されるマージンを表します。
    /// </summary>
    public class ChangeMarkerMargin : AbstractMargin, IDisposable
    {
        private const double MARGIN_WIDTH = 8;

        private ChangeTracker _changeTracker;

        public static readonly DependencyProperty AddedLineBrushProperty =
            DependencyPropertyExtensions.Register(new FrameworkPropertyMetadata(Brushes.LightSeaGreen));

        public static readonly DependencyProperty ModifiedLineBrushProperty =
            DependencyPropertyExtensions.Register(new FrameworkPropertyMetadata(Brushes.MediumOrchid));

        public static readonly DependencyProperty UnsavedLineBrushProperty =
            DependencyPropertyExtensions.Register(new FrameworkPropertyMetadata(Brushes.Goldenrod));

        public Brush AddedLineBrush
        {
            get => (Brush)this.GetValue(AddedLineBrushProperty);
            set => this.SetValue(AddedLineBrushProperty, value);
        }

        public Brush ModifiedLineBrush
        {
            get => (Brush)this.GetValue(ModifiedLineBrushProperty);
            set => this.SetValue(ModifiedLineBrushProperty, value);
        }

        public Brush UnsavedLineBrush
        {
            get => (Brush)this.GetValue(UnsavedLineBrushProperty);
            set => this.SetValue(UnsavedLineBrushProperty, value);
        }

        public ChangeMarkerMargin()
        {
            this._changeTracker = new();
            this._changeTracker.ChangeOccurred += this.ChangeWatcher_ChangeOccurred;
        }

        ~ChangeMarkerMargin()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this._changeTracker.ChangeOccurred -= this.ChangeWatcher_ChangeOccurred;
            this._changeTracker.Dispose();
            this._changeTracker = null;
        }

        protected override void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
        {
            if (newDocument != null)
                this._changeTracker.Initialize(newDocument);
            base.OnDocumentChanged(oldDocument, newDocument);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.TextView == null || this.TextView.VisualLinesValid == false)
                return;

            var zeroLineInfo = this._changeTracker.ChangeList.First();
            foreach (var line in this.TextView.VisualLines)
            {
                var rect = new Rect(0, line.VisualTop - this.TextView.ScrollOffset.Y - 1, MARGIN_WIDTH - 3, line.Height + 2);
                var info = this._changeTracker.ChangeList[line.FirstDocumentLine.LineNumber];

                if (zeroLineInfo.ChangeType == ChangeKind.Deleted &&
                    info.ChangeType != ChangeKind.Unsaved &&
                    line.FirstDocumentLine.LineNumber == 1)
                {
                    info.ChangeType = ChangeKind.Modified;
                }

                switch (info.ChangeType)
                {
                    case ChangeKind.None:
                        break;
                    case ChangeKind.Added:
                        drawingContext.DrawRectangle(this.AddedLineBrush, null, rect);
                        break;
                    case ChangeKind.Deleted:
                    case ChangeKind.Modified:
                        drawingContext.DrawRectangle(this.ModifiedLineBrush, null, rect);
                        break;
                    case ChangeKind.Unsaved:
                        drawingContext.DrawRectangle(this.UnsavedLineBrush, null, rect);
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(info.ChangeType), (int)info.ChangeType, info.ChangeType.GetType());
                }
            }
        }

        protected override void OnTextViewChanged(ICSharpCode.AvalonEdit.Rendering.TextView oldTextView, ICSharpCode.AvalonEdit.Rendering.TextView newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= this.TextView_VisualLinesChanged;
                oldTextView.ScrollOffsetChanged -= this.TextView_ScrollOffsetChanged;
            }

            base.OnTextViewChanged(oldTextView, newTextView);

            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += this.TextView_VisualLinesChanged;
                newTextView.ScrollOffsetChanged += this.TextView_ScrollOffsetChanged;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
            => new(MARGIN_WIDTH, 0);

        private void ChangeWatcher_ChangeOccurred(object sender, EventArgs e)
            => this.InvalidateVisual();

        private void TextView_VisualLinesChanged(object sender, EventArgs e)
            => this.InvalidateVisual();

        private void TextView_ScrollOffsetChanged(object sender, EventArgs e)
            => this.InvalidateVisual();
    }
}