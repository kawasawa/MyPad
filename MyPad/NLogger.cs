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

        /// <summary>
        /// 発行元のロガーの型を取得または設定します。
        /// </summary>
        public Type PublisherType { get; set; }

        /// <summary>
        /// ファクトリーの生成に使用される構成情報を取得または設定します。
        /// </summary>
        public Func<LoggingConfiguration> ConfigurationFactory { get; set; }

        /// <summary>
        /// ロガーのインスタンスを生成する処理をフックするためのメソッドを取得または設定します。
        /// </summary>
        public Action<Logger, Category> CreateLoggerHook { get; set; }

        /// <summary>
        /// ログが出力されるときに発生します。
        /// </summary>
        public event EventHandler<LogEventArgs> LogWriting;

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
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

        /// <summary>
        /// ログを出力します。
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="category">ログの種類</param>
        /// <param name="priority">ログの優先度</param>
        public void Log(string message, Category category, Priority priority = Priority.None)
        {
            var logger = category switch
            {
                Category.Trace => this.TraceLogger.Value,
                Category.Debug => this.DebugLogger.Value,
                Category.Info => this.InfoLogger.Value,
                Category.Warn => this.WarnLogger.Value,
                Category.Error => this.ErrorLogger.Value,
                Category.Fatal => this.FatalLogger.Value,
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

            var logInfo = new LogEventInfo(level, logger.Name, message);
            var e = new LogEventArgs(category, priority, logInfo);
            this.LogWriting?.Invoke(this, e);
            if (e.Cancel)
                return;

            logger.Log(this.PublisherType, logInfo);
        }

        /// <summary>
        /// ログの出力に使用されるロガーのインスタンスを生成します。
        /// </summary>
        /// <param name="category">ログの種類</param>
        /// <returns>NLog.Logger のロガーのインスタンス</returns>
        protected virtual Logger CreateLogger(Category category)
        {
            var logger = this.CreateLogFactory().GetLogger(nameof(NLogger));
            this.CreateLoggerHook?.Invoke(logger, category);
            return logger;
        }

        /// <summary>
        /// ロガーのインスタンスを生成するためのファクトリーを生成します。
        /// </summary>
        /// <returns>NLog.LogFactory ファクトリー</returns>
        protected virtual LogFactory CreateLogFactory()
        {
            if (this.ConfigurationFactory != null)
                return new LogFactory(this.ConfigurationFactory.Invoke());
            else
                return new LogFactory();
        }
    }
}
