using MyPad.Models;
using MyPad.ViewModels.Events;
using Plow;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Unity;

namespace MyPad.ViewModels
{
    public class WorkspaceViewModel : ViewModelBase
    {
        #region インジェクション

        // Constructor Injection
        public IEventAggregator EventAggregator { get; set; }

        // Dependency Injection

        private IProductInfo _productInfo;
        private SettingsService _settingsService;
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

        public ReactiveCommand ExitApplicationCommand { get; }

        #endregion

        [InjectionConstructor]
        [LogInterceptor]
        public WorkspaceViewModel(IEventAggregator eventAggregator)
        {
            this.EventAggregator = eventAggregator;

            async void exitApplication() => await this.ExitApplication();
            this.EventAggregator.GetEvent<ExitApplicationEvent>().Subscribe(exitApplication);

            this.ExitApplicationCommand = new ReactiveCommand()
                .WithSubscribe(() => exitApplication())
                .AddTo(this.CompositeDisposable);
        }

        [LogInterceptor]
        private async Task ExitApplication()
        {
            // すべての ViewModel の破棄に成功した場合はアプリケーションを終了する
            var views = this.GetViews();
            for (var i = views.Count() - 1; 0 <= i; i--)
            {
                var view = views.ElementAt(i);
                view.SetForegroundWindow();
                if (await ((MainWindowViewModel)view.DataContext).InvokeExit() == false)
                    return;
            }
            this.Dispose();
        }
    }
}