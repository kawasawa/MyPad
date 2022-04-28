using MyBase.Logging;
using System;
using System.Reflection;
using System.Windows;

[module: MyPad.LogInterceptor]
namespace MyPad;

/// <summary>
/// メソッドの起動時、終了時にログを出力するインターセプターを表します。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
public class LogInterceptorAttribute : Attribute
{
    private static int GlobalSequence = 0;

    private int _sequence;
    private ILoggerFacade _logger;
    private MethodBase _method;

    /// <summary>
    /// インターセプターの処理に必要な初期設定を行います。
    /// </summary>
    /// <param name="instance">メソッドが定義されたオブジェクトのインスタンス</param>
    /// <param name="method">メソッドの情報</param>
    /// <param name="args">メソッドの引数</param>
    public void Init(object instance, MethodBase method, object[] args)
    {
        this._sequence = ++GlobalSequence;
        this._logger = ((App)Application.Current)?.Logger;
        this._method = method;
    }

    /// <summary>
    /// メソッドが起動するときに呼び出されます。
    /// </summary>
    public void OnEntry()
        => this._logger?.Log(this.CreateMessage("BEGIN"), Category.Trace);

    /// <summary>
    /// メソッドが正常終了した後に呼び出されます。
    /// </summary>
    public void OnExit()
        => this._logger?.Log(this.CreateMessage("END  "), Category.Trace);

    /// <summary>
    /// メソッドの実行中にハンドルされていない例外が発生したときに呼び出されます。
    /// </summary>
    /// <param name="e">例外の情報</param>
    public void OnException(Exception e)
        => this._logger?.Log(this.CreateMessage("ERROR"), Category.Trace);

    /// <summary>
    /// ログメッセージを生成します。
    /// </summary>
    /// <param name="kind">ログメッセージに付与する種別の文言</param>
    /// <returns>ログメッセージ</returns>
    private string CreateMessage(string kind)
        => $"{kind} #{this._sequence}: {this._method.DeclaringType?.Name ?? "<unknown>"}.{this._method.Name}";
}
