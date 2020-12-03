using NLog;
using NLog.Config;
using Plow.Logging;
using System;

namespace MyPad
{
    /// <summary>
    /// <see cref="NLog"/> を使用してメッセージを出力するためのロガーを表します。
    /// </summary>
    public class NLogger : ILoggerFacade
    {
        private readonly Lazy<Logger> TraceLogger;
        private readonly Lazy<Logger> DebugLogger;
        private readonly Lazy<Logger> InfoLogger;
        private readonly Lazy<Logger> WarnLogger;
        private readonly Lazy<Logger> ErrorLogger;
        private readonly Lazy<Logger> FatalLogger;

        public Type PublisherType { get; set; }
        public Func<LoggingConfiguration> ConfigurationFactory { get; set; }
        public Action<Logger, Category> CreateLoggerHook { get; set; }
        public event EventHandler<LogEventArgs> LogWriting;

        public NLogger()
        {
            this.TraceLogger = new Lazy<Logger>(() => this.CreateLogger(Category.Trace));
            this.DebugLogger = new Lazy<Logger>(() => this.CreateLogger(Category.Debug));
            this.InfoLogger = new Lazy<Logger>(() => this.CreateLogger(Category.Info));
            this.WarnLogger = new Lazy<Logger>(() => this.CreateLogger(Category.Warn));
            this.ErrorLogger = new Lazy<Logger>(() => this.CreateLogger(Category.Error));
            this.FatalLogger = new Lazy<Logger>(() => this.CreateLogger(Category.Fatal));
            this.PublisherType = this.GetType();
        }

        public void Log(string message, Category category, Priority priority = Priority.None)
        {
            var logger = category switch
            {
                Category.Trace => this.TraceLogger.Value,
                Category.Debug => this.DebugLogger.Value,
                Category.Info => this.InfoLogger.Value,
                Category.Warn => this.WarnLogger.Value,
                Category.Error => this.ErrorLogger.Value,
                Category.Fatal => this.ErrorLogger.Value,
                _ => this.InfoLogger.Value,
            };
            var level = category switch
            {
                Category.Trace => LogLevel.Trace,
                Category.Debug => LogLevel.Debug,
                Category.Info => LogLevel.Info,
                Category.Warn => LogLevel.Warn,
                Category.Error => LogLevel.Error,
                Category.Fatal => LogLevel.Fatal,
                _ => LogLevel.Info,
            };

            var logInfo = new NLog.LogEventInfo(level, logger.Name, message);
            var e = new LogEventArgs(category, priority, logInfo);
            this.LogWriting?.Invoke(this, e);
            if (e.Cancel)
                return;

            logger.Log(this.PublisherType, logInfo);
        }

        protected virtual Logger CreateLogger(Category category)
        {
            var logger = this.CreateLogFactory().GetLogger(nameof(NLogger));
            this.CreateLoggerHook?.Invoke(logger, category);
            return logger;
        }

        protected virtual LogFactory CreateLogFactory()
        {
            if (this.ConfigurationFactory != null)
                return new LogFactory(this.ConfigurationFactory.Invoke());
            else
                return new LogFactory();
        }
    }
}
