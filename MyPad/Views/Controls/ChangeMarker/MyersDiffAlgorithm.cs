using ICSharpCode.AvalonEdit.Document;
using System.Collections.Generic;

namespace MyPad.Views.Controls.ChangeMarker
{
    /// <summary>
    /// このクラスは ICSharpCode.SharpDevelop.Widgets.MyersDiff.MyersDiffAlgorithm を参考に構築された差分検出アルゴリズムです。
    /// 
    /// <para/>
    /// 
    /// Diff algorithm, based on "An O(ND) Difference Algorithm and its Variations", by Eugene Myers.
    /// The basic idea is to put the line numbers of text A as columns("x") and the lines of text B as rows("y").
    /// Now you try to find the shortest "edit path" from the upper left corner to the lower right corner, where you can always go horizontally or vertically, but diagonally from(x, y) to(x+1, y+1) only if line x in text A is identical to line y in text B.
    /// Myers' fundamental concept is the "furthest reaching D-path on diagonal k": a D-path is an edit path starting at the upper left corner and containing exactly D non-diagonal elements ("differences").
    /// The furthest reaching D-path on diagonal k is the one that contains the most (diagonal) elements which ends on diagonal k (where k = y - x).
    ///
    /// Example:
    ///
    /// H E L L O W O R L D
    ///   ____
    /// L     \___
    /// O         \___
    /// W             \________
    ///
    /// Since every D-path has exactly D horizontal or vertical elements, it can only end on the diagonals -D, -D+2, ..., D-2, D.
    /// Since every furthest reaching D-path contains at least one furthest reaching(D-1)-path(except for D= 0), we can construct them recursively.
    /// Since we are really interested in the shortest edit path, we can start looking for a 0-path, then a 1-path, and so on, until we find a path that ends in the lower right corner.
    /// To save space, we do not need to store all paths (which has quadratic space requirements), but generate the D-paths simultaneously from both sides.
    /// When the ends meet, we will have found "the middle" of the path.From the end points of that diagonal part, we can generate the rest recursively.
    /// This only requires linear space.
    /// 
    /// The overall(runtime) complexity is
    /// O(N * D^2 + 2 * N/2 * (D/2)^2 + 4 * N/4 * (D/4)^2 + ...) = O(N * D^2 * 5 / 4) = O(N * D^2),
    /// (With each step, we have to find the middle parts of twice as many regions as before, but the regions (as well as the D) are halved.)
    /// So the overall runtime complexity stays the same with linear space, albeit with a larger constant factor.
    /// </summary>
    public class MyersDiffAlgorithm
    {
        private readonly MiddleEdit _middleEdit;

        public List<Edit> Edits { get; }

        public MyersDiffAlgorithm(TextDocument documentA, TextDocument documentB)
        {
            this.Edits = new();

            var hashDick = new Dictionary<string, int>();
            var sequenceA = new Sequence(documentA, ref hashDick);
            var sequenceB = new Sequence(documentB, ref hashDick);
            this._middleEdit = new(sequenceA, sequenceB);
            if (this._middleEdit.EndA <= this._middleEdit.BeginA && this._middleEdit.EndB <= this._middleEdit.BeginB)
                return;

            this.CalculateEdits(this._middleEdit.BeginA, this._middleEdit.EndA, this._middleEdit.BeginB, this._middleEdit.EndB);
        }

        /// <summary>
        /// Entrypoint into the algorithm this class is all about.
        /// This method triggers that the differences between A and B are calculated in form of a list of edits.
        /// </summary>
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

        /// <summary>
        /// A modified region detected between two versions of roughly the same content.
        /// Regions should be specified using 0 based notation, so add 1 to the start and end marks for line numbers in a file.
        /// An edit where <code>beginA == endA &amp;&amp; beginB &gt; endB</code> is an insert edit, that is sequence B inserted the elements in region<code>[beginB, endB)</code> at <code> beginA </code>.
        /// An edit where <code>beginA &gt; endA &amp;&amp; beginB &gt; endB</code> is a replace edit, that is sequence B has replaced the range of elements between<code>[beginA, endA)</code> with those found in <code>[beginB, endB)</code>.
        /// </summary>
        public class Edit
        {
            public int BeginA { get; }
            public int EndA { get; }
            public int BeginB { get; }
            public int EndB { get; }

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
                => Equals(left, right);

            public static bool operator !=(Edit left, Edit right)
                => Equals(left, right) == false;

            public override int GetHashCode()
                => this.BeginA ^ this.EndA;
        }

        /// <summary>
        /// A class to help bisecting the sequences a and b to find minimal edit paths.
        /// As the arrays are reused for space efficiency, you will need one instance per thread.
        /// The entry function is the calculate() method.
        /// </summary>
        public class MiddleEdit
        {
            // For each d, we need to hold the d-paths for the diagonals k = -d, -d + 2, ..., d - 2, d.These are stored in the forward(and backward) array.
            // As we allow subsequences, too, this needs some refinement: the forward paths start on the diagonal forwardK = beginB - beginA, and backward paths start on the diagonal backwardK = endB - endA.
            // So, we need to hold the forward d-paths for the diagonals k = forwardK - d, forwardK - d + 2, ..., forwardK + d and the analogue for the backward d-paths.This means that we can turn(k, d) into the forward array index using this formula: i = (d + k - forwardK) / 2
            // There is a further complication: the edit paths should not leave the specified subsequences, so k is bounded by minK = beginB - endA and maxK = endB - beginA.However, (k - forwardK) _must_ be odd whenever d is odd, and it _must_ be even when d is even.
            // The values in the "forward" and "backward" arrays are positions ("x") in the sequence a, to get the corresponding positions("y") in the sequence b, you have to calculate the appropriate k and then y:
            // k = forwardK - d + i * 2
            // y = k + x
            // (substitute backwardK for forwardK if you want to get the y position for an entry in the "backward" array.

            public Sequence SequenceA { get; }
            public Sequence SequenceB { get; }
            public EditPath Forward { get; }
            public EditPath Backward { get; }
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
                this.EndA = this.Backward.Snake(endK, endA);
                this.EndB = this.EndA + endK;
            }

            /// <summary>
            /// This function calculates the "middle" Edit of the shortest edit path between the given subsequences of a and b.
            /// Once a forward path and a backward path meet, we found the middle part.From the last snake end point on both of them, we construct the Edit.
            /// It is assumed that there is at least one edit in the range.
            /// </summary>
            public Edit Calculate(int beginA, int endA, int beginB, int endB)
            {
                if (beginA == endA || beginB == endB)
                    return new Edit(beginA, endA, beginB, endB);

                // Following the conventions in Myers' paper, "k" is the difference between the index into "b" and the index into "a".
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

            /// <summary>
            /// A class to help bisecting the sequences a and b to find minimal edit paths.
            /// As the arrays are reused for space efficiency, you will need one instance per thread.
            /// The entry function is the calculate() method.
            /// </summary>
            public abstract class EditPath
            {
                private readonly List<int> _xList = new();
                private readonly List<long> _snakeList = new();

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
                    this._snakeList.Add(CreateNewSnake(k, x));
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
                            leftSnake = left == x ? this._snakeList[i] : CreateNewSnake(k - 1, x);
                            if (this.Meets(d, k - 1, x, leftSnake))
                                return true;
                            left = this.GetLeft(x);
                        }
                        if (k < prevEndK)
                        {
                            var i = this.GetIndex(d - 1, k + 1);
                            right = this._xList[i];
                            var x = this.Snake(k + 1, right);
                            rightSnake = right == x ? this._snakeList[i] : CreateNewSnake(k + 1, x);
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

                private static long CreateNewSnake(int k, int x)
                    => ((long)x << 32) | (long)(k + x);

                /// <summary>
                /// Check for incompatible partial edit paths: when there are ambiguities, we might have hit incompatible (i.e. non-overlapping) forward/backward paths.
                /// In that case, just pretend that we have an empty edit at the end of one snake; this will force a decision which path to take in the next recursion step.
                /// </summary>
                protected void MakeEdit(long snake1, long snake2)
                {
                    static int snake2x(long snake) => (int)((ulong)snake >> 32);
                    static int snake2y(long snake) => (int)(((ulong)snake << 32) >> 32);

                    var x1 = snake2x(snake1);
                    var x2 = snake2x(snake2);
                    var y1 = snake2y(snake1);
                    var y2 = snake2y(snake2);

                    this.MiddleEdit.Edit = (x2 < x1 || y2 < y1) ? new(x2, x2, y2, y2) : new(x1, x2, y1, y2);
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
}
