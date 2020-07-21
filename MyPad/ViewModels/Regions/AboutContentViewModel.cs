using Plow;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.IO;
using System.Text;
using Unity;

namespace MyPad.ViewModels.Regions
{
    public class AboutContentViewModel : ViewModelBase
    {
        private static readonly Encoding FILE_ENCODING = Encoding.UTF8;
        private string ChangeHistoryPath => Path.Combine(this.ProductInfo.Working, "HISTORY.md");

        [Dependency]
        public IDialogService DialogService { get; set; }
        [Dependency]
        public IProductInfo ProductInfo { get; set; }

        public ReactiveProperty<string> ChangeHistory { get; }

        public ReactiveCommand<EventArgs> LoadedHandler { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public AboutContentViewModel()
        {
            this.ChangeHistory = new ReactiveProperty<string>()
                .AddTo(this.CompositeDisposable);

            this.LoadedHandler = new ReactiveCommand<EventArgs>()
                .WithSubscribe(e => this.ChangeHistory.Value = File.ReadAllText(this.ChangeHistoryPath, FILE_ENCODING))
                .AddTo(this.CompositeDisposable);
        }
    }
}
