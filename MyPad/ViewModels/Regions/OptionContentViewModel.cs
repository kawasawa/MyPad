using Microsoft.VisualBasic.FileIO;
using MyPad.Models;
using MyPad.Properties;
using Plow;
using Plow.Wpf.CommonDialogs;
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

        public ReactiveCommand OpenAppDataDirectoryCommand { get; }
        public ReactiveCommand ExportLogArchiveCommand { get; }
        public ReactiveCommand InitializeSyntaxCommand { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public OptionContentViewModel()
        {
            this.IsWorking = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);

            this.OpenAppDataDirectoryCommand = new ReactiveCommand()
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
            const int MAX_LOOP_COUNT = 10;
            const int LOOP_DELAY = 500;

            var sourcePath = this.SharedDataService.LogDirectoryPath;
            var tempPath = Path.Combine(this.SharedDataService.TempDirectoryPath, Path.GetFileNameWithoutExtension(path));

            try
            {
                this.IsWorking.Value = true;

                // 一時フォルダに退避する
                if (Directory.Exists(tempPath))
                    await Task.Run(() => Directory.Delete(tempPath, true));
                for (var i = 0; i < MAX_LOOP_COUNT && Directory.Exists(tempPath); i++)
                    await Task.Delay(LOOP_DELAY);
                if (Directory.Exists(tempPath))
                    throw new IOException(Resources.Message_NotifyTryAgainLater);
                await Task.Run(() => FileSystem.CopyDirectory(sourcePath, tempPath, UIOption.AllDialogs, UICancelOption.ThrowException));

                // 退避したファイルを圧縮して出力する
                if (File.Exists(path))
                    await Task.Run(() => File.Delete(path));
                for (var i = 0; i < MAX_LOOP_COUNT && File.Exists(path); i++)
                    await Task.Delay(LOOP_DELAY);
                if (File.Exists(path))
                    throw new IOException(Resources.Message_NotifyTryAgainLater);
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
    }
}
