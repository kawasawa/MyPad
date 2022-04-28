using MyBase;
using System.Diagnostics;
using System.IO;

namespace MyPad;

/// <summary>
/// プロダクト情報を取得するためのサービスを表します。
/// </summary>
public class AppProductInfo : ProductInfo
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
    [LogInterceptorIgnore] // このクラスが初期化されるのはロガーの生成前のため
    public AppProductInfo()
    {
        var process = Process.GetCurrentProcess();
        this.LogDirectoryPath = Path.Combine(this.Local, "log");
        this.TempDirectoryPath = Path.Combine(this.Temporary, process.StartTime.ToString("yyyyMMddHHmmssfff"));
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
