using MyPad.Models;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Text;
using Unity;

namespace MyPad.ViewModels.Dialogs;

/// <summary>
/// <see cref="Views.Dialogs.ChangeEncodingDialog"/> に対応する ViewModel を表します。
/// </summary>
public class ChangeEncodingDialogViewModel : DialogViewModelBase
{
    [Dependency]
    public SyntaxService SyntaxService { get; set; }

    public ReactiveProperty<Encoding> Encoding { get; }
    public ReactiveCommand OKCommand { get; }
    public ReactiveCommand CancelCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public ChangeEncodingDialogViewModel()
    {
        this.Encoding = new ReactiveProperty<Encoding>().AddTo(this.CompositeDisposable);

        this.OKCommand = new ReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(
                new DialogResult(ButtonResult.OK, new DialogParameters {
                    { nameof(this.Encoding), this.Encoding.Value },
                })))
            .AddTo(this.CompositeDisposable);

        this.CancelCommand = new ReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.Cancel)))
            .AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// ダイアログが表示されたときに行う処理を定義します。
    /// </summary>
    /// <param name="parameters">ダイアログのパラメータ</param>
    [LogInterceptor]
    public override void OnDialogOpened(IDialogParameters parameters)
    {
        base.OnDialogOpened(parameters);
        this.Encoding.Value = parameters.GetValue<Encoding>(nameof(this.Encoding));
    }
}
