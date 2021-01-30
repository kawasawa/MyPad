using Microsoft.CodeAnalysis.CSharp.Scripting;
using Plow.Logging;
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
    public class ScriptRunnerViewModel : ViewModelBase
    {
        #region インジェクション

        [Dependency]
        public ILoggerFacade Logger { get; set; }

        #endregion

        #region プロパティ

        public ReactiveCollection<string> ScriptHistories { get; }
        public ReactiveCollection<string> ResultHistories { get; }

        public ReactiveProperty<bool> IsWorking { get; }
        public ReactiveProperty<string> Script { get; }
        public ReactiveProperty<string> OutputText { get; }

        public ReactiveCommand RunScriptCommand { get; }

        #endregion

        [InjectionConstructor]
        [LogInterceptor]
        public ScriptRunnerViewModel()
        {
            this.ScriptHistories = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
            this.ResultHistories = new ReactiveCollection<string>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.ScriptHistories, new object());
            BindingOperations.EnableCollectionSynchronization(this.ResultHistories, new object());

            this.IsWorking = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
            this.Script = new ReactiveProperty<string>().AddTo(this.CompositeDisposable);
            this.OutputText = this.ResultHistories.CollectionChangedAsObservable()
                .Select(_ => string.Join(Environment.NewLine, this.ResultHistories))
                .ToReactiveProperty()
                .AddTo(this.CompositeDisposable);

            this.RunScriptCommand = new ReactiveCommand()
                .WithSubscribe(() => this.RunScript())
                .AddTo(this.CompositeDisposable);
        }

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
            this.Logger.Debug($"スクリプトランナーでスクリプトを実行します。: Script={script}");

            var result = await this.EvaluateAsync(script);
            this.ResultHistories.Add($"> {script}");
            if (string.IsNullOrEmpty(result) == false)
                this.ResultHistories.Add(result);

            while (AppSettings.TerminalBufferSize < this.ResultHistories.Count)
                this.ResultHistories.RemoveAt(0);

            this.Script.Value = string.Empty;
        }

        [LogInterceptor]
        private async Task<string> EvaluateAsync(string script)
        {
            try
            {
                this.IsWorking.Value = true;
                var result = await CSharpScript.EvaluateAsync(script);
                return result?.ToString().Trim();
            }
            catch (Exception e)
            {
                return e.Message?.Trim();
            }
            finally
            {
                this.IsWorking.Value = false;
            }
        }
    }
}
