using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Unity;

namespace MyPad.ViewModels.Dialogs;

/// <summary>
/// <see cref="Views.Dialogs.NotifyDialog"/> に対応する ViewModel を表します。
/// </summary>
public class NotifyDialogViewModel : MessageBoxViewModelBase
{
    public ReactiveCommand OKCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public NotifyDialogViewModel()
    {
        this.OKCommand = new ReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.OK)))
            .AddTo(this.CompositeDisposable);
    }
}
