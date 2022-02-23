using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace MyPad.Views.Behaviors;

/// <summary>
/// <see cref="Window"/> クラスをアクティブ化させるためのトリガーアクションを表します。
/// </summary>
public class WindowActivateAction : TriggerAction<Window>
{
    /// <summary>
    /// アクションを実行します。
    /// </summary>
    /// <param name="parameter">パラメータ</param>
    protected override void Invoke(object parameter)
    {
        this.AssociatedObject.SetForegroundWindow();
    }
}
