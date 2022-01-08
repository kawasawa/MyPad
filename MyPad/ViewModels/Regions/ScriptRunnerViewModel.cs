using Microsoft.CodeAnalysis.CSharp.Scripting;
using MyBase.Logging;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Unity;

namespace MyPad.ViewModels.Regions
{
    /// <summary>
    /// <see cref="Views.Regions.ScriptRunnerView"/> に対応する ViewModel を表します。
    /// </summary>
    public class ScriptRunnerViewModel : RegionViewModelBase
    {
        [Dependency]
        public ILoggerFacade Logger { get; set; }

        public ReactiveCollection<string> ScriptHistories { get; }
        public ReactiveCollection<string> ResultHistories { get; }

        public ReactiveProperty<bool> IsPending { get; }
        public ReactiveProperty<string> Script { get; }
        public ReactiveProperty<string> OutputText { get; }

        public ReactiveCommand RunScriptCommand { get; }

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        [InjectionConstructor]
        [LogInterceptor]
        public ScriptRunnerViewModel()
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

            this.RunScriptCommand = new ReactiveCommand()
                .WithSubscribe(() => this.RunScript())
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// スクリプトを実行します。
        /// </summary>
        [LogInterceptor]
        private async void RunScript()
        {
            var script = this.Script.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(script) == false)
            {
                var i = this.ScriptHistories.IndexOf(script);
                if (0 <= i)
                    this.ScriptHistories.Move(i, 0);
                else
                    this.ScriptHistories.Insert(0, script);
            }
            this.Logger.Log($"スクリプトランナーでスクリプトを実行します。: Script={script}", Category.Info);

            var result = await this.EvaluateAsync(script);
            this.ResultHistories.Add($"> {script}");
            if (string.IsNullOrEmpty(result) == false)
                this.ResultHistories.Add(result);

            while (AppSettingsReader.TerminalBufferSize < this.ResultHistories.Count)
                this.ResultHistories.RemoveAt(0);

            this.Script.Value = string.Empty;
        }

        /// <summary>
        /// C＃スクリプトを実行します。
        /// </summary>
        /// <param name="script">C# のスクリプト</param>
        /// <returns>スクリプトの実行結果</returns>
        [LogInterceptor]
        private async Task<string> EvaluateAsync(string script)
        {
            try
            {
                this.IsPending.Value = true;
                var result = await CSharpScript.EvaluateAsync(script);
                return result?.ToString().Trim();
            }
            catch (Exception e)
            {
                return e.Message?.Trim();
            }
            finally
            {
                this.IsPending.Value = false;
            }
        }
    }
}
