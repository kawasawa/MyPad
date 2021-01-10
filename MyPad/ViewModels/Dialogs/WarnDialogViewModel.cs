using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Unity;

namespace MyPad.ViewModels.Dialogs
{
    public class WarnDialogViewModel : MessageBoxViewModelBase
    {
        public ReactiveCommand OKCommand { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public WarnDialogViewModel()
        {
            this.OKCommand = new ReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.OK)))
                .AddTo(this.CompositeDisposable);
        }
    }
}