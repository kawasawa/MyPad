using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MyPad.ViewModels.Dialogs
{
    public class NotifyDialogViewModel : MessageBoxViewModelBase
    {
        public ReactiveCommand OKCommand { get; }

        public NotifyDialogViewModel()
        {
            this.OKCommand = new ReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.OK)))
                .AddTo(this.CompositeDisposable);
        }
    }
}