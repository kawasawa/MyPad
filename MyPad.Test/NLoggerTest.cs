using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MyPad.Test
{
    public class NLoggerTest
    {
        private string LogDirectoryPath { get; } = "./log";
        private NLogger Logger { get; set; }

        [SetUp]
        public void Initialize()
        {
            if (Directory.Exists(this.LogDirectoryPath))
                Directory.Delete(this.LogDirectoryPath, true);
            while (Directory.Exists(this.LogDirectoryPath))
                System.Threading.Thread.Sleep(100);

            Directory.CreateDirectory(this.LogDirectoryPath);

            this.Logger = new NLogger()
            {
                ConfigurationFactory = () =>
                {
                    var layout = new NLog.Layouts.CsvLayout();
                    layout.Delimiter = NLog.Layouts.CsvColumnDelimiterMode.Tab;
                    layout.Quoting = NLog.Layouts.CsvQuotingMode.Nothing;
                    layout.WithHeader = false;
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${date:format=yyyy/MM/dd HH\\:mm\\:ss}"));
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${message}"));

                    var target = new NLog.Targets.FileTarget("log");
                    target.Encoding = Encoding.UTF8;
                    target.Footer = "${newline}";
                    target.FileName = "${var:DIR}/${var:CTG}.log";
                    target.ArchiveFileName = "${var:DIR}/archive/{#}.${var:CTG}.log";
                    target.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
                    target.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date;
                    target.MaxArchiveFiles = 10;
                    target.Layout = layout;

                    var config = new NLog.Config.LoggingConfiguration();
                    config.AddTarget(target);
                    config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, target));
                    return config;
                },
                CreateLoggerHook = (logger, category) =>
                {
                    logger.Factory.Configuration.Variables.Add("DIR", this.LogDirectoryPath);
                    logger.Factory.Configuration.Variables.Add("CTG", category.ToString());
                    logger.Factory.ReconfigExistingLoggers();
                },
            };
        }

        private void Log(string message, Plow.Logging.Category category)
        {
            this.Logger.Log(message, category);

            var path = Path.Combine(this.LogDirectoryPath, $"{category}.log");
            Assert.That(File.Exists(path), Is.EqualTo(true));

            var text = File.ReadAllText(path);
            Assert.That(string.IsNullOrEmpty(text), Is.EqualTo(false));

            var match = Regex.Match(text, "^.*?\t(.*)[\r|\n|\r\n]*$", RegexOptions.Singleline);
            Assert.That(match.Groups.Count, Is.Not.EqualTo(0));

            Assert.That(() => match.Groups[1].Value.StartsWith(message), Is.EqualTo(true));
        }

        [TearDown]
        public void Cleanup()
        {
            this.Logger = null;

            if (Directory.Exists(this.LogDirectoryPath))
                Directory.Delete(this.LogDirectoryPath, true);
            while (Directory.Exists(this.LogDirectoryPath))
                System.Threading.Thread.Sleep(100);
        }

        [TestCaseSource(typeof(Source), nameof(Source.Messages))]
        public void Trace(string message)
            => this.Log(message, Plow.Logging.Category.Trace);

        [TestCaseSource(typeof(Source), nameof(Source.Messages))]
        public void Debug(string message)
            => this.Log(message, Plow.Logging.Category.Debug);

        [TestCaseSource(typeof(Source), nameof(Source.Messages))]
        public void Info(string message)
            => this.Log(message, Plow.Logging.Category.Info);

        [TestCaseSource(typeof(Source), nameof(Source.Messages))]
        public void Warn(string message)
            => this.Log(message, Plow.Logging.Category.Warn);

        [TestCaseSource(typeof(Source), nameof(Source.Messages))]
        public void Error(string message)
            => this.Log(message, Plow.Logging.Category.Error);

        [TestCaseSource(typeof(Source), nameof(Source.Messages))]
        public void Fatal(string message)
            => this.Log(message, Plow.Logging.Category.Fatal);

        private static class Source
        {
            private const string TAB = "\t";

            public static object[] Messages =
            {
                new[] { $"メッセージ出力テスト" },
                new[] {
                    @$"スタックトレース出力テスト{TAB}メッセージ{Environment.NewLine}" +
                    @$"  場所 Program.A() 場所 c:\test\program.cs:行 1{Environment.NewLine}" +
                    @$"  場所 Program.B() 場所 c:\test\program.cs:行 10{Environment.NewLine}" +
                    @$"  場所 Program.C(object c) 場所 c:\test\program.cs:行 100"
                },
            };
        }
    }
}
