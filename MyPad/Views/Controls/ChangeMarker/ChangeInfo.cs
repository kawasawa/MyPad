using System;

namespace MyPad.Views.Controls.ChangeMarker;

/// <summary>
/// 変更状態を格納するモデルを表します。
/// </summary>
public struct ChangeInfo : IEquatable<ChangeInfo>
{
    public static readonly ChangeInfo Empty = new(ChangeKind.None, 1, 1);

    public ChangeKind ChangeType { get; set; }
    public int StartLine { get; }
    public int EndLine { get; }

    public ChangeInfo(ChangeKind change, int startLine, int endLine)
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
        => obj is ChangeInfo info && this.Equals(info);

    public bool Equals(ChangeInfo other)
        => this.ChangeType == other.ChangeType &&
            this.StartLine == other.StartLine &&
            this.EndLine == other.EndLine;

    public static bool operator ==(ChangeInfo lhs, ChangeInfo rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(ChangeInfo lhs, ChangeInfo rhs)
        => lhs.Equals(rhs) == false;
}
