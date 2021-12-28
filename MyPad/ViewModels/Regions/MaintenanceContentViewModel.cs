using LiveCharts;
using LiveCharts.Defaults;
using MyBase;
using MyBase.Logging;
using MyBase.Wpf.CommonDialogs;
using MyPad.PubSub;
using Prism.Events;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Unity;

namespace MyPad.ViewModels.Regions
{
    /// <summary>
    /// <see cref="Views.Regions.MaintenanceContentView"/> に対応する ViewModel を表します。
    /// </summary>
    public class MaintenanceContentViewModel : ViewModelBase
    {
        // Constructor Injection
        public IEventAggregator EventAggregator { get; set; }

        // Dependency Injection
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

        public ReactiveCollection<string> TraceLogs { get; }
        public ReactiveCollection<string> DebugLogs { get; }
        public ReactiveCollection<string> InfoLogs { get; }
        public ReactiveCollection<string> WarnLogs { get; }

        public ChartValues<ObservableValue> CpuUsage { get; }
        public ChartValues<ObservableValue> MemoryUsage { get; }

        public ReactiveProperty<bool> IsPending { get; }

        public ReactiveCommand ExportLogArchiveCommand { get; }
        public ReactiveCommand UpdateLogsCommand { get; }
        public ReactiveCommand<DependencyPropertyChangedEventArgs> IsVisibleChangedHandler { get; }

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        /// <param name="eventAggregator"></param>
        [InjectionConstructor]
        [LogInterceptor]
        public MaintenanceContentViewModel(IEventAggregator eventAggregator)
        {
            // ----- インジェクション ------------------------------

            this.EventAggregator = eventAggregator;

            // ----- プロパティの定義 ------------------------------
            this.TraceLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.TraceLogs, new object());
            this.DebugLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.DebugLogs, new object());
            this.InfoLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.InfoLogs, new object());
            this.WarnLogs = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.WarnLogs, new object());

            this.CpuUsage = new();
            this.MemoryUsage = new();

            this.IsPending = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);

            // ----- コマンドの定義 ------------------------------

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

            this.UpdateLogsCommand = new ReactiveCommand()
                .WithSubscribe(() => this.UpdateLogs())
                .AddTo(this.CompositeDisposable);

            this.IsVisibleChangedHandler = new ReactiveCommand<DependencyPropertyChangedEventArgs>()
                .WithSubscribe(e =>
                {
                    if (e.NewValue is bool isVisible && isVisible)
                        this.UpdateLogs();
                })
                .AddTo(this.CompositeDisposable);

            // ----- PUB/SUB メッセージ ------------------------------

            void performanceChecked((double? processorTime, double? workingSetPrivate) payload)
                => this.PerformanceChecked(payload.processorTime, payload.workingSetPrivate);
            this.EventAggregator.GetEvent<PerformanceCheckedEvent>().Subscribe(performanceChecked);
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

        /// <summary>
        /// 最新のログを取得します。
        /// </summary>
        [LogInterceptor]
        private void UpdateLogs()
        {
            static IEnumerable<string> getLogs(NLog.ILogger coreLogger, int startAt)
                        => coreLogger.Factory.Configuration.ConfiguredNamedTargets.OfType<NLog.Targets.MemoryTarget>().FirstOrDefault()?.Logs.Skip(startAt) ?? Enumerable.Empty<string>();

            this.IsPending.Value = true;
            var nlogger = ((CompositeLogger)this.Logger).OfType<NLogger>().First();
            this.TraceLogs.AddRangeOnScheduler(getLogs(nlogger.TraceCoreLogger.Value, this.TraceLogs.Count));
            this.DebugLogs.AddRangeOnScheduler(getLogs(nlogger.DebugCoreLogger.Value, this.DebugLogs.Count));
            this.InfoLogs.AddRangeOnScheduler(getLogs(nlogger.InfoCoreLogger.Value, this.InfoLogs.Count));
            this.WarnLogs.AddRangeOnScheduler(getLogs(nlogger.WarnCoreLogger.Value, this.WarnLogs.Count));
            this.IsPending.Value = false;
        }

        /// <summary>
        /// 計測されたパフォーマンス情報を保管します。
        /// </summary>
        /// <param name="processorTime">CPU 時間</param>
        /// <param name="workingSetPrivate">ワーキングメモリ</param>
        [LogInterceptorIgnore]
        private void PerformanceChecked(double? processorTime, double? workingSetPrivate)
        {
            if (processorTime.HasValue)
                this.CpuUsage.Add(new ObservableValue(processorTime.Value));
            if (AppSettingsReader.PerformanceGraphLimit < this.CpuUsage.Count)
                this.CpuUsage.RemoveAt(0);

            if (workingSetPrivate.HasValue)
                this.MemoryUsage.Add(new ObservableValue(workingSetPrivate.Value));
            if (AppSettingsReader.PerformanceGraphLimit < this.MemoryUsage.Count)
                this.MemoryUsage.RemoveAt(0);
        }
    }
}
