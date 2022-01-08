using Prism.Services.Dialogs;
using System;

namespace MyPad.ViewModels.Dialogs
{
    /// <summary>
    /// ダイアログに対応する ViewModel の基底クラスを表します。
    /// </summary>
    public abstract class DialogViewModelBase : ViewModelBase, IDialogAware
    {
        protected IDialogParameters Parameters { get; set; }

        /// <summary>
        /// ダイアログのタイトル
        /// </summary>
        public string Title { get; protected set; }

        /// <summary>
        /// ダイアログを閉じるためのアクション
        /// </summary>
        public event Action<IDialogResult> RequestClose;

        /// <summary>
        /// ダイアログを閉じられるかどうかを示す値を取得します。
        /// </summary>
        /// <returns>ダイアログを閉じられるかどうかを示す値</returns>
        public virtual bool CanCloseDialog()
        {
            return true;
        }

        /// <summary>
        /// ダイアログが表示されたときに行う処理を定義します。
        /// </summary>
        /// <param name="parameters">ダイアログのパラメータ</param>
        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            this.Parameters = parameters;
            this.Title = parameters.GetValue<string>(nameof(this.Title)) ?? string.Empty;
        }

        /// <summary>
        /// ダイアログが閉じられた後に行う処理を定義します。
        /// </summary>
        public virtual void OnDialogClosed()
        {
        }

        /// <summary>
        /// <see cref="RequestClose"/> を呼び出します。
        /// </summary>
        /// <param name="dialogResult">ダイアログの結果</param>
        protected virtual void OnRequestClose(IDialogResult dialogResult)
        {
            this.RequestClose?.Invoke(dialogResult);
        }
    }
}
