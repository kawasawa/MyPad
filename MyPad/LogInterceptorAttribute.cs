using Prism.Logging;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

[module: MyPad.LogInterceptor]
namespace MyPad
{
    /// <summary>
    /// メソッドの起動時、終了時にログを出力するインターセプターを表します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
    public class LogInterceptorAttribute : Attribute, IMethodInterceptor
    {
        private static int GlobalSequence = 0;

        private int _sequence;
        private ILoggerFacade _logger;
        private Process _process;
        private MethodBase _method;

        public void Init(object instance, MethodBase method, object[] args)
        {
            this._sequence = ++GlobalSequence;
            this._logger = ((App)Application.Current).Logger;
            this._process = ((App)Application.Current).SharedDataService.Process;
            this._method = method;
        }

        public void OnEntry()
            => this._logger.Log(this.GetMessage("BEGIN"), Category.Debug);

        public void OnExit()
            => this._logger.Log(this.GetMessage("END  "), Category.Debug);

        public void OnException(Exception e)
            => this._logger.Log(this.GetMessage("ERROR"), Category.Debug);

        private string GetMessage(string kind)
            => $"[{string.Format("{0,5}", this._process.Id)}] [{string.Format("{0,5}", this._sequence)}] {kind}: {this._method.DeclaringType?.Name ?? "<unknown>"}.{this._method.Name}";
    }
}