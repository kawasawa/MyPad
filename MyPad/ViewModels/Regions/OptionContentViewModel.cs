using MyBase;
using MyBase.Logging;
using MyBase.Wpf.CommonDialogs;
using MyPad.Models;
using MyPad.Properties;
using MyPad.PubSub;
using Prism.Events;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using Unity;

namespace MyPad.ViewModels.Regions;

/// <summary>
/// <see cref="Views.Regions.OptionContentView"/> に対応する ViewModel を表します。
/// </summary>
public class OptionContentViewModel : RegionViewModelBase
{
    [Dependency]
    public IEventAggregator EventAggregator { get; set; }
    [Dependency]
    public IDialogService DialogService { get; set; }
    [Dependency]
    public ICommonDialogService CommonDialogService { get; set; }
    [Dependency]
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public IProductInfo ProductInfo { get; set; }
    [Dependency]
    public Settings Settings { get; set; }
    [Dependency]
    public SyntaxService SyntaxService { get; set; }

    public ReactiveProperty<bool> IsPending { get; }

    public ReactiveCommand<object> SelectDirectoryCommand { get; }
    public ReactiveCommand<object> RemoveDirectoryCommand { get; }
    public ReactiveCommand RecreateExplorerCommand { get; }
    public ReactiveCommand InitializeSyntaxCommand { get; }
    public ReactiveCommand ImportSettingsFileCommand { get; }
    public ReactiveCommand ExportSettingsFileCommand { get; }
    public ReactiveCommand InitializeSettingsCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public OptionContentViewModel()
    {
        this.IsPending = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);

        this.SelectDirectoryCommand = this.IsPending.Inverse().ToReactiveCommand<object>()
            .WithSubscribe(args =>
            {
                var parameter = new FolderBrowserDialogParameters();
                var info = args as ToolSettings.PathInfo;
                if (string.IsNullOrEmpty(info?.Path) == false)
                    parameter.InitialDirectory = info.Path;

                var ready = this.CommonDialogService.ShowDialog(parameter);
                if (ready == false)
                    return;

                if (info != null)
                    info.Path = parameter.FileName;
                else
                    this.Settings.OtherTools.ExplorerRoots.Add(new ToolSettings.PathInfo() { Path = parameter.FileName });
            })
            .AddTo(this.CompositeDisposable);

        this.RemoveDirectoryCommand = this.IsPending.Inverse().ToReactiveCommand<object>()
            .WithSubscribe(args =>
            {
                var info = (ToolSettings.PathInfo)args;
                this.Settings.OtherTools.ExplorerRoots.Remove(info);
            })
            .AddTo(this.CompositeDisposable);

        this.RecreateExplorerCommand = this.IsPending.Inverse().ToReactiveCommand()
            .WithSubscribe(() => this.EventAggregator?.GetEvent<RecreateExplorerEvent>().Publish())
            .AddTo(this.CompositeDisposable);

        this.InitializeSyntaxCommand = this.IsPending.Inverse().ToReactiveCommand()
            .WithSubscribe(() =>
            {
                if (this.DialogService.Confirm(Resources.Message_ConfirmInitializeSyntax))
                    this.SyntaxService.CreateDefinitionFiles(true);
            })
            .AddTo(this.CompositeDisposable);

        this.ImportSettingsFileCommand = this.IsPending.Inverse().ToReactiveCommand()
            .WithSubscribe(() =>
            {
                var fileName = Path.GetFileNameWithoutExtension(this.Settings.FilePath);
                var extension = Path.GetExtension(this.Settings.FilePath);
                var parameters = new OpenFileDialogParameters()
                {
                    Filter = $"{Resources.Label_SettingFile}|*{extension}|{Resources.Label_AllFiles}|*.*",
                    DefaultExtension = extension,
                };
                var ready = this.CommonDialogService.ShowDialog(parameters);
                if (ready == false)
                    return;

                if (this.DialogService.Confirm(Resources.Message_ConfirmOverrideSettings))
                    this.ImportSettingsFile(parameters.FileName);
            })
            .AddTo(this.CompositeDisposable);

        this.ExportSettingsFileCommand = this.IsPending.Inverse().ToReactiveCommand()
            .WithSubscribe(() =>
            {
                var fileName = Path.GetFileNameWithoutExtension(this.Settings.FilePath);
                var extension = Path.GetExtension(this.Settings.FilePath);
                var parameters = new SaveFileDialogParameters()
                {
                    DefaultFileName = $"{fileName} ({DateTime.Now:yyyyMMddHHmmss})",
                    Filter = $"{Resources.Label_SettingFile}|*{extension}|{Resources.Label_AllFiles}|*.*",
                    DefaultExtension = extension,
                };
                var ready = this.CommonDialogService.ShowDialog(parameters);
                if (ready == false)
                    return;

                this.ExportSettingsFile(parameters.FileName);
            })
            .AddTo(this.CompositeDisposable);

        this.InitializeSettingsCommand = this.IsPending.Inverse().ToReactiveCommand()
            .WithSubscribe(() =>
            {
                if (this.DialogService.Confirm(Resources.Message_ConfirmInitializeSettings))
                    this.Settings.Initialize(true);
            })
            .AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// 指定されたパスのファイルを読み込み、設定情報をインポートします。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private bool ImportSettingsFile(string path)
    {
        try
        {
            this.IsPending.Value = true;
            this.Settings.Load(path);
            this.Settings.Save();
            this.Logger.Log($"設定ファイルをインポートしました。: Path={path}", Category.Info);
        }
        catch (Exception e)
        {
            this.Logger.Log($"設定ファイルのインポートに失敗しました。: Path={path}", Category.Error, e);
            this.DialogService.Warn(e.Message);
            return false;
        }
        finally
        {
            this.IsPending.Value = false;
        }
        return true;
    }

    /// <summary>
    /// 設定情報を指定のファイルのエクスポートします。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private bool ExportSettingsFile(string path)
    {
        try
        {
            this.IsPending.Value = true;
            this.Settings.Save(path);
            Process.Start("explorer.exe", $"/select, {path}");
            this.Logger.Log($"設定ファイルをエクスポートしました。: Path={path}", Category.Info);
        }
        catch (Exception e)
        {
            this.Logger.Log($"設定ファイルのエクスポートに失敗しました。: Path={path}", Category.Error, e);
            this.DialogService.Warn(e.Message);
            return false;
        }
        finally
        {
            this.IsPending.Value = false;
        }
        return true;
    }
}
