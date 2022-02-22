using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Unity;

namespace MyPad.ViewModels.Dialogs;

/// <summary>
/// <see cref="Views.Dialogs.CancelableConfirmDialog"/> に対応する ViewModel を表します。
/// </summary>
public class CancelableConfirmDialogViewModel : ConfirmDialogViewModel
{
    public ReactiveCommand CancelCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public CancelableConfirmDialogViewModel()
    {
        this.CancelCommand = new ReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.Cancel)))
            .AddTo(this.CompositeDisposable);
    }
}
