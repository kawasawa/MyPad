using CaseExtensions;
using Microsoft.Extensions.Configuration;
using System;

namespace MyPad
{
    /// <summary>
    /// アプリケーション構成ファイルの情報を読み込みます。
    /// </summary>
    public static class AppSettingsReader
    {
        private static readonly Lazy<IConfigurationRoot> _lazyConfiguration
            = new(() =>
            {
                var builder = new ConfigurationBuilder();
                builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                builder.AddJsonFile("appsettings.json");
                return builder.Build();
            });

        private static readonly long SIZE_MB = 1024 * 1024;

        public static string ProjectSite
            => _lazyConfiguration.Value[nameof(ProjectSite).ToSnakeCase()];

        public static string CreatorSite
            => _lazyConfiguration.Value[nameof(CreatorSite).ToSnakeCase()];

        public static string DonationSite
           => _lazyConfiguration.Value[nameof(DonationSite).ToSnakeCase()];

        public static int CacheLifetime
            => int.TryParse(_lazyConfiguration.Value[nameof(CacheLifetime).ToSnakeCase()], out var value) && 0 <= value ? value : 10;

        public static int ToastLifetime
            => int.TryParse(_lazyConfiguration.Value[nameof(ToastLifetime).ToSnakeCase()], out var value) && 0 <= value ? value : 5;

        public static int ToastCountLimit
            => int.TryParse(_lazyConfiguration.Value[nameof(ToastCountLimit).ToSnakeCase()], out var value) && 0 <= value ? value : 5;

        public static int PerformanceCheckInterval
            => int.TryParse(_lazyConfiguration.Value[nameof(PerformanceCheckInterval).ToSnakeCase()], out var value) && 500 <= value ? value : 2000;
        
        public static int PerformanceGraphLimit
            => int.TryParse(_lazyConfiguration.Value[nameof(PerformanceGraphLimit).ToSnakeCase()], out var value) && 10 <= value ? value : 50;

        public static long EditorFileSizeThreshold
            => long.TryParse(_lazyConfiguration.Value[nameof(EditorFileSizeThreshold).ToSnakeCase()], out var value) && SIZE_MB <= value ? value : 10 * SIZE_MB;

        public static long TerminalBufferSize
            => long.TryParse(_lazyConfiguration.Value[nameof(TerminalBufferSize).ToSnakeCase()], out var value) && 0 <= value ? value : 10000;
    }
}
