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
    public ReactiveProperty<int?> PomodoroDuration { get; }
    [Required]
    public ReactiveProperty<int?> PomodoroBreakDuration { get; }
    public ReactiveProperty<int> PomodoroDurationLimit { get; }
    public ReactiveCommand OKCommand { get; }
    public ReactiveCommand CancelCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public ChangePomodoroTimerDialogViewModel()
    {
        this.PomodoroDuration = new ReactiveProperty<int?>()
            .SetValidateAttribute(() => this.PomodoroDuration)
            .AddTo(this.CompositeDisposable);
        this.PomodoroBreakDuration = new ReactiveProperty<int?>()
            .SetValidateAttribute(() => this.PomodoroBreakDuration)
            .AddTo(this.CompositeDisposable);
        this.PomodoroDurationLimit = new ReactiveProperty<int>()
            .AddTo(this.CompositeDisposable);

        this.OKCommand = new[] {
                this.PomodoroDuration.ObserveHasErrors.Inverse(),
                this.PomodoroBreakDuration.ObserveHasErrors.Inverse(),
            }
            .CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(() => this.OnRequestClose(
                new DialogResult(ButtonResult.OK, new DialogParameters {
                    { nameof(this.PomodoroDuration), this.PomodoroDuration.Value },
                    { nameof(this.PomodoroBreakDuration), this.PomodoroBreakDuration.Value },
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
        this.PomodoroDuration.Value = parameters.GetValue<int>(nameof(this.PomodoroDuration));
        this.PomodoroBreakDuration.Value = parameters.GetValue<int>(nameof(this.PomodoroBreakDuration));
        this.PomodoroDurationLimit.Value = parameters.GetValue<int>(nameof(this.PomodoroDurationLimit));
    }
}
