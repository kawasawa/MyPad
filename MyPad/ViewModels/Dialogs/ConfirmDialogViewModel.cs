using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MyPad.ViewModels.Dialogs
{
    public class ConfirmDialogViewModel : MessageBoxViewModelBase
    {
        public ReactiveCommand YesCommand { get; }
        public ReactiveCommand NoCommand { get; }

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