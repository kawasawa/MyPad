using MahApps.Metro.Controls;
using Prism.Services.Dialogs;
using System.Windows;

namespace MyPad.Views;

/// <summary>
/// Prism によって表示されるダイアログウィンドウの基底クラスを置き換えます。
/// </summary>
/// <remarks>
/// 継承元にカスタムウィンドウを指定することで、ダイアログにデザインテンプレートを適用します。
/// </remarks>
public class PrismDialogWindow : MetroWindow, IDialogWindow
{
    IDialogResult IDialogWindow.Result { get; set; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [LogInterceptor]
    public PrismDialogWindow()
    {
        this.Loaded += this.Window_Loaded;
    }

    /// <summary>
    /// ウィンドウがロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= this.Window_Loaded;

        if (this.DataContext is IDialogAware dialogAware)
            this.Title = dialogAware.Title;

        // オーナーウィンドウのフォントを継承する
        if (this.Owner != null)
            this.FontFamily = this.Owner.FontFamily;
    }
}