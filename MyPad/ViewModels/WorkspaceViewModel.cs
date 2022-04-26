using MyBase;
using MyBase.Logging;
using MyPad.Models;
using MyPad.PubSub;
using Prism.Events;
using Prism.Ioc;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
    [Dependency]
    public IContainerExtension Container { get; set; }

    #endregion

    #region プロパティ

    public ReactiveCommand NewWindowCommand { get; }
    public ReactiveCommand ExitApplicationCommand { get; }

    #endregion

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="eventAggregator">イベントアグリゲーター</param>
    [InjectionConstructor]
    [LogInterceptor]
    public WorkspaceViewModel(IEventAggregator eventAggregator)
    {
        this.EventAggregator = eventAggregator;

        async void exitApplication() => await this.ExitApplication();
        this.EventAggregator.GetEvent<ExitApplicationEvent>().Subscribe(exitApplication);

        this.NewWindowCommand = new ReactiveCommand()
            .WithSubscribe(() => this.Container.Resolve<Views.MainWindow>().Show())
            .AddTo(this.CompositeDisposable);

        this.ExitApplicationCommand = new ReactiveCommand()
            .WithSubscribe(() => exitApplication())
            .AddTo(this.CompositeDisposable);
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
}
