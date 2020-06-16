using CaseExtensions;
using Microsoft.Extensions.Configuration;
using System;

namespace MyPad
{
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

        public static int TempsLifetime
            => int.TryParse(_lazyConfiguration.Value[nameof(TempsLifetime).ToSnakeCase()], out var value) && 0 <= value ? value : 10;

        public static long LargeSizeBorder
        {
            get
            {
                const long MB = 1048576;
                return long.TryParse(_lazyConfiguration.Value[nameof(LargeSizeBorder).ToSnakeCase()], out var value) && MB <= value ? value : 10 * MB;
            }
        }

        public static string ProjectSite
            => _lazyConfiguration.Value[nameof(ProjectSite).ToSnakeCase()];

        public static string DonationSite
           => _lazyConfiguration.Value[nameof(DonationSite).ToSnakeCase()];

        public static int DonationAmount
           => int.TryParse(_lazyConfiguration.Value[nameof(DonationAmount).ToSnakeCase()], out var value) && 100 <= value ? value : 100;
    }
}
