using System.Windows;
using System.Windows.Input;

namespace MyPad.Views.Behaviors
{
    /// <summary>
    /// <see cref="Window"/> クラスに汎用処理を追加する添付ビヘイビアを表します。
    /// </summary>
    public class WindowAttachedBehavior
    {
        #region CloseByEsc

        public static readonly DependencyProperty CloseByEscProperty
            = DependencyPropertyExtensions.RegisterAttached(new PropertyMetadata(OnCloseByEscChanged));
        public static bool GetCloseByEsc(DependencyObject obj)
            => (bool)obj.GetValue(CloseByEscProperty);
        public static void SetCloseByEsc(DependencyObject obj, bool value)
            => obj.SetValue(CloseByEscProperty, value);

        private static void OnCloseByEscChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not Window window)
                return;

            if ((bool)e.OldValue)
                window.PreviewKeyDown -= PreviewKeyDown4CloseByEsc;
            if ((bool)e.NewValue)
                window.PreviewKeyDown += PreviewKeyDown4CloseByEsc;
        }

        private static void PreviewKeyDown4CloseByEsc(object sender, KeyEventArgs e)
        {
            if (sender is not Window window || window.IsMouseCaptureWithin)
                return;
            if (e.Key != Key.Escape || Keyboard.Modifiers != ModifierKeys.None)
                return;

            window.Close();
            e.Handled = true;
        }

        #endregion

        #region DraggableAnywhere

        public static readonly DependencyProperty DraggableAnywhereProperty
            = DependencyPropertyExtensions.RegisterAttached(new PropertyMetadata(OnDraggableAnywhereChanged));
        public static bool GetDraggableAnywhere(DependencyObject obj)
            => (bool)obj.GetValue(DraggableAnywhereProperty);
        public static void SetDraggableAnywhere(DependencyObject obj, bool value)
            => obj.SetValue(DraggableAnywhereProperty, value);

        private static void OnDraggableAnywhereChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not Window window)
                return;

            if ((bool)e.OldValue)
                window.MouseLeftButtonDown -= MouseLeftButtonDown4DraggableAnywhere;
            if ((bool)e.NewValue)
                window.MouseLeftButtonDown += MouseLeftButtonDown4DraggableAnywhere;
        }

        private static void MouseLeftButtonDown4DraggableAnywhere(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Window window)
                return;
            if (e.ButtonState != MouseButtonState.Pressed)
                return;

            window.DragMove();
            e.Handled = true;
        }

        #endregion
    }
}
