using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;
using Unity;

namespace MyPad.ViewModels.Dialogs;

/// <summary>
/// <see cref="Views.Dialogs.ChangePomodoroTimerDialog"/> に対応する ViewModel を表します。
/// </summary>
public class ChangePomodoroTimerDialogViewModel : DialogViewModelBase
{
    [Required]
    public ReactiveProperty<int?> WorkMinutes { get; }
    [Required]
    public ReactiveProperty<int?> BreakMinutes { get; }
    public ReactiveProperty<int> MaxInterval { get; }
    public ReactiveCommand OKCommand { get; }
    public ReactiveCommand CancelCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public ChangePomodoroTimerDialogViewModel()
    {
        this.WorkMinutes = new ReactiveProperty<int?>()
            .SetValidateAttribute(() => this.WorkMinutes)
            .AddTo(this.CompositeDisposable);
        this.BreakMinutes = new ReactiveProperty<int?>()
            .SetValidateAttribute(() => this.BreakMinutes)
            .AddTo(this.CompositeDisposable);
        this.MaxInterval = new ReactiveProperty<int>()
            .AddTo(this.CompositeDisposable);

        this.OKCommand = new[] {
                this.WorkMinutes.ObserveHasErrors.Inverse(),
                this.BreakMinutes.ObserveHasErrors.Inverse(),
            }
            .CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(
                new DialogResult(ButtonResult.OK, new DialogParameters {
                    { nameof(this.WorkMinutes), this.WorkMinutes.Value },
                    { nameof(this.BreakMinutes), this.BreakMinutes.Value },
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
        this.WorkMinutes.Value = parameters.GetValue<int>(nameof(this.WorkMinutes));
        this.BreakMinutes.Value = parameters.GetValue<int>(nameof(this.BreakMinutes));
        this.MaxInterval.Value = parameters.GetValue<int>(nameof(this.MaxInterval));
    }
}
