using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MyPad.ViewModels.Dialogs;

/// <summary>
/// メッセージボックスに対応する ViewModel の基底クラスを表します。
/// </summary>
public abstract class MessageBoxViewModelBase : DialogViewModelBase, IDialogAware
{
    /// <summary>
    /// メッセージボックスのメッセージ
    /// </summary>
    public ReactiveProperty<string> Message { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    public MessageBoxViewModelBase()
    {
        this.Message = new ReactiveProperty<string>().AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// ダイアログが表示されたときに行う処理を定義します。
    /// </summary>
    /// <param name="parameters">ダイアログのパラメータ</param>
    public override void OnDialogOpened(IDialogParameters parameters)
    {
        base.OnDialogOpened(parameters);
        this.Message.Value = parameters.GetValue<string>(nameof(this.Message));
    }
}
