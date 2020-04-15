using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MyPad.ViewModels.Dialogs
{
    public class CancelableConfirmDialogViewModel : ConfirmDialogViewModel
    {
        public ReactiveCommand CancelCommand { get; }

        public CancelableConfirmDialogViewModel()
        {
            this.CancelCommand = new ReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.Cancel)))
                .AddTo(this.CompositeDisposable);
        }
    }
}