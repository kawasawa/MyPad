﻿using MyBase;
using MyBase.Wpf.CommonDialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.IO;
using System.Text;
using Unity;

namespace MyPad.ViewModels.Regions;

/// <summary>
/// <see cref="Views.Regions.AboutContentView"/> に対応する ViewModel を表します。
/// </summary>
public class AboutContentViewModel : RegionViewModelBase
{
    private static readonly Encoding FILE_ENCODING = Encoding.UTF8;

    private string RootDirectoryPath => Path.Combine(this.ProductInfo.Working, "docs");
    private string DisclaimerPath => Path.Combine(this.RootDirectoryPath, "DISCLAIMER.md");
    private string HistoryPath => Path.Combine(this.RootDirectoryPath, "HISTORY.md");
    private string OssLicensePath => Path.Combine(this.RootDirectoryPath, "OSS_LICENSE.md");
    private string PrivacyPolicyPath => Path.Combine(this.RootDirectoryPath, "PRIVACY_POLICY.md");

    [Dependency]
    public ICommonDialogService DialogService { get; set; }
    [Dependency]
    public IProductInfo ProductInfo { get; set; }

    public ReactiveProperty<string> Disclaimer { get; }
    public ReactiveProperty<string> History { get; }
    public ReactiveProperty<string> OssLicense { get; }
    public ReactiveProperty<string> PrivacyPolicy { get; }

    public ReactiveCommand<EventArgs> LoadedHandler { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
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
