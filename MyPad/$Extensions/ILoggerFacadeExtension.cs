using MyBase.Logging;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MyPad;

/// <summary>
/// <see cref="ILoggerFacade"/> インターフェースの拡張メソッドを提供します。
/// </summary>
public static class ILoggerFacadeExtension
{
    /// <summary>
    /// ログを出力します。
    /// </summary>
    /// <param name="self"><see cref="ILoggerFacade"/> インターフェースを実装するインスタンス</param>
    /// <param name="message">メッセージ</param>
    /// <param name="category">ログの種類</param>
    public static void Log(this ILoggerFacade self, string message, Category category)
        => self.Log(message, category, Priority.None);

    /// <summary>
    /// ログを出力します。
    /// </summary>
    /// <param name="self"><see cref="ILoggerFacade"/> インターフェースを実装するインスタンス</param>
    /// <param name="message">メッセージ</param>
    /// <param name="category">ログの種類</param>
    /// <param name="exception">例外の情報</param>
    public static void Log(this ILoggerFacade self, string message, Category category, Exception exception)
        => self.Log($"{message}{Environment.NewLine}{string.Join(Environment.NewLine, exception.ToString().Split(Environment.NewLine).Select(s => $"\t{s}"))}", category);

    /// <summary>
    /// ログを出力します。
    /// </summary>
    /// <param name="self"><see cref="ILoggerFacade"/> インターフェースを実装するインスタンス</param>
    /// <param name="message">メッセージ</param>
    /// <param name="callerFilePath">呼び出し元のファイルパス</param>
    /// <param name="callerMemberName">呼び出し元のメンバー名</param>
    /// <param name="callerLineNumber">呼び出し元の行数</param>
    public static void Debug(this ILoggerFacade self, string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int? callerLineNumber = null)
        => self.Log($"[{callerFilePath}, {callerMemberName}, {callerLineNumber?.ToString() ?? "<unknown>"}] {message}", Category.Debug);
}
