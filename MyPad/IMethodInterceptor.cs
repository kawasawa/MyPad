using System;
using System.Reflection;

namespace MyPad
{
    /// <summary>
    /// メソッドの起動と終了、例外の発生を捉えるためのインターフェースを表します。
    /// </summary>
    public interface IMethodInterceptor
    {
        /// <summary>
        /// インターセプターの処理に必要な初期設定を行います。
        /// </summary>
        /// <param name="instance">メソッドが定義されたオブジェクトのインスタンス</param>
        /// <param name="method">メソッドの情報</param>
        /// <param name="args">メソッドの引数</param>
        void Init(object instance, MethodBase method, object[] args);

        /// <summary>
        /// メソッドが起動するときに呼び出されます。
        /// </summary>
        void OnEntry();

        /// <summary>
        /// メソッドが正常終了した後に呼び出されます。
        /// </summary>
        void OnExit();

        /// <summary>
        /// メソッドの実行中にハンドルされていない例外が発生したときに呼び出されます。
        /// </summary>
        /// <param name="e">例外の情報</param>
        void OnException(Exception e);
    }
}