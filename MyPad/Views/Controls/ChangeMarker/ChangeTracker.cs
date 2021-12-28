using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.ComponentModel;

namespace MyPad.Views.Controls.ChangeMarker
{
    /// <summary>
    /// ドキュメントの変更状態を監視するトラッカーを表します。
    /// </summary>
    public class ChangeTracker : ILineTracker, IDisposable
    {
        private TextDocument _baseDocument;
        private TextDocument _document;

        public CompressingTreeList<ChangeInfo> ChangeList { get; }

        public event EventHandler ChangeOccurred;

        public ChangeTracker()
        {
            this.ChangeList = new((x, y) => x.Equals(y));
        }

        ~ChangeTracker()
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
            if (this._document != null)
            {
                if (this._document.LineTrackers.Contains(this) == false)
                    this._document.LineTrackers.Remove(this);
                this._document.UndoStack.PropertyChanged -= this.UndoStack_PropertyChanged;
            }
        }

        public void Initialize(TextDocument document)
        {
            if (this._document != null)
            {
                this._document.LineTrackers.Remove(this);
                this._document.UndoStack.PropertyChanged -= this.UndoStack_PropertyChanged;
            }

            this._document = document;
            this._baseDocument = new(document);

            this.ChangeList.Clear();
            this.ChangeList.InsertRange(0, document.LineCount + 1, ChangeInfo.Empty);

            if (this._document.LineTrackers.Contains(this) == false)
            {
                this._document.LineTrackers.Add(this);
                this._document.UndoStack.PropertyChanged += this.UndoStack_PropertyChanged;
            }
        }

        private void UndoStack_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this._document.FileName):
                    // HACK: UndoStack の SizeLimit 変更を起点にファイルのリロードを検知
                    // 偶然 ViewModel 側で SizeLimit を操作しているため実現するが、内部実装レベルで依存しており条件として非常に脆い。
                    this._baseDocument = new(this._document);
                    break;
                case nameof(this._document.UndoStack.IsOriginalFile) when this._document.UndoStack.IsOriginalFile:
                    break;
                default:
                    return;
            }

            try
            {
                this.ChangeList.Clear();
                this.ChangeList.Add(ChangeInfo.Empty);

                var lastEnd = 0;
                var diff = new MyersDiffAlgorithm(this._baseDocument, this._document);
                foreach (var edit in diff.Edits)
                {
                    var change = new ChangeInfo(edit.Change, edit.BeginA, edit.EndA);
                    this.ChangeList.InsertRange(this.ChangeList.Count, edit.BeginB - lastEnd, ChangeInfo.Empty);
                    if (edit.EndB == edit.BeginB)
                        this.ChangeList[^1] = change;
                    else
                        this.ChangeList.InsertRange(this.ChangeList.Count, edit.EndB - edit.BeginB + (edit.BeginB != 0 ? 0 : 1), change);
                    lastEnd = edit.EndB;
                }
                this.ChangeList.InsertRange(this.ChangeList.Count, this._document.LineCount - lastEnd, ChangeInfo.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"行の変更状態の更新時にエラーが発生しました。: {ex}");
                this.ChangeList.Clear();
                this.ChangeList.InsertRange(0, this._document.LineCount + 1, ChangeInfo.Empty);
            }
            this.OnChangeOccurred(EventArgs.Empty);
        }

        protected virtual void OnChangeOccurred(EventArgs e)
        {
            this.ChangeOccurred?.Invoke(this, e);
        }

        void ILineTracker.BeforeRemoveLine(DocumentLine line)
        {
            this.ChangeList.RemoveAt(line.LineNumber);
        }

        void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
        {
        }

        void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
        {
            var info = new ChangeInfo(ChangeKind.Unsaved, insertionPos.LineNumber, insertionPos.LineNumber);
            this.ChangeList[insertionPos.LineNumber] = info;
            this.ChangeList.Insert(insertionPos.LineNumber + 1, info);
        }

        void ILineTracker.RebuildDocument()
        {
            this.ChangeList.Clear();
            this.ChangeList.InsertRange(0, this._document.LineCount + 1, new ChangeInfo(ChangeKind.Unsaved, 1, this._baseDocument.LineCount));
        }

        void ILineTracker.SetLineLength(DocumentLine line, int newTotalLength)
        {
            var info = this.ChangeList[line.LineNumber];
            info.ChangeType = ChangeKind.Unsaved;
            this.ChangeList[line.LineNumber] = info;
        }
    }
}