using MyBase;
using MyPad.Models;
using MyPad.Properties;
using MyPad.PubSub;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Unity;

namespace MyPad.ViewModels;

/// <summary>
/// 複数の要素で共有されるプロパティ群を表します。
/// </summary>
public sealed class SharedProperties : ValidatableBase
{
    private static readonly TimeSpan DownedPomodoroTimerValue = TimeSpan.Zero;

    [Dependency]
    public IEventAggregator EventAggregator { get; set; }
    [Dependency]
    public Settings Settings { get; set; }

    public ReactiveProperty<TimeSpan> PomodoroTimer { get; }
    public ReactiveProperty<bool> IsInPomodoro { get; }
    public ReactiveProperty<bool> IsPomodoroWorking { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public SharedProperties(IEventAggregator eventAggregator)
    {
        this.EventAggregator = eventAggregator;

        this.PomodoroTimer = new ReactiveProperty<TimeSpan>(DownedPomodoroTimerValue).AddTo(this.CompositeDisposable);
        this.IsInPomodoro = this.PomodoroTimer.Select(t => t != DownedPomodoroTimerValue).ToReactiveProperty().AddTo(this.CompositeDisposable);
        this.IsPomodoroWorking = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);

        void switchPomodoroTimer()
        {
            if (this.IsInPomodoro.Value)
                this.StopPomodoro();
            else
                this.TransitionPomodoroSet();
        }
        this.EventAggregator.GetEvent<SwitchPomodoroTimerEvent>().Subscribe(switchPomodoroTimer);

        Observable.Timer(TimeSpan.FromSeconds(1))
            .Where(_ => this.IsInPomodoro.Value)
            .Repeat()
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ =>
            {
                var timer = this.PomodoroTimer.Value - TimeSpan.FromSeconds(1);
                if (DownedPomodoroTimerValue < timer)
                    this.PomodoroTimer.Value = timer;
                else
                    this.TransitionPomodoroSet();
            }).AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// ポモドーロの作業セットと休憩セットを切り替えます。
    /// </summary>
    [LogInterceptor]
    private void TransitionPomodoroSet()
    {
        if (this.IsPomodoroWorking.Value)
        {
            this.IsPomodoroWorking.Value = false;
            this.PomodoroTimer.Value = TimeSpan.FromMinutes(this.Settings.OtherTools.PomodoroBreakInterval);
            this.EventAggregator.GetEvent<RaiseBalloonEvent>().Publish((Resources.Label_PomodoroTimer, Resources.Message_NotifyPomodoroBreakTime));
        }
        else
        {
            this.IsPomodoroWorking.Value = true;
            this.PomodoroTimer.Value = TimeSpan.FromMinutes(this.Settings.OtherTools.PomodoroWorkInterval);
            this.EventAggregator.GetEvent<RaiseBalloonEvent>().Publish((Resources.Label_PomodoroTimer, Resources.Message_NotifyPomodoroWorkTime));
        }
    }

    /// <summary>
    /// ポモドーロを終了します。
    /// </summary>
    [LogInterceptor]
    private void StopPomodoro()
    {
        this.PomodoroTimer.Value = DownedPomodoroTimerValue;
    }
}
