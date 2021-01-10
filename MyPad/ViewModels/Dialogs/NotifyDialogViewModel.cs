using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Unity;

namespace MyPad.ViewModels.Dialogs
{
    public class NotifyDialogViewModel : MessageBoxViewModelBase
    {
        public ReactiveCommand OKCommand { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public NotifyDialogViewModel()
        {
            this.OKCommand = new ReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.OK)))
                .AddTo(this.CompositeDisposable);
        }
    }
}