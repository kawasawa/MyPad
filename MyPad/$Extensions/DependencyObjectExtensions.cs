using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MyPad
{
    public static class DependencyObjectExtensions
    {
        public static DependencyObject Parent(this DependencyObject self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return VisualTreeHelper.GetParent(self);
        }

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
