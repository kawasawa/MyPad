using MyBase.Wpf.CommonDialogs;
using MyPad.Models;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using Unity;

namespace MyPad.ViewModels.Dialogs
{
    public class SelectDiffFilesDialogViewModel : DialogViewModelBase
    {
        [Dependency]
        public ICommonDialogService CommonDialogService { get; set; }
        [Dependency]
        public Settings Settings { get; set; }
        [Dependency]
        public SyntaxService SyntaxService { get; set; }

        public ReactiveCollection<string> FileNames { get; }
        [Required]
        public ReactiveProperty<string> DiffSourcePath { get; }
        [Required]
        public ReactiveProperty<string> DiffDestinationPath { get; }
        public ReactiveCommand OKCommand { get; }
        public ReactiveCommand CancelCommand { get; }

        [InjectionConstructor]
        [LogInterceptor]
        public SelectDiffFilesDialogViewModel()
        {
            this.FileNames = new ReactiveCollection<string>()
                .AddTo(this.CompositeDisposable);
            this.DiffSourcePath = new ReactiveProperty<string>()
                .SetValidateAttribute(() => this.DiffSourcePath)
                .AddTo(this.CompositeDisposable);
            this.DiffDestinationPath = new ReactiveProperty<string>()
                .SetValidateAttribute(() => this.DiffDestinationPath)
                .AddTo(this.CompositeDisposable);

            this.OKCommand = Observable.Merge<object>(this.DiffSourcePath, this.DiffDestinationPath)
                .Select(_ => string.IsNullOrEmpty(this.DiffSourcePath.Value) == false &&
                             string.IsNullOrEmpty(this.DiffDestinationPath.Value) == false)
                .ToReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(
                    new DialogResult(ButtonResult.OK, new DialogParameters {
                        { nameof(this.DiffSourcePath), this.DiffSourcePath.Value },
                        { nameof(this.DiffDestinationPath), this.DiffDestinationPath.Value },
                    })))
                .AddTo(this.CompositeDisposable);

            this.CancelCommand = new ReactiveCommand()
                .WithSubscribe(() => this.OnRequestClose(new DialogResult(ButtonResult.Cancel)))
                .AddTo(this.CompositeDisposable);
        }

        [LogInterceptor]
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);
            if (parameters.GetValue<IEnumerable<string>>(nameof(this.FileNames)) is IEnumerable<string> fileNames)
                this.FileNames.AddRange(fileNames);
            if (parameters.GetValue<string>(nameof(this.DiffSourcePath)) is string diffSourcePath)
                this.DiffSourcePath.Value = diffSourcePath;
            if (parameters.GetValue<string>(nameof(this.DiffDestinationPath)) is string diffDestinationPath)
                this.DiffDestinationPath.Value = diffDestinationPath;
        }
    }
}
