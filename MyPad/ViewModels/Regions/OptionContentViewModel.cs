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
using System.Windows;
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
                        Filter = "ZIP|.zip",
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
            var sourcePath = ((App)Application.Current).LogDirectoryPath;
            var tempPath = Path.Combine(this.ProductInfo.Temporary, Path.GetFileNameWithoutExtension(path));

            try
            {
                this.IsWorking.Value = true;

                await Task.Run(() =>
                {
                    FileSystem.CopyDirectory(sourcePath, tempPath, UIOption.AllDialogs, UICancelOption.ThrowException);
                    ZipFile.CreateFromDirectory(tempPath, path, CompressionLevel.Optimal, false);
                });
                _ = Task.Run(() => Directory.Delete(tempPath, true));

                Process.Start("explorer.exe", $"/select, {path}");
                this.Logger.Log($"ログファイルを出力しました。: Path={path}, Temp={tempPath}", Category.Info);
            }
            catch (OperationCanceledException e)
            {
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
                this.IsWorking.Value = false;
            }
            return true;
        }
    }
}
