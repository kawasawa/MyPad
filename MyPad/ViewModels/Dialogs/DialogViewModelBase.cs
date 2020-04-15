using Prism.Services.Dialogs;
using System;

namespace MyPad.ViewModels.Dialogs
{
    public abstract class DialogViewModelBase : ViewModelBase, IDialogAware
    {
        protected IDialogParameters Parameters { get; set; }
        public string Title { get; protected set; }

        public event Action<IDialogResult> RequestClose;

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            this.Parameters = parameters;
            this.Title = parameters.GetValue<string>(nameof(this.Title)) ?? string.Empty;
        }

        public virtual bool CanCloseDialog()
        {
            return true;
        }

        public virtual void OnDialogClosed()
        {
        }

        protected virtual void OnRequestClose(IDialogResult dialogResult)
        {
            this.RequestClose?.Invoke(dialogResult);
        }
    }
}
