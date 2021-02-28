using MyBase.Logging;
using NLog;
using NLog.Config;
using System;

namespace MyPad
{
    /// <summary>
    /// <see cref="NLog"/> を使用してメッセージを出力するためのロガーを表します。
    /// </summary>
    public class NLogger : ILoggerFacade
    {
        public readonly Lazy<ILogger> TraceCoreLogger;
        public readonly Lazy<ILogger> DebugCoreLogger;
        public readonly Lazy<ILogger> InfoCoreLogger;
        public readonly Lazy<ILogger> WarnCoreLogger;
        public readonly Lazy<ILogger> ErrorCoreLogger;
        public readonly Lazy<ILogger> FatalCoreLogger;

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
        public Action<ILogger, Category> CreateLoggerHook { get; set; }

        /// <summary>
        /// ログが出力されるときに発生します。
        /// </summary>
        public event EventHandler<LogEventArgs> LogWriting;

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        public NLogger()
        {
            this.TraceCoreLogger = new Lazy<ILogger>(() => this.CreateLogger(Category.Trace));
            this.DebugCoreLogger = new Lazy<ILogger>(() => this.CreateLogger(Category.Debug));
            this.InfoCoreLogger = new Lazy<ILogger>(() => this.CreateLogger(Category.Info));
            this.WarnCoreLogger = new Lazy<ILogger>(() => this.CreateLogger(Category.Warn));
            this.ErrorCoreLogger = new Lazy<ILogger>(() => this.CreateLogger(Category.Error));
            this.FatalCoreLogger = new Lazy<ILogger>(() => this.CreateLogger(Category.Fatal));
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
                Category.Trace => this.TraceCoreLogger.Value,
                Category.Debug => this.DebugCoreLogger.Value,
                Category.Info => this.InfoCoreLogger.Value,
                Category.Warn => this.WarnCoreLogger.Value,
                Category.Error => this.ErrorCoreLogger.Value,
                Category.Fatal => this.FatalCoreLogger.Value,
                _ => this.InfoCoreLogger.Value,
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
        /// <returns>NLog.ILogger を実装したインスタンス</returns>
        protected virtual ILogger CreateLogger(Category category)
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
