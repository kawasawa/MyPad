using System.Windows;
using System.Windows.Input;

namespace MyPad.Views.Controls
{
    /// <summary>
    /// 数値の入力欄と値を増減させるためのボタンを持ったコントロールを表します。
    /// </summary>
    public class NumericUpDown : MahApps.Metro.Controls.NumericUpDown
    {
        public static readonly DependencyProperty ReverseIncrementProperty = DependencyPropertyExtensions.Register();

        /// <summary>
        /// 値の増減処理を反転させるかどうかを示す値
        /// </summary>
        public bool ReverseIncrement
        {
            get => (bool)this.GetValue(ReverseIncrementProperty);
            set => this.SetValue(ReverseIncrementProperty, value);
        }

        /// <summary>
        /// マウスホイールの移動が処理される前に行う処理を定義します。
        /// </summary>
        /// <param name="e">イベントの情報</param>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            // マウスホイールによる動作を反転させる
            if (this.ReverseIncrement)
                base.OnPreviewMouseWheel(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta * -1) { RoutedEvent = MouseWheelEvent });
            else
                base.OnPreviewMouseWheel(e);
        }

        /// <summary>
        /// キーの入力が処理される前に行う処理を定義します。
        /// </summary>
        /// <param name="e">イベントの情報</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (this.ReverseIncrement)
            {
                // 上下キーの動作を反転させる
                switch (e.Key)
                {
                    case Key.Up:
                        base.OnPreviewKeyDown(new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, Key.Down) { RoutedEvent = KeyDownEvent });
                        break;
                    case Key.Down:
                        base.OnPreviewKeyDown(new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, Key.Up) { RoutedEvent = KeyUpEvent });
                        break;
                    default:
                        base.OnPreviewKeyDown(e);
                        break;
                }
            }
            else
            {
                base.OnPreviewKeyDown(e);
            }
        }
    }
}
