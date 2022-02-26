using MyBase.Logging;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using Unity;

namespace MyPad.ViewModels.Regions;

/// <summary>
/// <see cref="Views.Regions.TerminalView"/> に対応する ViewModel を表します。
/// </summary>
public class TerminalViewModel : RegionViewModelBase
{
    [Dependency]
    public ILoggerFacade Logger { get; set; }

    private const string COMSPEC = "COMSPEC";

    private Process _terminalProcess;
    private StreamWriter _terminalInput;
    private bool _terminalClosing;

    public ReactiveCollection<string> ScriptHistories { get; }
    public ReactiveCollection<string> ResultHistories { get; }

    public ReactiveProperty<bool> IsPending { get; }
    public ReactiveProperty<string> Script { get; }
    public ReactiveProperty<string> OutputText { get; }
    public ReactiveProperty<string> OutputLastLine { get; }

    public ReactiveCommand RunScriptCommand { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public TerminalViewModel()
    {
        this.ScriptHistories = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
        this.ResultHistories = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
        BindingOperations.EnableCollectionSynchronization(this.ScriptHistories, new object());
        BindingOperations.EnableCollectionSynchronization(this.ResultHistories, new object());

        this.IsPending = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
        this.Script = new ReactiveProperty<string>().AddTo(this.CompositeDisposable);
        this.OutputText = this.ResultHistories.CollectionChangedAsObservable()
            .Select(_ => string.Join(Environment.NewLine, this.ResultHistories))
            .ToReactiveProperty()
            .AddTo(this.CompositeDisposable);
        this.OutputLastLine = new ReactiveProperty<string>().AddTo(this.CompositeDisposable);

        this.RunScriptCommand = new ReactiveCommand()
            .WithSubscribe(() => this.RunScript())
            .AddTo(this.CompositeDisposable);

        this.StartTerminalProcess();
    }

    /// <summary>
    /// このインスタンスが保持するリソースを解放します。
    /// </summary>
    /// <param name="disposing">マネージリソースを破棄するかどうかを示す値</param>
    [LogInterceptor]
    protected override void Dispose(bool disposing)
    {
        if (this.IsDisposed || this._terminalClosing)
            return;

        this.CloseTerminalProcess();
        base.Dispose(disposing);
    }

    /// <summary>
    /// プロセスが終了した後に行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Terminal_Exited(object sender, EventArgs e)
    {
        this.CloseTerminalProcess();
        this.StartTerminalProcess();
    }

    /// <summary>
    /// ターミナルのプロセスを起動します。
    /// </summary>
    [LogInterceptor]
    private void StartTerminalProcess()
    {
        this._terminalProcess = new();
        this._terminalProcess.StartInfo.FileName = Environment.GetEnvironmentVariable(COMSPEC);
        this._terminalProcess.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        this._terminalProcess.StartInfo.CreateNoWindow = true;
        this._terminalProcess.StartInfo.UseShellExecute = false;
        this._terminalProcess.StartInfo.RedirectStandardInput = true;
        this._terminalProcess.StartInfo.RedirectStandardOutput = true;
        this._terminalProcess.StartInfo.RedirectStandardError = true;
        this._terminalProcess.EnableRaisingEvents = true;
        this._terminalProcess.OutputDataReceived += this.Terminal_DataReceived;
        this._terminalProcess.ErrorDataReceived += this.Terminal_DataReceived;
        this._terminalProcess.Exited += this.Terminal_Exited;

        this._terminalProcess.Start();
        this._terminalProcess.BeginOutputReadLine();
        this._terminalProcess.BeginErrorReadLine();
        this._terminalInput = this._terminalProcess.StandardInput;
    }

    /// <summary>
    /// ターミナルのプロセスを終了します。
    /// </summary>
    [LogInterceptor]
    private void CloseTerminalProcess()
    {
        if (this._terminalProcess == null)
            return;

        this._terminalProcess.CancelOutputRead();
        this._terminalProcess.CancelErrorRead();

        this._terminalProcess.OutputDataReceived -= this.Terminal_DataReceived;
        this._terminalProcess.ErrorDataReceived -= this.Terminal_DataReceived;
        this._terminalProcess.Exited -= this.Terminal_Exited;

        if (this._terminalProcess.HasExited == false)
        {
            this._terminalClosing = true;
            this._terminalInput?.Close();
            this._terminalProcess.WaitForExit();
            this._terminalProcess.Close();
            this._terminalClosing = false;
        }

        this._terminalProcess = null;
    }

    /// <summary>
    /// スクリプトを実行します。
    /// </summary>
    [LogInterceptor]
    private void RunScript()
    {
        if (this._terminalInput.BaseStream.CanWrite == false)
            return;

        var script = this.Script.Value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(script) == false)
        {
            var i = this.ScriptHistories.IndexOf(script);
            if (0 <= i)
                this.ScriptHistories.Move(i, 0);
            else
                this.ScriptHistories.Insert(0, script);
        }
        this.Logger.Log($"ターミナルでスクリプトを実行します。: Script={script}", Category.Info);

        // DataReceived の際にカレントディレクトリの表示が重複してしまうため削除しておく
        this.OutputLastLine.Value = null;
        // 改行コードを付けないとターミナルの出力結果の最終行を取得できない
        this._terminalInput.WriteLine(script + Environment.NewLine);

        this.Script.Value = string.Empty;
    }

    /// <summary>
    /// プロセスのストリームに行が書き込まれたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptorIgnore]
    private void Terminal_DataReceived(object sender, DataReceivedEventArgs e)
    {
        if (this.OutputLastLine.Value != null)
            this.ResultHistories.Add(this.OutputLastLine.Value);
        this.OutputLastLine.Value = e.Data;

        // ターミナルの初期化時にカレントディレクトリを表示する
        if (this.ScriptHistories.Any() == false && string.IsNullOrEmpty(e.Data))
        {
            this.ResultHistories.Add(e.Data);
            this.OutputLastLine.Value = $"{this._terminalProcess.StartInfo.WorkingDirectory}>";
        }

        while (AppSettingsReader.TerminalLineLimit < this.ResultHistories.Count)
            this.ResultHistories.RemoveAt(0);
    }
}
