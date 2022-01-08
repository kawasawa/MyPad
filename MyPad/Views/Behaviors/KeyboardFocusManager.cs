using System.Windows;
using System.Windows.Input;

namespace MyPad.Views.Behaviors
{
    /// <summary>
    /// 指定されたコントロールにキーボードフォーカスをセットする添付ビヘイビアを表します。
    /// </summary>
    public static class KeyboardFocusManager
    {
        public static readonly DependencyProperty FocusedElementProperty
            = DependencyPropertyExtensions.RegisterAttached(new PropertyMetadata(OnFocusedElementChanged));
        public static FrameworkElement GetFocusedElement(UIElement obj)
            => (FrameworkElement)obj.GetValue(FocusedElementProperty);
        public static void SetFocusedElement(UIElement obj, FrameworkElement value)
            => obj.SetValue(FocusedElementProperty, value);

        private static void OnFocusedElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not FrameworkElement associated)
                return;
            var element = GetFocusedElement(associated);
            if (element == null)
                return;

            associated.Loaded += FocusedElement_Loaded;
        }

        private static void FocusedElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement associated)
                return;
            var element = GetFocusedElement(associated);
            if (element == null)
                return;

            associated.Loaded -= FocusedElement_Loaded;
            Keyboard.Focus(element);
        }
    }
}
