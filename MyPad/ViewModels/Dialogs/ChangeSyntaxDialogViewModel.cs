using MyPad.Models;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Unity;

namespace MyPad.ViewModels.Dialogs;

/// <summary>
/// <see cref="Views.Dialogs.ChangeSyntaxDialog"/> に対応する ViewModel を表します。
/// </summary>
public class ChangeSyntaxDialogViewModel : DialogViewModelBase
{
    [Dependency]
    public SyntaxService SyntaxService { get; set; }

    public ReactiveProperty<string> Syntax { get; }

    public ReactiveCommand OKCommand { get; }
    public ReactiveCommand CancelCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public ChangeSyntaxDialogViewModel()
    {
        this.Syntax = new ReactiveProperty<string>().AddTo(this.CompositeDisposable);

        this.OKCommand = new ReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(
                new DialogResult(ButtonResult.OK, new DialogParameters {
                    { nameof(this.Syntax), this.Syntax.Value },
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
        this.Syntax.Value = parameters.GetValue<string>(nameof(this.Syntax));
    }
}
