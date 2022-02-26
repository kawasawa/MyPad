using MyBase;
using MyBase.Logging;
using MyBase.Wpf.CommonDialogs;
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

namespace MyPad.ViewModels.Regions;

/// <summary>
/// <see cref="Views.Regions.MaintenanceContentView"/> に対応する ViewModel を表します。
/// </summary>
public class MaintenanceContentViewModel : RegionViewModelBase
{
    private SharedProperties _sharedProperties;
    [Dependency]
    public IDialogService DialogService { get; set; }
    [Dependency]
    public ICommonDialogService CommonDialogService { get; set; }
    [Dependency]
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public IProductInfo ProductInfo { get; set; }
    [Dependency]
    public SharedDataStore SharedDataStore { get; set; }
    [Dependency]
    public SharedProperties SharedProperties
    {
        get => this._sharedProperties;
        set => this.SetProperty(ref this._sharedProperties, value);
    }

    public ReactiveProperty<bool> IsPending { get; }

    public ReactiveCommand ExportLogArchiveCommand { get; }
    public ReactiveCommand CollectLogsCommand { get; }
    public ReactiveCommand<DependencyPropertyChangedEventArgs> IsVisibleChangedHandler { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public MaintenanceContentViewModel()
    {
        this.IsPending = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);

        this.ExportLogArchiveCommand = this.IsPending.Inverse().ToReactiveCommand()
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

        this.CollectLogsCommand = new ReactiveCommand()
            .WithSubscribe(async () =>
            {
                this.IsPending.Value = true;
                await this.SharedProperties.CollectLogs();
                this.IsPending.Value = false;
            })
            .AddTo(this.CompositeDisposable);

        this.IsVisibleChangedHandler = new ReactiveCommand<DependencyPropertyChangedEventArgs>()
            .WithSubscribe(async e =>
            {
                if (e.NewValue is bool isVisible && isVisible)
                {
                    this.IsPending.Value = true;
                    await this.SharedProperties.CollectLogs();
                    this.IsPending.Value = false;
                }
            })
            .AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// ログファイルのアーカイブを指定のパスに出力します。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private async Task<bool> ExportLogArchive(string path)
    {
        const int LOOP_DELAY = 500;

        var tempPath = Path.Combine(this.SharedDataStore.TempDirectoryPath, Path.GetFileNameWithoutExtension(path));

        try
        {
            this.IsPending.Value = true;

            this.SharedDataStore.CreateTempDirectory();

            // 一時フォルダに複製する
            if (Directory.Exists(tempPath))
                await Task.Run(() => Directory.Delete(tempPath, true));
            while (Directory.Exists(tempPath))
                await Task.Delay(LOOP_DELAY);
            await Task.Run(() => Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(this.SharedDataStore.LogDirectoryPath, tempPath, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException));
            this.Logger.Debug($"ログファイルを一時フォルダに複製しました。: Source={this.SharedDataStore.LogDirectoryPath}, Dest={tempPath}");

            // 複製したファイルを圧縮して出力する
            if (File.Exists(path))
                await Task.Run(() => File.Delete(path));
            while (File.Exists(path))
                await Task.Delay(LOOP_DELAY);
            await Task.Run(() => ZipFile.CreateFromDirectory(tempPath, path, CompressionLevel.Optimal, false));
            this.Logger.Debug($"ログファイルを圧縮して出力しました。: Source={tempPath}, Dest={path}");

            Process.Start("explorer.exe", $"/select, {path}");
            this.Logger.Log($"ログファイルを出力しました。: Path={path}", Category.Info);
        }
        catch (OperationCanceledException e)
        {
            // FileSystem.CopyDirectory の処理をキャンセルした場合
            this.Logger.Log($"ログファイルの出力をキャンセルしました。: Path={path}", Category.Info, e);
            this.DialogService.Notify(e.Message);
            return false;
        }
        catch (Exception e)
        {
            this.Logger.Log($"ログファイルの出力に失敗しました。: Path={path}, Temp={tempPath}", Category.Error, e);
            this.DialogService.Warn(e.Message);
            return false;
        }
        finally
        {
            if (Directory.Exists(tempPath))
                _ = Task.Run(() => { try { Directory.Delete(tempPath, true); } catch { } });
            this.IsPending.Value = false;
        }
        return true;
    }
}
