using MyBase;
using MyBase.Logging;
using MyPad.Models;
using MyPad.Properties;
using MyPad.PubSub;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Unity;

namespace MyPad.ViewModels;

/// <summary>
/// アプリケーションのメインプロセスを制御する ViewModel を表します。
/// </summary>
public class WorkspaceViewModel : ViewModelBase
{
    #region インジェクション

    // Constructor Injection
    public IEventAggregator EventAggregator { get; set; }
    public SharedDataStore SharedDataStore { get; set; }

    // Dependency Injection
    private ILoggerFacade _logger;
    private IProductInfo _productInfo;
    private Settings _settings;
    [Dependency]
    public ILoggerFacade Logger
    {
        get => this._logger;
        set => this.SetProperty(ref this._logger, value);
    }
    [Dependency]
    public IProductInfo ProductInfo
    {
        get => this._productInfo;
        set => this.SetProperty(ref this._productInfo, value);
    }
    [Dependency]
    public Settings Settings
    {
        get => this._settings;
        set => this.SetProperty(ref this._settings, value);
    }

    #endregion

    #region プロパティ

    private DispatcherTimer PerformanceCheckTimer { get; }
    private PerformanceCounter ProcessorTimeCounter { get; set; }
    private PerformanceCounter WorkingSetPrivateCounter { get; set; }

    public ReactiveCommand NewWindowCommand { get; }
    public ReactiveCommand ExitApplicationCommand { get; }

    #endregion

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="eventAggregator">イベントアグリゲーター</param>
    [InjectionConstructor]
    [LogInterceptor]
    public WorkspaceViewModel(IEventAggregator eventAggregator, SharedDataStore sharedDataStore)
    {
        this.EventAggregator = eventAggregator;
        this.SharedDataStore = sharedDataStore;

        this.PerformanceCheckTimer = new();
        this.PerformanceCheckTimer.Tick += this.PerformanceCheckTimer_Tick;
        this.PerformanceCheckTimer.Interval = TimeSpan.FromMilliseconds(AppSettingsReader.PerformanceCheckInterval);
        this.PerformanceCheckTimer.Start();

        async void exitApplication() => await this.ExitApplication();
        this.EventAggregator.GetEvent<ExitApplicationEvent>().Subscribe(exitApplication);

        void switchPomodoroTimer()
        {
            if (this.SharedDataStore.IsInPomodoro.Value)
                this.StopPomodoro();
            else
                this.TransitionPomodoroSet();
        }
        this.EventAggregator.GetEvent<SwitchPomodoroTimerEvent>().Subscribe(switchPomodoroTimer);

        this.NewWindowCommand = new ReactiveCommand()
            .WithSubscribe(() => this.EventAggregator.GetEvent<CreateWindowEvent>().Publish())
            .AddTo(this.CompositeDisposable);

        this.ExitApplicationCommand = new ReactiveCommand()
            .WithSubscribe(() => exitApplication())
            .AddTo(this.CompositeDisposable);

        Observable.Timer(TimeSpan.FromSeconds(1))
            .Where(_ => this.SharedDataStore.IsInPomodoro.Value)
            .Repeat()
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ =>
            {
                var timer = this.SharedDataStore.PomodoroTimer.Value - TimeSpan.FromSeconds(1);
                if (SharedDataStore.DownedPomodoroTimerValue < timer)
                    this.SharedDataStore.PomodoroTimer.Value = timer;
                else
                    this.TransitionPomodoroSet();
            }).AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// このインスタンスが保持するリソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージリソースを破棄するかどうかを示す値</param>
    [LogInterceptor]
    protected override void Dispose(bool disposing)
    {
        this.PerformanceCheckTimer.Tick -= this.PerformanceCheckTimer_Tick;
        this.PerformanceCheckTimer.Stop();
        base.Dispose(disposing);
    }

    /// <summary>
    /// アプリケーションの終了を試行します。
    /// 内包するすべての <see cref="MainWindowViewModel"/> が終了要求に応じた場合、アプリケーションは終了します。
    /// </summary>
    /// <returns>非同期タスク</returns>
    [LogInterceptor]
    private async Task ExitApplication()
    {
        // すべての ViewModel の破棄に成功した場合はアプリケーションを終了する
        var viewModels = MvvmHelper.GetMainWindowViewModels();
        for (var i = viewModels.Count() - 1; 0 <= i; i--)
        {
            if (await viewModels.ElementAt(i).InvokeClose() == false)
                return;
        }
        this.Dispose();
    }

    /// <summary>
    /// パフォーマンス計測タイマーに割り込んで処理を実行させます。
    /// </summary>
    /// <param name="func">割り込み処理</param>
    /// <returns>非同期タスク</returns>
    [LogInterceptorIgnore]
    private async Task Interrupt(Func<Task> func)
    {
        this.PerformanceCheckTimer.Stop();
        await func.Invoke();
        this.PerformanceCheckTimer.Interval = TimeSpan.FromMilliseconds(AppSettingsReader.PerformanceCheckInterval);
        this.PerformanceCheckTimer.Start();
    }

    /// <summary>
    /// パフォーマンス再計測タイマーのインターバルが経過したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptorIgnore]
    private async void PerformanceCheckTimer_Tick(object sender, EventArgs e)
    {
        await this.Interrupt(async () =>
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
                this.EventAggregator.GetEvent<PerformanceCheckedEvent>().Publish((processorTime, workingSetPrivate));
            });
        });
    }

    /// <summary>
    /// ポモドーロの作業セットと休憩セットを切り替えます。
    /// </summary>
    [LogInterceptor]
    private void TransitionPomodoroSet()
    {
        if (this.SharedDataStore.IsPomodoroWorking.Value)
        {
            this.SharedDataStore.IsPomodoroWorking.Value = false;
            this.SharedDataStore.PomodoroTimer.Value = TimeSpan.FromMinutes(this.Settings.OtherTools.PomodoroBreakInterval);
            this.EventAggregator.GetEvent<RaiseBalloonEvent>().Publish((Resources.Label_PomodoroTimer, Resources.Message_NotifyPomodoroBreakTime));
        }
        else
        {
            this.SharedDataStore.IsPomodoroWorking.Value = true;
            this.SharedDataStore.PomodoroTimer.Value = TimeSpan.FromMinutes(this.Settings.OtherTools.PomodoroWorkInterval);
            this.EventAggregator.GetEvent<RaiseBalloonEvent>().Publish((Resources.Label_PomodoroTimer, Resources.Message_NotifyPomodoroWorkTime));
        }
    }

    /// <summary>
    /// ポモドーロを終了します。
    /// </summary>
    [LogInterceptor]
    private void StopPomodoro()
    {
        this.SharedDataStore.PomodoroTimer.Value = SharedDataStore.DownedPomodoroTimerValue;
    }
}
