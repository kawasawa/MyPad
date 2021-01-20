using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${longdate}"));
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
                    config.Variables.Add("DIR", this.LogDirectoryPath);
                    return config;
                },
                CreateLoggerHook = (logger, category) =>
                {
                    logger.Factory.Configuration.Variables.Add("CTG", category.ToString());
                    logger.Factory.ReconfigExistingLoggers();
                },
            };
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

        [TestCaseSource(typeof(Source), nameof(Source.Messages))]
        public async Task StressTest(string message)
        {
            IEnumerable<Task> createTasks(int taskCount, int lineCount, Plow.Logging.Category category)
            {
                for (var i = 1; i <= taskCount; i++)
                {
                    var taskNumber = i;
                    yield return Task.Run(() =>
                    {
                        for (var j = 1; j <= lineCount; j++)
                            this.Logger.Log($"[Task {taskNumber}: {string.Format("{0," + lineCount.ToString().Length + "}", j)}] {message}", category);
                    });
                }
            }

            const int TASK_COUNT = 10;
            const int LINE_COUNT = 1000;
            const Plow.Logging.Category CATEGORY = Plow.Logging.Category.Trace;

            await Task.WhenAll(createTasks(TASK_COUNT, LINE_COUNT, CATEGORY));

            var path = Path.Combine(this.LogDirectoryPath, $"{CATEGORY}.log");
            Assert.That(File.Exists(path), Is.EqualTo(true));

            var logLineCount = File.ReadLines(path).Count();
            var messageLineCount = (message.Length - message.Replace(Environment.NewLine, string.Empty).Length) / Environment.NewLine.Length + 1;
            Assert.That(logLineCount, Is.EqualTo(TASK_COUNT * LINE_COUNT * messageLineCount));
        }

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
