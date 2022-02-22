using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;
using Unity;

namespace MyPad.ViewModels.Dialogs;

/// <summary>
/// <see cref="Views.Dialogs.ChangeLineDialog"/> に対応する ViewModel を表します。
/// </summary>
public class ChangeLineDialogViewModel : DialogViewModelBase
{
    [Required]
    public ReactiveProperty<int?> Line { get; }
    public ReactiveProperty<int> MaxLine { get; }
    public ReactiveCommand OKCommand { get; }
    public ReactiveCommand CancelCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public ChangeLineDialogViewModel()
    {
        this.Line = new ReactiveProperty<int?>()
            .SetValidateAttribute(() => this.Line)
            .AddTo(this.CompositeDisposable);
        this.MaxLine = new ReactiveProperty<int>()
            .AddTo(this.CompositeDisposable);

        this.OKCommand = this.Line.ObserveHasErrors.Inverse()
            .ToReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(
                new DialogResult(ButtonResult.OK, new DialogParameters {
                    { nameof(this.Line), this.Line.Value },
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
        this.Line.Value = parameters.GetValue<int>(nameof(this.Line));
        this.MaxLine.Value = parameters.GetValue<int>(nameof(this.MaxLine));
    }
}
