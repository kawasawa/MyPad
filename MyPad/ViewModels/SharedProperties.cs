using LiveCharts;
using LiveCharts.Defaults;
using MyBase;
using MyBase.Logging;
using MyPad.Models;
using MyPad.Properties;
using MyPad.PubSub;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
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
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public Settings Settings { get; set; }

    private PerformanceCounter ProcessorTimeCounter { get; set; }
    private PerformanceCounter WorkingSetPrivateCounter { get; set; }
    public ChartValues<ObservableValue> CpuUsage { get; }
    public ChartValues<ObservableValue> MemoryUsage { get; }

    public ReactiveCollection<string> TraceLogs { get; }
    public ReactiveCollection<string> DebugLogs { get; }
    public ReactiveCollection<string> InfoLogs { get; }
    public ReactiveCollection<string> WarnLogs { get; }

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

        this.CpuUsage = new();
        this.MemoryUsage = new();

        Observable.Timer(TimeSpan.FromSeconds(AppSettingsReader.PerformanceCheckInterval))
            .Repeat()
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(async _ =>
            {
                await Task.Run(() =>
                {
                    float? processorTime = null;
                    float? workingSetPrivate = null;
                    try
                    {
                        // INFO: PerformanceCounter の初期化には時間がかかる
                        // コンストラクタや同期処理で実行しないように
                        this.ProcessorTimeCounter ??= new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName, true).AddTo(this.CompositeDisposable);
                        processorTime = this.ProcessorTimeCounter.NextValue();
                    }
                    catch
                    {
                        this.Logger.Log($"パフォーマンスカウンタの値を取得できませんでした。: Category={this.ProcessorTimeCounter.CategoryName}, Counter={this.ProcessorTimeCounter.CounterName}, Instance={this.ProcessorTimeCounter.InstanceName}, Machine={this.ProcessorTimeCounter.MachineName}", Category.Warn);
                    }
                    try
                    {
                        this.WorkingSetPrivateCounter ??= new PerformanceCounter("Process", "Working Set - Private", Process.GetCurrentProcess().ProcessName, true).AddTo(this.CompositeDisposable);
                        workingSetPrivate = this.WorkingSetPrivateCounter.NextValue() / 1000 / 1000;
                    }
                    catch
                    {
                        this.Logger.Log($"パフォーマンスカウンタの値を取得できませんでした。: Category={this.WorkingSetPrivateCounter.CategoryName}, Counter={this.WorkingSetPrivateCounter.CounterName}, Instance={this.WorkingSetPrivateCounter.InstanceName}, Machine={this.WorkingSetPrivateCounter.MachineName}", Category.Warn);
                    }

                    if (processorTime.HasValue)
                        this.CpuUsage.Add(new ObservableValue(processorTime.Value));
                    if (AppSettingsReader.PerformanceCountLimit < this.CpuUsage.Count)
                        this.CpuUsage.RemoveAt(0);

                    if (workingSetPrivate.HasValue)
                        this.MemoryUsage.Add(new ObservableValue(workingSetPrivate.Value));
                    if (AppSettingsReader.PerformanceCountLimit < this.MemoryUsage.Count)
                        this.MemoryUsage.RemoveAt(0);
                });
            }).AddTo(this.CompositeDisposable);

        this.TraceLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
        BindingOperations.EnableCollectionSynchronization(this.TraceLogs, new object());
        this.DebugLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
        BindingOperations.EnableCollectionSynchronization(this.DebugLogs, new object());
        this.InfoLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
        BindingOperations.EnableCollectionSynchronization(this.InfoLogs, new object());
        this.WarnLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
        BindingOperations.EnableCollectionSynchronization(this.WarnLogs, new object());

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
    /// 最新のログを収集します。
    /// </summary>
    /// <returns>非同期タスク</returns>
    [LogInterceptor]
    public async Task CollectLogs()
    {
        static IEnumerable<string> getLogs(NLog.ILogger coreLogger, int startAt)
            => coreLogger.Factory.Configuration.ConfiguredNamedTargets.OfType<NLog.Targets.MemoryTarget>().FirstOrDefault()?.Logs.Skip(startAt) ?? Enumerable.Empty<string>();

        await Task.Run(() =>
        {
            var nlogger = ((CompositeLogger)this.Logger).OfType<NLogger>().First();
            var logs = getLogs(nlogger.TraceCoreLogger.Value, this.TraceLogs.Count);
            this.TraceLogs.AddRangeOnScheduler(logs);
            logs = getLogs(nlogger.DebugCoreLogger.Value, this.DebugLogs.Count);
            this.DebugLogs.AddRangeOnScheduler(logs);
            logs = getLogs(nlogger.InfoCoreLogger.Value, this.InfoLogs.Count);
            this.InfoLogs.AddRangeOnScheduler(logs);
            logs = getLogs(nlogger.WarnCoreLogger.Value, this.WarnLogs.Count);
            this.WarnLogs.AddRangeOnScheduler(logs);
        });
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
            this.PomodoroTimer.Value = TimeSpan.FromMinutes(this.Settings.OtherTools.PomodoroBreakDuration);
            this.EventAggregator.GetEvent<RaiseBalloonEvent>().Publish((Resources.Command_PomodoroTimer, Resources.Message_NotifyPomodoroBreakTime));
        }
        else
        {
            this.IsPomodoroWorking.Value = true;
            this.PomodoroTimer.Value = TimeSpan.FromMinutes(this.Settings.OtherTools.PomodoroDuration);
            this.EventAggregator.GetEvent<RaiseBalloonEvent>().Publish((Resources.Command_PomodoroTimer, Resources.Message_NotifyPomodoroWorkTime));
        }
    }

    /// <summary>
    /// ポモドーロを終了します。
    /// </summary>
    [LogInterceptor]
    private void StopPomodoro()
    {
        this.IsPomodoroWorking.Value = false;
        this.PomodoroTimer.Value = DownedPomodoroTimerValue;
    }
}
