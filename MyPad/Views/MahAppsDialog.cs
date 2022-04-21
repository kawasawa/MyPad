using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace MyPad.Views;

/// <summary>
/// MahApps のカスタムダイアログを拡張します。
/// </summary>
public class MahAppsDialog : CustomDialog
{
    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="parentWindow">オーナーウィンドウ</param>
    /// <param name="setting">ダイアログの設定</param>
    [LogInterceptor]
    public MahAppsDialog(MetroWindow parentWindow, MetroDialogSettings setting) : base(parentWindow, setting)
    {
    }

    /// <summary>
    /// ウィンドウがロードされたときに行う処理を定義します。
    /// </summary>
    [LogInterceptor]
    protected override void OnLoaded()
    {
        base.OnLoaded();

        // 基底ではオーナーウィンドウの高さの 25% として設定される
        // このため、コンテンツが少ない状態でオーナーウィンドウを最大化すると、間延びしたように見えてしまう
        // この設定はスタイルでは上書きできない
        this.MinHeight = 0;
    }
}