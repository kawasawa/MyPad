using Microsoft.VisualBasic.FileIO;
using MyPad.Models;
using MyPad.Properties;
using MyPad.ViewModels.Events;
using Plow;
using Plow.Wpf.CommonDialogs;
using Prism.Events;
using Prism.Logging;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
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
        public SharedDataService SharedDataService { get; set; }
        [Dependency]
        public SettingsService SettingsService { get; set; }
        [Dependency]
        public SyntaxService SyntaxService { get; set; }

        public ReactiveProperty<bool> IsWorking { get; }

        public ReactiveCommand<object> SelectDirectoryCommand { get; }
        public ReactiveCommand<object> RemoveDirectoryCommand { get; }
        public ReactiveCommand RefreshExplorerCommand { get; }
        public ReactiveCommand OpenAppDataDirectoryCommand { get; }
        public ReactiveCommand ExportLogArchiveCommand { get; }
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
                    var info = (ToolSettings.PathInfo)args;
                    var parameter = new FolderBrowserDialogParameters();
                    if (string.IsNullOrEmpty(info.Path) == false)
                        parameter.InitialDirectory = info.Path;
                    var ready = this.CommonDialogService.ShowDialog(parameter);
                    if (ready)
                        info.Path = parameter.FileName;
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

            this.OpenAppDataDirectoryCommand = this.IsWorking.Inverse().ToReactiveCommand()
                .WithSubscribe(() => this.OpenAppDataDirectory())
                .AddTo(this.CompositeDisposable);

            this.ExportLogArchiveCommand = this.IsWorking.Inverse().ToReactiveCommand()
                .WithSubscribe(async () =>
                {
                    var parameters = new SaveFileDialogParameters()
                    {
                        DefaultFileName = $"{this.ProductInfo.Product}-log ({DateTime.Now:yyyyMMddHHmmss})",
                        Filter = "ZIP|*.zip",
                        DefaultExtension = ".zip",
                    };
                    var ready = this.CommonDialogService.ShowDialog(parameters);
                    if (ready == false)
                        return;

                    await this.ExportLogArchive(parameters.FileName);
                })
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
        private void OpenAppDataDirectory()
        {
            try
            {
                var path = string.Empty;
                try
                {
                    path = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
                }
                catch
                {
                    path = this.ProductInfo.Roaming;
                }
                Process.Start("explorer.exe", path);
                this.Logger.Log($"データフォルダをオープンしました。: Path={path}", Category.Info);
            }
            catch (Exception e)
            {
                this.Logger.Log($"データフォルダのオープンに失敗しました。", Category.Warn, e);
                this.DialogService.Warn(e.Message);
            }
        }

        [LogInterceptor]
        private async Task<bool> ExportLogArchive(string path)
        {
            const int LOOP_DELAY = 500;

            var tempPath = Path.Combine(this.SharedDataService.TempDirectoryPath, Path.GetFileNameWithoutExtension(path));

            try
            {
                this.IsWorking.Value = true;

                this.SharedDataService.CreateTempDirectory();

                // 一時フォルダに退避する
                if (Directory.Exists(tempPath))
                    await Task.Run(() => Directory.Delete(tempPath, true));
                while (Directory.Exists(tempPath))
                    await Task.Delay(LOOP_DELAY);
                await Task.Run(() => FileSystem.CopyDirectory(this.SharedDataService.LogDirectoryPath, tempPath, UIOption.AllDialogs, UICancelOption.ThrowException));

                // 退避したファイルを圧縮して出力する
                if (File.Exists(path))
                    await Task.Run(() => File.Delete(path));
                while (File.Exists(path))
                    await Task.Delay(LOOP_DELAY);
                await Task.Run(() => ZipFile.CreateFromDirectory(tempPath, path, CompressionLevel.Optimal, false));

                Process.Start("explorer.exe", $"/select, {path}");
                this.Logger.Log($"ログファイルを出力しました。: Path={path}, Temp={tempPath}", Category.Info);
            }
            catch (OperationCanceledException e)
            {
                // NOTE: FileSystem.CopyDirectory の処理をキャンセルした場合
                this.DialogService.Notify(e.Message);
                return false;
            }
            catch (Exception e)
            {
                this.Logger.Log($"ログファイルの出力に失敗しました。: Path={path}, Temp={tempPath}", Category.Warn, e);
                this.DialogService.Warn(e.Message);
                return false;
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    _ = Task.Run(() => { try { Directory.Delete(tempPath, true); } catch { } });
                this.IsWorking.Value = false;
            }
            return true;
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
                this.Logger.Log($"設定ファイルのインポートに失敗しました。: Path={path}", Category.Warn, e);
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
                this.Logger.Log($"設定ファイルのエクスポートに失敗しました。: Path={path}", Category.Warn, e);
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
