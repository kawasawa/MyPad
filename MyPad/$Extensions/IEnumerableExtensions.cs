using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPad;

/// <summary>
/// <see cref="IEnumerable{T}"/> インターフェースの拡張メソッドを提供します。
/// </summary>
public static class IEnumerableExtensions
{
    /// <summary>
    /// シーケンスの要素を自然順の昇順に並び変えます。
    /// </summary>
    /// <typeparam name="T">要素の型</typeparam>
    /// <param name="self"><see cref="IEnumerable{T}"/> インターフェースを実装するシーケンス</param>
    /// <param name="keySelector">要素から並び替えのキーを抽出するためのメソッド</param>
    /// <returns>並び替えを行った結果</returns>
    public static IOrderedEnumerable<T> NaturallyOrderBy<T>(this IEnumerable<T> self, Func<T, string> keySelector)
        => self.OrderBy(keySelector, NaturalComparer.Default);

    /// <summary>
    /// シーケンスの要素を自然順の降順に並び変えます。
    /// </summary>
    /// <typeparam name="T">要素の型</typeparam>
    /// <param name="self"><see cref="IEnumerable{T}"/> インターフェースを実装するシーケンス</param>
    /// <param name="keySelector">要素から並び替えのキーを抽出するためのメソッド</param>
    /// <returns>並び替えを行った結果</returns>
    public static IOrderedEnumerable<T> NaturallyOrderByDescending<T>(this IEnumerable<T> self, Func<T, string> keySelector)
        => self.OrderByDescending(keySelector, NaturalComparer.Default);
}
