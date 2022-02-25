using MyBase;
using MyBase.Logging;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace MyPad;

/// <summary>
/// アプリケーション全体で共有される情報の保管庫を表します。
/// </summary>
public sealed class SharedDataStore : ValidatableBase
{
    private readonly ILoggerFacade _logger;
    private readonly IProductInfo _productInfo;
    private readonly Process _process;

    public static readonly TimeSpan DownedPomodoroTimerValue = TimeSpan.Zero;

    public ReactiveProperty<TimeSpan> PomodoroTimer { get; }
    public ReactiveProperty<bool> IsInPomodoro { get; }
    public ReactiveProperty<bool> IsPomodoroWorking { get; }

    /// <summary>
    /// このアプリケーションの実行中のバージョンにおける固有の識別子
    /// </summary>
    public string Identifier => $"__{this._productInfo.Company}:{this._productInfo.Product}:{this._productInfo.Version}__";

    /// <summary>
    /// ログフォルダのパス
    /// </summary>
    public string LogDirectoryPath => Path.Combine(this._productInfo.Local, "log");

    /// <summary>
    /// 一時フォルダのパス
    /// </summary>
    public string TempDirectoryPath => Path.Combine(this._productInfo.Temporary, this._process.StartTime.ToString("yyyyMMddHHmmssfff"));

    /// <summary>
    /// キャッシュフォルダのパス
    /// </summary>
    public IEnumerable<string> CachedDirectories { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// コマンドライン引数
    /// </summary>
    public IEnumerable<string> CommandLineArgs { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="logger">ロガー</param>
    /// <param name="productInfo">プロダクト情報</param>
    /// <param name="process">プロセス情報</param>
    public SharedDataStore(ILoggerFacade logger, IProductInfo productInfo, Process process)
    {
        this._logger = logger;
        this._productInfo = productInfo;
        this._process = process;

        this.PomodoroTimer = new ReactiveProperty<TimeSpan>(DownedPomodoroTimerValue).AddTo(this.CompositeDisposable);
        this.IsInPomodoro = this.PomodoroTimer.Select(t => t != DownedPomodoroTimerValue).ToReactiveProperty().AddTo(this.CompositeDisposable);
        this.IsPomodoroWorking = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
    }

    /// <summary>
    /// このプロセスで使用する一時フォルダを生成する。
    /// </summary>
    public void CreateTempDirectory()
    {
        // フォルダを作成し、隠し属性を付与する
        var info = new DirectoryInfo(this.TempDirectoryPath);
        info.Create();
        info.Attributes |= FileAttributes.Hidden;

        this._logger.Debug($"実行中のプロセスが使用する一時フォルダを作成しました。: Path={this.TempDirectoryPath}");
    }
}
