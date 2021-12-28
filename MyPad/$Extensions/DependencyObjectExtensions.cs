using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MyPad
{
    /// <summary>
    /// <see cref="DependencyObject"/> クラスの拡張メソッドを提供します。
    /// </summary>
    public static class DependencyObjectExtensions
    {
        /// <summary>
        /// ビジュアルツリー上の親要素を取得します。
        /// </summary>
        /// <param name="self"><see cref="DependencyObject"/> クラスのインスタンス</param>
        /// <returns>親要素のインスタンス</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static DependencyObject Parent(this DependencyObject self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return VisualTreeHelper.GetParent(self);
        }

        /// <summary>
        /// ビジュアルツリーを遡り、すべての親要素を取得します。
        /// </summary>
        /// <param name="self"><see cref="DependencyObject"/> クラスのインスタンス</param>
        /// <returns>すべての親要素のインスタンス</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<DependencyObject> Ancestor(this DependencyObject self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            var parent = self.Parent();
            if (parent == null)
                yield break;

            yield return parent;
            foreach (var grandParent in parent.Ancestor())
                yield return grandParent;
        }

        /// <summary>
        /// ビジュアルツリー上の子要素を取得します。
        /// </summary>
        /// <param name="self"><see cref="DependencyObject"/> クラスのインスタンス</param>
        /// <returns>子要素のインスタンス</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<DependencyObject> Children(this DependencyObject self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            var count = VisualTreeHelper.GetChildrenCount(self);
            if (count == 0)
                yield break;

            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(self, i);
                if (child != null)
                    yield return child;
            }
        }

        /// <summary>
        /// ビジュアルツリーを遡り、すべての子要素を取得します。
        /// </summary>
        /// <param name="self"><see cref="DependencyObject"/> クラスのインスタンス</param>
        /// <returns>すべての子要素のインスタンス</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<DependencyObject> Descendants(this DependencyObject self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            foreach (var child in self.Children())
            {
                yield return child;
                foreach (var grandChild in child.Descendants())
                    yield return grandChild;
            }
        }
    }
}
