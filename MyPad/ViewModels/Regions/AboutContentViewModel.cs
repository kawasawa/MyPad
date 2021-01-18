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
        private string DisclaimerPath => Path.Combine(this.ProductInfo.Working, "DISCLAIMER.md");
        private string HistoryPath => Path.Combine(this.ProductInfo.Working, "HISTORY.md");
        private string OssLicensePath => Path.Combine(this.ProductInfo.Working, "OSS_LICENSE.md");
        private string PrivacyPolicyPath => Path.Combine(this.ProductInfo.Working, "PRIVACY_POLICY.md");

        [Dependency]
        public IDialogService DialogService { get; set; }
        [Dependency]
        public IProductInfo ProductInfo { get; set; }

        public ReactiveProperty<string> Disclaimer { get; }
        public ReactiveProperty<string> History { get; }
        public ReactiveProperty<string> OssLicense { get; }
        public ReactiveProperty<string> PrivacyPolicy { get; }

        public ReactiveCommand<EventArgs> LoadedHandler { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public AboutContentViewModel()
        {
            this.Disclaimer = new ReactiveProperty<string>()
                .AddTo(this.CompositeDisposable);
            this.History = new ReactiveProperty<string>()
                .AddTo(this.CompositeDisposable);
            this.OssLicense = new ReactiveProperty<string>()
                .AddTo(this.CompositeDisposable);
            this.PrivacyPolicy = new ReactiveProperty<string>()
                .AddTo(this.CompositeDisposable);

            this.LoadedHandler = new ReactiveCommand<EventArgs>()
                .WithSubscribe(e =>
                {
                    this.Disclaimer.Value = File.ReadAllText(this.DisclaimerPath, FILE_ENCODING);
                    this.History.Value = File.ReadAllText(this.HistoryPath, FILE_ENCODING);
                    this.OssLicense.Value = File.ReadAllText(this.OssLicensePath, FILE_ENCODING);
                    this.PrivacyPolicy.Value = File.ReadAllText(this.PrivacyPolicyPath, FILE_ENCODING);
                })
                .AddTo(this.CompositeDisposable);
        }
    }
}
