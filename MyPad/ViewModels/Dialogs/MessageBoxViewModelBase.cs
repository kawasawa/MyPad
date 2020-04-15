using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MyPad.ViewModels.Dialogs
{
    public abstract class MessageBoxViewModelBase : DialogViewModelBase, IDialogAware
    {
        public ReactiveProperty<string> Message { get; }

        public MessageBoxViewModelBase()
        {
            this.Message = new ReactiveProperty<string>().AddTo(this.CompositeDisposable);
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);
            this.Message.Value = parameters.GetValue<string>(nameof(this.Message));
        }
    }
}
