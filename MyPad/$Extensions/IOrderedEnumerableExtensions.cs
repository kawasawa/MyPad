using System;
using System.Linq;

namespace MyPad
{
    /// <summary>
    /// <see cref="IOrderedEnumerable{T}"/> インターフェースの拡張メソッドを提供します。
    /// </summary>
    public static class IOrderedEnumerableExtensions
    {
        /// <summary>
        /// 並び替えられたシーケンスに対して、要素を自然順の昇順に並び変えます。
        /// </summary>
        /// <typeparam name="T">要素の型</typeparam>
        /// <param name="self"><see cref="IOrderedEnumerable{T}"/> インターフェースを実装するシーケンス</param>
        /// <param name="keySelector">要素から並び替えのキーを抽出するためのメソッド</param>
        /// <returns>並び替えを行った結果</returns>
        public static IOrderedEnumerable<T> NaturallyThenBy<T>(this IOrderedEnumerable<T> self, Func<T, string> keySelector)
            => self.ThenBy(keySelector, NaturalComparer.Default);

        /// <summary>
        /// 並び替えられたシーケンスに対して、要素を自然順の降順に並び変えます。
        /// </summary>
        /// <typeparam name="T">要素の型</typeparam>
        /// <param name="self"><see cref="IOrderedEnumerable{T}"/> インターフェースを実装するシーケンス</param>
        /// <param name="keySelector">要素から並び替えのキーを抽出するためのメソッド</param>
        /// <returns>並び替えを行った結果</returns>
        public static IOrderedEnumerable<T> NaturallyThenByDescending<T>(this IOrderedEnumerable<T> self, Func<T, string> keySelector)
            => self.ThenByDescending(keySelector, NaturalComparer.Default);
    }
}
