using MyBase;
using System.Diagnostics;
using System.IO;

namespace MyPad;

/// <summary>
/// アプリケーション全体で共有される情報の保管庫を表します。
/// </summary>
public sealed class SharedDataStore
{
    /// <summary>
    /// ログフォルダのパス
    /// </summary>
    public string LogDirectoryPath { get; }

    /// <summary>
    /// 一時フォルダのパス
    /// </summary>
    public string TempDirectoryPath { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="productInfo">プロダクト情報</param>
    /// <param name="process">プロセス情報</param>
    [LogInterceptorIgnore]
    public SharedDataStore(IProductInfo productInfo, Process process)
    {
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
