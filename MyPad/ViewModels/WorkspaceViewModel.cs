using Livet.Messaging;
using MyBase;
using MyBase.Logging;
using MyPad.Models;
using MyPad.PubSub;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Unity;

namespace MyPad.ViewModels
{
    public class WorkspaceViewModel : ViewModelBase
    {
        #region インジェクション

        // Constructor Injection
        public IEventAggregator EventAggregator { get; set; }

        // Dependency Injection
        private ILoggerFacade _logger;
        private IProductInfo _productInfo;
        private SettingsService _settingsService;
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
        public SettingsService SettingsService
        {
            get => this._settingsService;
            set => this.SetProperty(ref this._settingsService, value);
        }

        #endregion

        #region プロパティ

        private PerformanceCounter ProcessorTimeCounter { get; }
        private PerformanceCounter WorkingSetPrivateCounter { get; }
        private DispatcherTimer UpdatePerformanceInfoTimer { get; }

        public ReactiveCommand ExitApplicationCommand { get; }

        #endregion

        [InjectionConstructor]
        [LogInterceptor]
        public WorkspaceViewModel(IEventAggregator eventAggregator)
        {
            this.EventAggregator = eventAggregator;

            var processName = Process.GetCurrentProcess().ProcessName;
            this.ProcessorTimeCounter = new PerformanceCounter("Process", "% Processor Time", processName, true).AddTo(this.CompositeDisposable);
            this.WorkingSetPrivateCounter = new PerformanceCounter("Process", "Working Set - Private", processName, true).AddTo(this.CompositeDisposable);

            this.UpdatePerformanceInfoTimer = new();
            this.UpdatePerformanceInfoTimer.Tick += this.UpdatePerformanceInfoTimer_Tick;
            this.UpdatePerformanceInfoTimer.Interval = TimeSpan.FromMilliseconds(AppSettingsReader.PerformanceCheckInterval);
            this.UpdatePerformanceInfoTimer.Start();

            async void exitApplication() => await this.ExitApplication();
            this.EventAggregator.GetEvent<ExitApplicationEvent>().Subscribe(exitApplication);

            this.ExitApplicationCommand = new ReactiveCommand()
                .WithSubscribe(() => exitApplication())
                .AddTo(this.CompositeDisposable);
        }

        [LogInterceptor]
        protected override void Dispose(bool disposing)
        {
            this.UpdatePerformanceInfoTimer.Tick -= this.UpdatePerformanceInfoTimer_Tick;
            this.UpdatePerformanceInfoTimer.Stop();
            base.Dispose(disposing);
        }

        [LogInterceptor]
        private async Task ExitApplication()
        {
            // すべての ViewModel の破棄に成功した場合はアプリケーションを終了する
            var viewModels = this.GetAllViewModels();
            for (var i = viewModels.Count() - 1; 0 <= i; i--)
            {
                var viewModel = viewModels.ElementAt(i);
                viewModel.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));
                if (await viewModel.InvokeExit() == false)
                    return;
            }
            this.Dispose();
        }

        // NOTE: このメソッドは頻発するためトレースしない
        private async void UpdatePerformanceInfoTimer_Tick(object sender, EventArgs e)
        {
            await this.Interrupt(async () =>
            {
                await Task.Run(() => {
                    float? processorTime = null;
                    float? workingSetPrivate = null;
                    try
                    {
                        processorTime = this.ProcessorTimeCounter.NextValue();
                    }
                    catch
                    {
                        this.Logger.Log($"パフォーマンスカウンタの値を取得できませんでした。: Category={this.ProcessorTimeCounter.CategoryName}, Counter={this.ProcessorTimeCounter.CounterName}, Instance={this.ProcessorTimeCounter.InstanceName}, Machine={this.ProcessorTimeCounter.MachineName}", Category.Warn);
                    }
                    try
                    {
                        workingSetPrivate = this.WorkingSetPrivateCounter.NextValue() / 1000 / 1000;
                    }
                    catch
                    {
                        this.Logger.Log($"パフォーマンスカウンタの値を取得できませんでした。: Category={this.WorkingSetPrivateCounter.CategoryName}, Counter={this.WorkingSetPrivateCounter.CounterName}, Instance={this.WorkingSetPrivateCounter.InstanceName}, Machine={this.WorkingSetPrivateCounter.MachineName}", Category.Warn);
                    }
                    this.EventAggregator.GetEvent<UpdatedPerformanceInfoEvent>().Publish((processorTime, workingSetPrivate));
                });
            });
        }

        // NOTE: このメソッドは頻発するためトレースしない
        private async Task Interrupt(Func<Task> func)
        {
            this.UpdatePerformanceInfoTimer.Stop();
            await func.Invoke();
            this.UpdatePerformanceInfoTimer.Interval = TimeSpan.FromMilliseconds(AppSettingsReader.PerformanceCheckInterval);
            this.UpdatePerformanceInfoTimer.Start();
        }
    }
}