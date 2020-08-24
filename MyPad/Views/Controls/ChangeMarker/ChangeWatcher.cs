using FastDeepCloner;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MyPad.Views.Controls.ChangeMarker
{
    public class ChangeWatcher : ILineTracker, IDisposable
    {
        private bool _disposed;
        private TextDocument _baseDocument;
        private TextDocument _document;

        public CompressingTreeList<LineChangeInfo> ChangeList { get; }

        public event EventHandler ChangeOccurred;

        public ChangeWatcher()
        {
            this.ChangeList = new CompressingTreeList<LineChangeInfo>((x, y) => x.Equals(y));
        }

        public void Dispose()
        {
            if (this._disposed)
                return;

            if (this._document != null)
            {
                if (this._document.LineTrackers.Contains(this) == false)
                    this._document.LineTrackers.Remove(this);
                this._document.UndoStack.PropertyChanged -= this.UndoStack_PropertyChanged;
            }
            this._disposed = true;
        }

        public void Initialize(TextDocument document)
        {
            if (this._document != null)
            {
                this._document.LineTrackers.Remove(this);
                this._document.UndoStack.PropertyChanged -= this.UndoStack_PropertyChanged;
            }

            this._document = document;
            this._baseDocument = document.Clone();

            this.ChangeList.Clear();
            this.ChangeList.InsertRange(0, document.LineCount + 1, LineChangeInfo.Empty);

            if (this._document.LineTrackers.Contains(this) == false)
            {
                this._document.LineTrackers.Add(this);
                this._document.UndoStack.PropertyChanged += this.UndoStack_PropertyChanged;
            }
        }

        private void UndoStack_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(this._document.UndoStack.IsOriginalFile) ||
                this._document.UndoStack.IsOriginalFile == false)
            {
                return;
            }

            try
            {
                this.ChangeList.Clear();
                this.ChangeList.Add(LineChangeInfo.Empty);

                var lastEnd = 0;
                var diff = new MyersDiffAlgorithm(this._baseDocument, this._document);
                foreach (var edit in diff.Edits)
                {
                    var change = new LineChangeInfo(edit.Change, edit.BeginA, edit.EndA);
                    this.ChangeList.InsertRange(this.ChangeList.Count, edit.BeginB - lastEnd, LineChangeInfo.Empty);
                    if (edit.EndB == edit.BeginB)
                        this.ChangeList[^1] = change;
                    else
                        this.ChangeList.InsertRange(this.ChangeList.Count, edit.EndB - edit.BeginB + (edit.BeginB != 0 ? 0 : 1), change);
                    lastEnd = edit.EndB;
                }
                this.ChangeList.InsertRange(this.ChangeList.Count, this._document.LineCount - lastEnd, LineChangeInfo.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"行の変更状態の更新時にエラーが発生しました。: {ex}");
                this.ChangeList.Clear();
                this.ChangeList.InsertRange(0, this._document.LineCount + 1, LineChangeInfo.Empty);
            }
            this.OnChangeOccurred(EventArgs.Empty);
        }

        protected virtual void OnChangeOccurred(EventArgs e)
            => this.ChangeOccurred?.Invoke(this, e);

        #region ILineTracker

        void ILineTracker.BeforeRemoveLine(DocumentLine line)
        {
            this.ChangeList.RemoveAt(line.LineNumber);
        }

        void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
        {
        }

        void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
        {
            var info = new LineChangeInfo(ChangeKind.Unsaved, insertionPos.LineNumber, insertionPos.LineNumber);
            this.ChangeList[insertionPos.LineNumber] = info;
            this.ChangeList.Insert(insertionPos.LineNumber + 1, info);
        }

        void ILineTracker.RebuildDocument()
        {
            this.ChangeList.Clear();
            this.ChangeList.InsertRange(0, this._document.LineCount + 1, new LineChangeInfo(ChangeKind.Unsaved, 1, this._baseDocument.LineCount));
        }

        void ILineTracker.SetLineLength(DocumentLine line, int newTotalLength)
        {
            var info = this.ChangeList[line.LineNumber];
            info.ChangeType = ChangeKind.Unsaved;
            this.ChangeList[line.LineNumber] = info;
        }

        #endregion

        #region MyersDiffAlgorithm

        public class MyersDiffAlgorithm
        {
            private readonly MiddleEdit _middleEdit;

            public List<Edit> Edits { get; }

            public MyersDiffAlgorithm(TextDocument documentA, TextDocument documentB)
            {
                this.Edits = new List<Edit>();

                var hashDick = new Dictionary<string, int>();
                var sequenceA = new Sequence(documentA, ref hashDick);
                var sequenceB = new Sequence(documentB, ref hashDick);
                this._middleEdit = new MiddleEdit(sequenceA, sequenceB);
                if (this._middleEdit.EndA <= this._middleEdit.BeginA && this._middleEdit.EndB <= this._middleEdit.BeginB)
                    return;

                this.CalculateEdits(this._middleEdit.BeginA, this._middleEdit.EndA, this._middleEdit.BeginB, this._middleEdit.EndB);
            }

            private void CalculateEdits(int beginA, int endA, int beginB, int endB)
            {
                var edit = this._middleEdit.Calculate(beginA, endA, beginB, endB);

                if (beginA < edit.BeginA || beginB < edit.BeginB)
                {
                    var k = edit.BeginB - edit.BeginA;
                    var x = this._middleEdit.Backward.Snake(k, edit.BeginA);
                    this.CalculateEdits(beginA, x, beginB, k + x);
                }

                if (edit.Change != ChangeKind.None)
                    this.Edits.Add(edit);

                if (edit.EndA < endA || edit.EndB < endB)
                {
                    var k = edit.EndB - edit.EndA;
                    var x = this._middleEdit.Forward.Snake(k, edit.EndA);
                    this.CalculateEdits(x, endA, k + x, endB);
                }
            }

            public class Sequence
            {
                private readonly int[] _hashes;

                public Sequence(TextDocument document, ref Dictionary<string, int> hashDict)
                {
                    this._hashes = new int[document.LineCount];
                    for (var i = 0; i < document.LineCount; i++)
                    {
                        var text = document.GetText(document.GetLineByNumber(i + 1));
                        if (hashDict.TryGetValue(text, out var hash) == false)
                        {
                            hash = hashDict.Count;
                            hashDict.Add(text, hash);
                        }
                        this._hashes[i] = hash;
                    }
                }

                public bool Equals(int index, Sequence other, int otherIndex)
                {
                    if (other == null)
                        return false;
                    if (index < 0 || this._hashes.Length <= index)
                        return false;
                    if (otherIndex < 0 || other._hashes.Length <= otherIndex)
                        return false;
                    return this._hashes[index] == other._hashes[otherIndex];
                }

                public int Size()
                    => this._hashes.Length;
            }

            public class Edit
            {
                public int BeginA { get; private set; }
                public int EndA { get; private set; }
                public int BeginB { get; private set; }
                public int EndB { get; private set; }

                public ChangeKind Change
                {
                    get
                    {
                        if (this.BeginA == this.EndA && this.BeginB == this.EndB)
                            return ChangeKind.None;
                        if (this.BeginA == this.EndA && this.BeginB < this.EndB)
                            return ChangeKind.Added;
                        if (this.BeginA < this.EndA && this.BeginB == this.EndB)
                            return ChangeKind.Deleted;

                        return ChangeKind.Modified;
                    }
                }

                public Edit(int beginA, int endA, int beginB, int endB)
                {
                    this.BeginA = beginA;
                    this.EndA = endA;
                    this.BeginB = beginB;
                    this.EndB = endB;
                }

                public override bool Equals(object obj)
                    => obj is Edit edit && this.Equals(edit);

                public bool Equals(Edit other)
                    => this.BeginA == other.BeginA &&
                        this.EndA == other.EndA &&
                        this.BeginB == other.BeginB &&
                        this.EndB == other.EndB;

                public static bool operator ==(Edit left, Edit right)
                    => left.Equals(right);

                public static bool operator !=(Edit left, Edit right)
                    => left.Equals(right) == false;

                public override int GetHashCode()
                    => this.BeginA ^ this.EndA;
            }

            public class MiddleEdit
            {
                public Sequence SequenceA { get; }
                public Sequence SequenceB { get; }
                public EditPath Forward { get; private set; }
                public EditPath Backward { get; private set; }
                public int BeginA { get; private set; }
                public int EndA { get; private set; }
                public int BeginB { get; private set; }
                public int EndB { get; private set; }
                public Edit Edit { get; set; }

                public MiddleEdit(Sequence sequenceA, Sequence sequenceB)
                {
                    this.SequenceA = sequenceA;
                    this.SequenceB = sequenceB;
                    this.Forward = new ForwardEditPath(this);
                    this.Backward = new BackwardEditPath(this);

                    var beginA = 0;
                    var endA = sequenceA.Size();
                    var beginB = 0;
                    var endB = sequenceB.Size();
                    var beginK = beginB - beginA;
                    var endK = endB - endA;

                    this.BeginA = this.Forward.Snake(beginK, beginA);
                    this.BeginB = this.BeginA + beginK;
                    this.EndA = this.Backward.Snake(beginK, endA);
                    this.EndB = this.EndA + endK;
                }

                public Edit Calculate(int beginA, int endA, int beginB, int endB)
                {
                    if (beginA == endA || beginB == endB)
                        return new Edit(beginA, endA, beginB, endB);

                    var beginK = beginB - beginA;
                    var endK = endB - endA;
                    var minK = beginB - endA;
                    var maxK = endB - beginA;

                    this.BeginA = beginA;
                    this.EndA = endA;
                    this.BeginB = beginB;
                    this.EndB = endB;
                    this.Forward.Initialize(beginK, beginA, minK, maxK);
                    this.Backward.Initialize(endK, endA, minK, maxK);

                    for (var d = 1; ; d++)
                    {
                        if (this.Forward.Calculate(d) || this.Backward.Calculate(d))
                            return this.Edit;
                    }
                }

                public abstract class EditPath
                {
                    private readonly List<int> _xList = new List<int>();
                    private readonly List<long> _snakeList = new List<long>();

                    protected MiddleEdit MiddleEdit { get; }
                    protected int MinK { get; set; }
                    protected int MaxK { get; set; }

                    public int BeginK { get; private set; }
                    public int EndK { get; private set; }
                    public int MiddleK { get; private set; }

                    public abstract int Snake(int k, int x);
                    protected abstract int GetLeft(int x);
                    protected abstract int GetRight(int x);
                    protected abstract bool IsBetter(int left, int right);
                    protected abstract void AdjustMinMaxK(int k, int x);
                    protected abstract bool Meets(int d, int k, int x, long snake);

                    protected EditPath(MiddleEdit middleEdit)
                    {
                        this.MiddleEdit = middleEdit;
                    }

                    public void Initialize(int k, int x, int minK, int maxK)
                    {
                        this.MinK = minK;
                        this.MaxK = maxK;
                        this.BeginK = k;
                        this.EndK = k;
                        this.MiddleK = k;
                        this._xList.Clear();
                        this._xList.Add(x);
                        this._snakeList.Clear();
                        this._snakeList.Add(this.NewSnake(k, x));
                    }

                    public bool Calculate(int d)
                    {
                        int forceKIntoRange(int k)
                        {
                            if (k < this.MinK)
                                return this.MinK + ((k ^ this.MinK) & 1);
                            if (this.MaxK < k)
                                return this.MaxK - ((k ^ this.MaxK) & 1);
                            return k;
                        }

                        var prevBeginK = this.BeginK;
                        var prevEndK = this.EndK;
                        this.BeginK = forceKIntoRange(this.MiddleK - d);
                        this.EndK = forceKIntoRange(this.MiddleK + d);

                        for (var k = this.EndK; this.BeginK <= k; k -= 2)
                        {
                            int left = -1, right = -1;
                            long leftSnake = -1L, rightSnake = -1L;
                            if (prevBeginK < k)
                            {
                                var i = this.GetIndex(d - 1, k - 1);
                                left = this._xList[i];
                                var x = this.Snake(k - 1, left);
                                leftSnake = left == x ? this._snakeList[i] : this.NewSnake(k - 1, x);
                                if (this.Meets(d, k - 1, x, leftSnake))
                                    return true;
                                left = this.GetLeft(x);
                            }
                            if (k < prevEndK)
                            {
                                var i = this.GetIndex(d - 1, k + 1);
                                right = this._xList[i];
                                var x = this.Snake(k + 1, right);
                                rightSnake = right == x ? this._snakeList[i] : this.NewSnake(k + 1, x);
                                if (this.Meets(d, k + 1, x, rightSnake))
                                    return true;
                                right = this.GetRight(x);
                            }

                            var isLeft = prevEndK <= k || (prevBeginK < k && this.IsBetter(left, right));
                            var newX = isLeft ? left : right;
                            var newSnakeTmp = isLeft ? leftSnake : rightSnake;
                            if (this.Meets(d, k, newX, newSnakeTmp))
                                return true;

                            this.AdjustMinMaxK(k, newX);

                            var index = this.GetIndex(d, k);
                            if (index == this._xList.Count)
                                this._xList.Add(newX);
                            else
                                this._xList[index] = newX;
                            if (index == this._snakeList.Count)
                                this._snakeList.Add(newSnakeTmp);
                            else
                                this._snakeList[index] = newSnakeTmp;
                        }
                        return false;
                    }

                    public int GetX(int d, int k)
                        => this._xList[this.GetIndex(d, k)];

                    public long GetSnake(int d, int k)
                        => this._snakeList[this.GetIndex(d, k)];

                    private int GetIndex(int d, int k)
                        => (d + k - this.MiddleK) / 2;

                    private long NewSnake(int k, int x)
                        => ((long)x << 32) | (long)(k + x);

                    protected void MakeEdit(long snake1, long snake2)
                    {
                        static int snake2x(long snake) => (int)((ulong)snake >> 32);
                        static int snake2y(long snake) => (int)(((ulong)snake << 32) >> 32);

                        var x1 = snake2x(snake1);
                        var x2 = snake2x(snake2);
                        var y1 = snake2y(snake1);
                        var y2 = snake2y(snake2);

                        this.MiddleEdit.Edit = (x2 < x1 || y2 < y1) ? new Edit(x2, x2, y2, y2) : new Edit(x1, x2, y1, y2);
                    }
                }

                private class ForwardEditPath : EditPath
                {
                    public ForwardEditPath(MiddleEdit middleEdit)
                        : base(middleEdit)
                    {
                    }

                    protected override int GetLeft(int x)
                        => x;

                    protected override int GetRight(int x)
                        => x + 1;

                    protected override bool IsBetter(int left, int right)
                        => right < left;

                    public override int Snake(int k, int x)
                    {
                        for (; x < this.MiddleEdit.EndA && k + x < this.MiddleEdit.EndB; x++)
                            if (this.MiddleEdit.SequenceA.Equals(x, this.MiddleEdit.SequenceB, k + x) == false)
                                break;
                        return x;
                    }

                    protected override void AdjustMinMaxK(int k, int x)
                    {
                        if (this.MiddleEdit.EndA <= x || this.MiddleEdit.EndB <= k + x)
                        {
                            if (this.MiddleEdit.Backward.MiddleK < k)
                                this.MaxK = k;
                            else
                                this.MinK = k;
                        }
                    }

                    protected override bool Meets(int d, int k, int x, long snake)
                    {
                        if (k < this.MiddleEdit.Backward.BeginK || this.MiddleEdit.Backward.EndK < k)
                            return false;
                        if ((d - 1 + k - this.MiddleEdit.Backward.MiddleK) % 2 == 1)
                            return false;
                        if (x < this.MiddleEdit.Backward.GetX(d - 1, k))
                            return false;

                        this.MakeEdit(snake, this.MiddleEdit.Backward.GetSnake(d - 1, k));
                        return true;
                    }
                }

                private class BackwardEditPath : EditPath
                {
                    public BackwardEditPath(MiddleEdit middleEdit)
                        : base(middleEdit)
                    {
                    }

                    protected override int GetLeft(int x)
                        => x - 1;

                    protected override int GetRight(int x)
                        => x;

                    protected override bool IsBetter(int left, int right)
                        => left < right;

                    public override int Snake(int k, int x)
                    {
                        for (; this.MiddleEdit.BeginA < x && this.MiddleEdit.BeginB < k + x; x--)
                            if (this.MiddleEdit.SequenceA.Equals(x - 1, this.MiddleEdit.SequenceB, k + x - 1) == false)
                                break;
                        return x;
                    }

                    protected override void AdjustMinMaxK(int k, int x)
                    {
                        if (x <= this.MiddleEdit.BeginA || k + x <= this.MiddleEdit.BeginB)
                        {
                            if (this.MiddleEdit.Forward.MiddleK < k)
                                this.MaxK = k;
                            else
                                this.MinK = k;
                        }
                    }

                    protected override bool Meets(int d, int k, int x, long snake)
                    {
                        if (k < this.MiddleEdit.Forward.BeginK || this.MiddleEdit.Forward.EndK < k)
                            return false;
                        if ((d + k - this.MiddleEdit.Forward.MiddleK) % 2 == 1)
                            return false;
                        if (this.MiddleEdit.Forward.GetX(d, k) < x)
                            return false;

                        this.MakeEdit(this.MiddleEdit.Forward.GetSnake(d, k), snake);
                        return true;
                    }
                }
            }
        }

        #endregion
    }
}