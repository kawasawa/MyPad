using CaseExtensions;
using Microsoft.Extensions.Configuration;
using System;

namespace MyPad
{
    /// <summary>
    /// アプリケーションの構成情報を表します。
    /// </summary>
    public static class AppSettings
    {
        private static readonly Lazy<IConfigurationRoot> _lazyConfiguration
            = new Lazy<IConfigurationRoot>(() =>
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

        public static int ToastMaxCount
            => int.TryParse(_lazyConfiguration.Value[nameof(ToastMaxCount).ToSnakeCase()], out var value) && 0 <= value ? value : 5;

        public static long FileSizeThreshold
            => long.TryParse(_lazyConfiguration.Value[nameof(FileSizeThreshold).ToSnakeCase()], out var value) && SIZE_MB <= value ? value : 10 * SIZE_MB;

        public static long TerminalBufferSize
            => long.TryParse(_lazyConfiguration.Value[nameof(TerminalBufferSize).ToSnakeCase()], out var value) && 0 <= value ? value : 10000;
    }
}
