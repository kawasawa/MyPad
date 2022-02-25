using MyBase;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyPad;

/// <summary>
/// アプリケーション全体で共有される情報の保管庫を表します。
/// </summary>
public sealed class SharedDataStore
{
    /// <summary>
    /// このアプリケーションの実行中のバージョンにおける固有の識別子
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// ログフォルダのパス
    /// </summary>
    public string LogDirectoryPath { get; }

    /// <summary>
    /// 一時フォルダのパス
    /// </summary>
    public string TempDirectoryPath { get; }

    /// <summary>
    /// コマンドライン引数
    /// </summary>
    public IEnumerable<string> CommandLineArgs { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// キャッシュフォルダのパス
    /// </summary>
    public IEnumerable<string> CachedDirectories { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="productInfo">プロダクト情報</param>
    /// <param name="process">プロセス情報</param>
    [LogInterceptorIgnore]
    public SharedDataStore(IProductInfo productInfo, Process process)
    {
        this.Identifier = $"__{productInfo.Company}:{productInfo.Product}:{productInfo.Version}__";
        this.LogDirectoryPath = Path.Combine(productInfo.Local, "log");
        this.TempDirectoryPath = Path.Combine(productInfo.Temporary, process.StartTime.ToString("yyyyMMddHHmmssfff"));
    }

    /// <summary>
    /// このプロセスで使用する一時フォルダを生成する。
    /// </summary>
    [LogInterceptor]
    public void CreateTempDirectory()
    {
        // フォルダを作成し、隠し属性を付与する
        var info = new DirectoryInfo(this.TempDirectoryPath);
        info.Create();
        info.Attributes |= FileAttributes.Hidden;
    }
}
