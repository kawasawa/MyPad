using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Unity;

namespace MyPad.ViewModels.Dialogs
{
    /// <summary>
    /// <see cref="Views.Dialogs.ConfirmDialog"/> に対応する ViewModel を表します。
    /// </summary>
    public class ConfirmDialogViewModel : MessageBoxViewModelBase
    {
        public ReactiveCommand YesCommand { get; }
        public ReactiveCommand NoCommand { get; }

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        [InjectionConstructor]
        [LogInterceptor]
        public ConfirmDialogViewModel()
        {
            this.YesCommand = new ReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.Yes)))
                .AddTo(this.CompositeDisposable);
            this.NoCommand = new ReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.No)))
                .AddTo(this.CompositeDisposable);
        }
    }
}