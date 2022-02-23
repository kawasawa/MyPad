using System;
using System.Collections;
using System.Collections.Generic;

namespace MyPad;

/// <summary>
/// 自然順となるように要素を比較するための仕組みを提供します。
/// </summary>
internal class NaturalComparer : IComparer, IComparer<string>
{
    private static NaturalComparer _default = null;
    public static NaturalComparer Default => _default ??= new();

    int IComparer.Compare(object x, object y)
    {
        try
        {
            return ((IComparer<string>)this).Compare((string)x, (string)y);
        }
        catch (InvalidCastException e)
        {
            throw new ArgumentException(e.Message);
        }
    }

    int IComparer<string>.Compare(string x, string y)
    {
        static IEnumerable<string> split(string self, Func<char, char, bool> selector)
        {
            var startIndex = 0;
            for (var i = 0; i < self.Length - 1; i++)
            {
                if (selector(self[i], self[i + 1]) == false)
                    continue;

                yield return self[startIndex..(i + 1)];
                startIndex = i + 1;
            }
            yield return self[startIndex..];
        }

        static bool numberCharBorder(char p, char n)
            => (('0' <= p && p <= '9') && !('0' <= n && n <= '9')) || (!('0' <= p && p <= '9') && ('0' <= n && n <= '9'));

        using (var xe = split(x, numberCharBorder).GetEnumerator())
        using (var ye = split(y, numberCharBorder).GetEnumerator())
        {
            while (true)
            {
                var xHasNext = xe.MoveNext();
                var yHasNext = ye.MoveNext();
                if (xHasNext == false || yHasNext == false)
                    return (xHasNext ? 1 : 0) - (yHasNext ? 1 : 0);

                var result = (ulong.TryParse(xe.Current, out var xi) && ulong.TryParse(ye.Current, out var yi)) ?
                    Comparer<ulong>.Default.Compare(xi, yi) :
                    Comparer<string>.Default.Compare(xe.Current, ye.Current);
                if (result != 0)
                    return result;
            }
        }
    }
}
