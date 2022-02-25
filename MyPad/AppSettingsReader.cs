using CaseExtensions;
using Microsoft.Extensions.Configuration;
using System;

namespace MyPad;

/// <summary>
/// アプリケーション構成ファイルの情報を読み込みます。
/// </summary>
public static class AppSettingsReader
{
    private static readonly long SIZE_MB = 1024 * 1024;
    private static readonly Lazy<IConfigurationRoot> _lazyConfiguration
        = new(() =>
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
            builder.AddJsonFile("appsettings.json");
            return builder.Build();
        });

    /// <summary>
    /// プロジェクトの Web サイト
    /// </summary>
    public static string ProjectSite
        => _lazyConfiguration.Value[nameof(ProjectSite).ToSnakeCase()];

    /// <summary>
    /// 作成者の Web サイト
    /// </summary>
    public static string CreatorSite
        => _lazyConfiguration.Value[nameof(CreatorSite).ToSnakeCase()];

    /// <summary>
    /// 寄付用の Web サイト
    /// </summary>
    public static string DonationSite
       => _lazyConfiguration.Value[nameof(DonationSite).ToSnakeCase()];

    /// <summary>
    /// キャッシュの保管期限
    /// </summary>
    public static int CacheLifetime
        => int.TryParse(_lazyConfiguration.Value[nameof(CacheLifetime).ToSnakeCase()], out var value) && 0 <= value ? value : 10;

    /// <summary>
    /// トースト通知の表示時間
    /// </summary>
    public static int ToastLifetime
        => int.TryParse(_lazyConfiguration.Value[nameof(ToastLifetime).ToSnakeCase()], out var value) && 0 <= value ? value : 5;

    /// <summary>
    /// トースト通知の最大表示数
    /// </summary>
    public static int ToastCountLimit
        => int.TryParse(_lazyConfiguration.Value[nameof(ToastCountLimit).ToSnakeCase()], out var value) && 0 <= value ? value : 5;

    /// <summary>
    /// ポモドーロタイマーのインターバルの最大値
    /// </summary>
    public static int PomodoroMaxInterval
        => int.TryParse(_lazyConfiguration.Value[nameof(PomodoroMaxInterval).ToSnakeCase()], out var value) && 5 <= value ? value : 600;

    /// <summary>
    /// パフォーマンス計測のインターバル
    /// </summary>
    public static int PerformanceCheckInterval
        => int.TryParse(_lazyConfiguration.Value[nameof(PerformanceCheckInterval).ToSnakeCase()], out var value) && 500 <= value ? value : 2000;

    /// <summary>
    /// パフォーマンスグラフの測定値表示数
    /// </summary>
    public static int PerformanceGraphLimit
        => int.TryParse(_lazyConfiguration.Value[nameof(PerformanceGraphLimit).ToSnakeCase()], out var value) && 10 <= value ? value : 50;

    /// <summary>
    /// テキストエディターのファイルサイズ制限
    /// </summary>
    public static long EditorFileSizeThreshold
        => long.TryParse(_lazyConfiguration.Value[nameof(EditorFileSizeThreshold).ToSnakeCase()], out var value) && SIZE_MB <= value ? value : 10 * SIZE_MB;

    /// <summary>
    /// ターミナルの最大表示行数
    /// </summary>
    public static long TerminalBufferSize
        => long.TryParse(_lazyConfiguration.Value[nameof(TerminalBufferSize).ToSnakeCase()], out var value) && 0 <= value ? value : 10000;
}
