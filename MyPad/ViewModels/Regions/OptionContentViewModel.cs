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

namespace MyPad.ViewModels.Regions
{
    public class OptionContentViewModel : ViewModelBase
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
        public SettingsService SettingsService { get; set; }
        [Dependency]
        public SyntaxService SyntaxService { get; set; }

        public ReactiveProperty<bool> IsWorking { get; }

        public ReactiveCommand<object> SelectDirectoryCommand { get; }
        public ReactiveCommand<object> RemoveDirectoryCommand { get; }
        public ReactiveCommand RefreshExplorerCommand { get; }
        public ReactiveCommand InitializeSyntaxCommand { get; }
        public ReactiveCommand ImportSettingsFileCommand { get; }
        public ReactiveCommand ExportSettingsFileCommand { get; }
        public ReactiveCommand InitializeSettingsCommand { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public OptionContentViewModel()
        {
            this.IsWorking = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);

            this.SelectDirectoryCommand = this.IsWorking.Inverse().ToReactiveCommand<object>()
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
                        this.SettingsService.OtherTools.ExplorerRoots.Add(new ToolSettings.PathInfo() { Path = parameter.FileName });
                })
                .AddTo(this.CompositeDisposable);

            this.RemoveDirectoryCommand = this.IsWorking.Inverse().ToReactiveCommand<object>()
                .WithSubscribe(args =>
                {
                    var info = (ToolSettings.PathInfo)args;
                    this.SettingsService.OtherTools.ExplorerRoots.Remove(info);
                })
                .AddTo(this.CompositeDisposable);

            this.RefreshExplorerCommand = this.IsWorking.Inverse().ToReactiveCommand()
                .WithSubscribe(() => this.EventAggregator?.GetEvent<RefreshExplorerEvent>().Publish())
                .AddTo(this.CompositeDisposable);

            this.InitializeSyntaxCommand = this.IsWorking.Inverse().ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    if (this.DialogService.Confirm(Resources.Message_ConfirmInitializeSyntax))
                        this.SyntaxService.Initialize(true);
                })
                .AddTo(this.CompositeDisposable);

            this.ImportSettingsFileCommand = this.IsWorking.Inverse().ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(this.SettingsService.FilePath);
                    var extension = Path.GetExtension(this.SettingsService.FilePath);
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

            this.ExportSettingsFileCommand = this.IsWorking.Inverse().ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(this.SettingsService.FilePath);
                    var extension = Path.GetExtension(this.SettingsService.FilePath);
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

            this.InitializeSettingsCommand = this.IsWorking.Inverse().ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    if (this.DialogService.Confirm(Resources.Message_ConfirmInitializeSettings))
                        this.SettingsService.Initialize(true);
                })
                .AddTo(this.CompositeDisposable);
        }

        [LogInterceptor]
        private bool ImportSettingsFile(string path)
        {
            try
            {
                this.IsWorking.Value = true;
                this.SettingsService.Load(path);
                this.SettingsService.Save();
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
                this.IsWorking.Value = false;
            }
            return true;
        }
        
        [LogInterceptor]
        private bool ExportSettingsFile(string path)
        {
            try
            {
                this.IsWorking.Value = true;
                this.SettingsService.Save(path);
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
                this.IsWorking.Value = false;
            }
            return true;
        }
    }
}
