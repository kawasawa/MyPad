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
    /// キャッシュの保存期間(日)
    /// </summary>
    public static int CacheLifetime
        => int.TryParse(_lazyConfiguration.Value[nameof(CacheLifetime).ToSnakeCase()], out var value) && 0 <= value ? value : 10;

    /// <summary>
    /// トースト通知の表示時間(秒)
    /// </summary>
    public static int ToastDuration
        => int.TryParse(_lazyConfiguration.Value[nameof(ToastDuration).ToSnakeCase()], out var value) && 0 <= value ? value : 5;

    /// <summary>
    /// トースト通知の表示数の最大値
    /// </summary>
    public static int ToastCountLimit
        => int.TryParse(_lazyConfiguration.Value[nameof(ToastCountLimit).ToSnakeCase()], out var value) && 0 <= value ? value : 5;

    /// <summary>
    /// パフォーマンスグラフの表示数の最大値
    /// </summary>
    public static int PerformanceCountLimit
        => int.TryParse(_lazyConfiguration.Value[nameof(PerformanceCountLimit).ToSnakeCase()], out var value) && 10 <= value ? value : 50;

    /// <summary>
    /// パフォーマンス計測のインターバル(秒)
    /// </summary>
    public static int PerformanceCheckInterval
        => int.TryParse(_lazyConfiguration.Value[nameof(PerformanceCheckInterval).ToSnakeCase()], out var value) && 1 <= value ? value : 60;

    /// <summary>
    /// ポモドーロタイマーの継続時間の最大値(分)
    /// </summary>
    public static int PomodoroDurationLimit
        => int.TryParse(_lazyConfiguration.Value[nameof(PomodoroDurationLimit).ToSnakeCase()], out var value) && 5 <= value ? value : 600;

    /// <summary>
    /// ターミナルの表示行数の最大値
    /// </summary>
    public static long TerminalLineLimit
        => long.TryParse(_lazyConfiguration.Value[nameof(TerminalLineLimit).ToSnakeCase()], out var value) && 0 <= value ? value : 10000;

    /// <summary>
    /// テキストエディタが大型ファイルと判断するサイズの閾値
    /// </summary>
    public static long HugeSizeThreshold
        => long.TryParse(_lazyConfiguration.Value[nameof(HugeSizeThreshold).ToSnakeCase()], out var value) && SIZE_MB <= value ? value : 10 * SIZE_MB;
}
