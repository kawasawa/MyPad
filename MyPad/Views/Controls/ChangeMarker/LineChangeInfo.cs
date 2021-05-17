using System;

namespace MyPad.Views.Controls.ChangeMarker
{
    public struct LineChangeInfo : IEquatable<LineChangeInfo>
    {
        public static readonly LineChangeInfo Empty = new(ChangeKind.None, 1, 1);

        public ChangeKind ChangeType { get; set; }
        public int StartLine { get; }
        public int EndLine { get; }

        public LineChangeInfo(ChangeKind change, int startLine, int endLine)
        {
            this.ChangeType = change;
            this.StartLine = startLine;
            this.EndLine = endLine;
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            unchecked
            {
                hashCode += 1000000007 * this.ChangeType.GetHashCode();
                hashCode += 1000000009 * this.StartLine.GetHashCode();
                hashCode += 1000000021 * this.EndLine.GetHashCode();
            }
            return hashCode;
        }

        public override bool Equals(object obj)
            => obj is LineChangeInfo info && this.Equals(info);

        public bool Equals(LineChangeInfo other)
            => this.ChangeType == other.ChangeType &&
                this.StartLine == other.StartLine &&
                this.EndLine == other.EndLine;

        public static bool operator ==(LineChangeInfo lhs, LineChangeInfo rhs)
            => lhs.Equals(rhs);

        public static bool operator !=(LineChangeInfo lhs, LineChangeInfo rhs)
            => lhs.Equals(rhs) == false;
    }
}