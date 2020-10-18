﻿using Dragablz;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Livet.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MyPad.Models;
using MyPad.Properties;
using MyPad.ViewModels.Events;
using Plow;
using Plow.Wpf.CommonDialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Logging;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using Unity;

namespace MyPad.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region インジェクション

        // Constructor Injection
        public IEventAggregator EventAggregator { get; set; }

        // Dependency Injection
        private ILoggerFacade _logger;
        private IProductInfo _productInfo;
        private SettingsService _settingsService;
        private SyntaxService _syntaxService;
        [Dependency]
        public IContainerExtension ContainerExtension { get; set; }
        [Dependency]
        public IDialogService DialogService { get; set; }
        [Dependency]
        public ICommonDialogService CommonDialogService { get; set; }
        [Dependency]
        public ILoggerFacade Logger
        {
            get => this._logger;
            set => this.SetProperty(ref this._logger, value);
        }
        [Dependency]
        public IProductInfo ProductInfo
        {
            get => this._productInfo;
            set => this.SetProperty(ref this._productInfo, value);
        }
        [Dependency]
        public SettingsService SettingsService
        {
            get => this._settingsService;
            set => this.SetProperty(ref this._settingsService, value);
        }
        [Dependency]
        public SyntaxService SyntaxService
        {
            get => this._syntaxService;
            set => this.SetProperty(ref this._syntaxService, value);
        }

        #endregion

        #region プロパティ

        public InteractionMessenger Messenger { get; }

        public ReactiveProperty<bool> IsWorking { get; }
        public ReactiveProperty<TextEditorViewModel> ActiveTextEditor { get; }
        public ReactiveProperty<FlowDocument> FlowDocument { get; }
        public ReactiveProperty<TextEditorViewModel> DiffSource { get; }
        public ReactiveProperty<TextEditorViewModel> DiffDestination { get; }
        public ReactiveProperty<bool> IsOpenDiffContent { get; }
        public ReactiveProperty<bool> IsOpenPrintPreviewContent { get; }
        public ReactiveProperty<bool> IsOpenOptionContent { get; }
        public ReactiveProperty<bool> IsOpenAboutContent { get; }

        public ReactiveCollection<TextEditorViewModel> TextEditors { get; }
        public ReactiveCollection<FileTreeNodeViewModel> FileTreeNodes { get; }

        public ReactiveCommand NewCommand { get; }
        public ReactiveCommand NewWindowCommand { get; }
        public ReactiveCommand<IEnumerable<string>> LoadCommand { get; }
        public ReactiveCommand OpenCommand { get; }
        public ReactiveCommand SaveCommand { get; }
        public ReactiveCommand SaveAsCommand { get; }
        public ReactiveCommand SaveAllCommand { get; }
        public ReactiveCommand ExitCommand { get; }
        public ReactiveCommand ExitApplicationCommand { get; }
        public ReactiveCommand CloseCommand { get; }
        public ReactiveCommand CloseAllCommand { get; }
        public ReactiveCommand CloseOtherCommand { get; }
        public ReactiveCommand DiffCommand { get; }
        public ReactiveCommand DiffUnmodifiedCommand { get; }
        public ReactiveCommand PropertyCommand { get; }
        public ReactiveCommand PrintCommand { get; }
        public ReactiveCommand PrintPreviewCommand { get; }
        public ReactiveCommand OptionCommand { get; }
        public ReactiveCommand AboutCommand { get; }
        public ReactiveCommand GoToLineCommand { get; }
        public ReactiveCommand ChangeEncodingCommand { get; }
        public ReactiveCommand ChangeSyntaxCommand { get; }

        public ReactiveCommand<DragEventArgs> DropHandler { get; }
        public ReactiveCommand<EventArgs> ContentRenderedHandler { get; }
        public ReactiveCommand<CancelEventArgs> ClosingHandler { get; }

        public Func<TextEditorViewModel> TextEditorFactory =>
            () => this.ContainerExtension.Resolve<TextEditorViewModel>();
        public Delegate ClosingTextEditorHandler
           => new ItemActionCallback(async e =>
           {
               if (e.IsCancelled || !(e.DragablzItem?.DataContext is TextEditorViewModel textEditor))
                   return;

               e.Cancel();
               await this.TryCloseTextEditor(textEditor);
               if (this.TextEditors.Any() == false)
                   this.NewCommand.Execute();
           });

        #endregion

        [InjectionConstructor]
        [LogInterceptor]
        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
            // ----- インジェクション ------------------------------

            this.EventAggregator = eventAggregator;

            // ----- プロパティの定義 ------------------------------

            this.Messenger = new InteractionMessenger();

            this.IsWorking = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
            this.ActiveTextEditor = new ReactiveProperty<TextEditorViewModel>().AddTo(this.CompositeDisposable);
            this.FlowDocument = new ReactiveProperty<FlowDocument>().AddTo(this.CompositeDisposable);
            this.DiffSource = new ReactiveProperty<TextEditorViewModel>().AddTo(this.CompositeDisposable);
            this.DiffDestination = new ReactiveProperty<TextEditorViewModel>().AddTo(this.CompositeDisposable);
            this.IsOpenDiffContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
            this.IsOpenPrintPreviewContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
            this.IsOpenOptionContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
            this.IsOpenAboutContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
            var compositeFlyout = new[] {
                this.IsOpenDiffContent,
                this.IsOpenPrintPreviewContent,
                this.IsOpenOptionContent,
                this.IsOpenAboutContent
            };

            this.TextEditors = new ReactiveCollection<TextEditorViewModel>().AddTo(this.CompositeDisposable);
            this.FileTreeNodes = new ReactiveCollection<FileTreeNodeViewModel>().AddTo(this.CompositeDisposable);
            BindingOperations.EnableCollectionSynchronization(this.TextEditors, new object());
            BindingOperations.EnableCollectionSynchronization(this.FileTreeNodes, new object());

            // ----- 変更通知の購読 ------------------------------

            this.IsOpenDiffContent
                .Where(isOpen => isOpen)
                .Subscribe(_ => compositeFlyout.Except(new[] { this.IsOpenDiffContent }).ForEach(p => p.Value = false))
                .AddTo(this.CompositeDisposable);

            this.IsOpenPrintPreviewContent
                .Where(isOpen => isOpen)
                .Subscribe(_ => compositeFlyout.Except(new[] { this.IsOpenPrintPreviewContent }).ForEach(p => p.Value = false))
                .AddTo(this.CompositeDisposable);

            this.IsOpenOptionContent
                .Where(isOpen => isOpen)
                .Subscribe(_ => compositeFlyout.Except(new[] { this.IsOpenOptionContent }).ForEach(p => p.Value = false))
                .AddTo(this.CompositeDisposable);

            this.IsOpenAboutContent
                .Where(isOpen => isOpen)
                .Subscribe(_ => compositeFlyout.Except(new[] { this.IsOpenAboutContent }).ForEach(p => p.Value = false))
                .AddTo(this.CompositeDisposable);

            this.IsOpenDiffContent
                .Inverse()
                .Where(isClose => isClose)
                .Subscribe(_ =>
                {
                    this.DiffSource.Value = null;
                    this.DiffDestination.Value = null;
                })
                .AddTo(this.CompositeDisposable);

            this.IsOpenPrintPreviewContent
                .Subscribe(async isOpen => this.FlowDocument.Value = isOpen ? await this.ActiveTextEditor.Value.CreateFlowDocument() : null)
                .AddTo(this.CompositeDisposable);

            // ----- コマンドの定義 ------------------------------

            this.NewCommand = new ReactiveCommand()
                .WithSubscribe(() => this.WakeUpTextEditor(this.AddTextEditor()))
                .AddTo(this.CompositeDisposable);

            this.NewWindowCommand = new ReactiveCommand()
                .WithSubscribe(() => this.EventAggregator.GetEvent<CreateWindowEvent>().Publish())
                .AddTo(this.CompositeDisposable);

            this.LoadCommand = new ReactiveCommand<IEnumerable<string>>()
                .WithSubscribe(async paths =>
                {
                    var results = await this.LoadTextEditor(paths);
                    this.WakeUpTextEditor(results.LastOrDefault(tuple => tuple.textEditor != null).textEditor);
                })
                .AddTo(this.CompositeDisposable);

            this.OpenCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    var results = await this.LoadTextEditor();
                    this.WakeUpTextEditor(results.LastOrDefault(tuple => tuple.textEditor != null).textEditor);
                })
                .AddTo(this.CompositeDisposable);

            this.SaveCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    if (this.ActiveTextEditor.Value.IsModified)
                        await this.SaveTextEditor(this.ActiveTextEditor.Value);
                })
                .AddTo(this.CompositeDisposable);

            this.SaveAsCommand = new ReactiveCommand()
                .WithSubscribe(async () => await this.SaveAsTextEditor(this.ActiveTextEditor.Value))
                .AddTo(this.CompositeDisposable);

            this.SaveAllCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    foreach (var target in this.TextEditors.Where(e => e.IsReadOnly == false))
                    {
                        this.WakeUpTextEditor(target);
                        var (result, _) = await this.SaveTextEditor(target);
                        if (result == false)
                            return;
                    }
                })
                .AddTo(this.CompositeDisposable);

            this.ExitCommand = new ReactiveCommand()
                .WithSubscribe(async () => await this.InvokeExit())
                .AddTo(this.CompositeDisposable);

            this.ExitApplicationCommand = new ReactiveCommand()
                .WithSubscribe(() => this.EventAggregator.GetEvent<ExitApplicationEvent>().Publish())
                .AddTo(this.CompositeDisposable);

            this.CloseCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    await this.TryCloseTextEditor(this.ActiveTextEditor.Value);
                    if (this.TextEditors.Any() == false)
                        this.NewCommand.Execute();
                })
                .AddTo(this.CompositeDisposable);

            this.CloseAllCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    for (var i = this.TextEditors.Count - 1; 0 <= i; i--)
                    {
                        var target = this.TextEditors[i];
                        this.WakeUpTextEditor(target);
                        if (await this.TryCloseTextEditor(target) == false)
                            return;
                    }
                    if (this.TextEditors.Any() == false)
                        this.NewCommand.Execute();
                })
                .AddTo(this.CompositeDisposable);

            this.CloseOtherCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    var current = this.ActiveTextEditor.Value;
                    for (var i = this.TextEditors.Count - 1; 0 <= i; i--)
                    {
                        var target = this.TextEditors[i];
                        if (target == current)
                            continue;

                        this.WakeUpTextEditor(target);
                        if (await this.TryCloseTextEditor(target) == false)
                            return;
                    }
                    this.WakeUpTextEditor(current);
                })
                .AddTo(this.CompositeDisposable);

            this.DiffCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    IEnumerable<(TextEditorViewModel textEditors, Views.MainWindow window)> getTextEditors()
                    {
                        foreach (var v in this.GetViews())
                            foreach (var e in ((MainWindowViewModel)v.DataContext).TextEditors)
                                yield return (e, v);
                    }

                    var textEditors = getTextEditors().Select(tuple => tuple.textEditors);
                    var (result, diffSourcePath, diffDestinationPath) = await this.DialogService.SelectDiffFiles(textEditors.Select(e => e.FileName), this.ActiveTextEditor.Value.FileName);
                    if (result == false)
                        return;

                    try
                    {
                        this.DiffSource.Value = textEditors.First(e => e.FileName == diffSourcePath);
                        this.DiffDestination.Value = textEditors.First(e => e.FileName == diffDestinationPath);
                        this.Logger.Log($"差分を比較するファイルを読み込みました。: SourcePath={diffSourcePath}, DestinationPath={diffDestinationPath}", Category.Info);
                    }
                    catch (Exception e)
                    {
                        this.DiffSource.Value = null;
                        this.DiffDestination.Value = null;

                        this.Logger.Log($"差分を比較するファイルの読み込みに失敗しました。: SourcePath={diffSourcePath}, DestinationPath={diffDestinationPath}", Category.Warn, e);
                        this.DialogService.Warn(e.Message);
                        return;
                    }

                    this.IsOpenDiffContent.Value = true;
                })
                .AddTo(this.CompositeDisposable);

            this.DiffUnmodifiedCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    this.DiffSource.Value = await this.ActiveTextEditor.Value.CloneUnmodified();
                    this.DiffDestination.Value = this.ActiveTextEditor.Value;
                    this.IsOpenDiffContent.Value = true;
                })
                .AddTo(this.CompositeDisposable);

            this.PrintCommand = this.FlowDocument.IsEmpty().Inverse().ToReactiveCommand()
                .WithSubscribe(async () =>
                {
                    var result = this.CommonDialogService.ShowDialog(
                        new PrintDialogParameters()
                        {
                            Title = this.ProductInfo.Description,
                            FlowDocument = this.FlowDocument.Value ?? await this.ActiveTextEditor.Value.CreateFlowDocument()
                        });
                    if (result)
                        this.Logger.Log($"ファイルを印刷しました。(OSやハードウェアの要因でキャンセルされた可能性もあります)", Category.Info);
                    this.IsOpenPrintPreviewContent.Value = false;
                })
                .AddTo(this.CompositeDisposable);

            this.PrintPreviewCommand = new ReactiveCommand()
                .WithSubscribe(() => this.IsOpenPrintPreviewContent.Value = true)
                .AddTo(this.CompositeDisposable);

            this.OptionCommand = new ReactiveCommand()
                .WithSubscribe(() => this.IsOpenOptionContent.Value = true)
                .AddTo(this.CompositeDisposable);

            this.AboutCommand = new ReactiveCommand()
                .WithSubscribe(() => this.IsOpenAboutContent.Value = true)
                .AddTo(this.CompositeDisposable);

            this.GoToLineCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    var target = this.ActiveTextEditor.Value;
                    if (await this.DialogService.ChangeLine(target) is (true, var line))
                    {
                        target.Line = line;
                        await this.Messenger.RaiseAsync(new InteractionMessage(nameof(Views.MainWindow.ScrollToCaret)));
                    }
                })
                .AddTo(this.CompositeDisposable);

            this.ChangeEncodingCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    var target = this.ActiveTextEditor.Value;
                    var (result, encoding) = await this.DialogService.ChangeEncoding(target);
                    if (result == false)
                        return;

                    if (target.IsNewFile)
                        target.Encoding = encoding;
                    else
                        await this.ReadFile(target.FileName, encoding, target.SyntaxDefinition, target.IsReadOnly);
                })
                .AddTo(this.CompositeDisposable);

            this.ChangeSyntaxCommand = new ReactiveCommand()
                .WithSubscribe(async () =>
                {
                    var target = this.ActiveTextEditor.Value;
                    var (result, syntax) = await this.DialogService.ChangeSyntax(target);
                    if (result == false)
                        return;

                    var definition = string.IsNullOrEmpty(syntax) ? null :
                        this.SyntaxService.Definitions.ContainsKey(syntax) ? this.SyntaxService.Definitions[syntax] : null;
                    target.SyntaxDefinition = definition;
                })
                .AddTo(this.CompositeDisposable);

            this.DropHandler = new ReactiveCommand<DragEventArgs>()
                .WithSubscribe(e =>
                {
                    if (e.Data.GetData(DataFormats.FileDrop) is IEnumerable<string> paths && paths.Any())
                    {
                        this.LoadCommand.Execute(paths);
                        e.Handled = true;
                    }
                })
                .AddTo(this.CompositeDisposable);

            this.ContentRenderedHandler = new ReactiveCommand<EventArgs>()
                .WithSubscribe(e =>
                {
                    this.RefreshExplorer();
                })
                .AddTo(this.CompositeDisposable);

            this.ClosingHandler = new ReactiveCommand<CancelEventArgs>()
                .WithSubscribe(e =>
                {
                    // NOTE: Closing イベント内で非同期処理後にイベントをキャンセルできなくなる問題 (ViewModel)
                    // 最初にイベントはキャンセルしてから非同期処理を行う。
                    // 閉じる条件を満たした場合は Dispose メソッドを実行する。
                    // (ViewModel の Dispose をトリガーに、View が Close メソッドを実行する。)
                    e.Cancel = true;
                    this.ExitCommand.Execute();
                })
                .AddTo(this.CompositeDisposable);

            // ----- PUB/SUB メッセージ ------------------------------

            void refreshExplorer() => this.RefreshExplorer();
            this.EventAggregator.GetEvent<RefreshExplorerEvent>().Subscribe(refreshExplorer);
        }

        [LogInterceptor]
        public async Task<bool> InvokeExit()
        {
            for (var i = this.TextEditors.Count - 1; 0 <= i; i--)
            {
                var target = this.TextEditors[i];
                this.WakeUpTextEditor(target);
                if (await this.TryCloseTextEditor(target) == false)
                    return false;
            }

            this.Dispose();
            return true;
        }

        [LogInterceptor]
        private void RefreshExplorer()
        {
            var roots = Enumerable.Empty<string>();
            if (this.SettingsService.OtherTools?.ExplorerRoots?.Any() == true)
                roots = this.SettingsService.OtherTools.ExplorerRoots.Where(i => i.IsEnabled).Select(i => i.Path);
            else
                roots = new[] { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
            var isExpanded = roots.Count() == 1;
            var isSelected = true;

            this.FileTreeNodes.ClearOnScheduler();
            this.FileTreeNodes.AddRangeOnScheduler(
                roots.Select(r =>
                {
                    var node = this.ContainerExtension.Resolve<FileTreeNodeViewModel>().Initialize(r, isSelected, isExpanded);
                    isSelected = false;
                    return node;
                }));
        }

        #region テキストエディターの制御

        [LogInterceptor]
        private TextEditorViewModel AddTextEditor()
        {
            var textEditor = this.TextEditorFactory.Invoke();
            this.TextEditors.Add(textEditor);
            return textEditor;
        }

        [LogInterceptor]
        private void RemoveTextEditor(TextEditorViewModel textEditor)
        {
            if (this.TextEditors.Contains(textEditor) == false)
                return;

            this.TextEditors.Remove(textEditor);
            textEditor.Dispose();
        }

        [LogInterceptor]
        private async Task<bool> TryCloseTextEditor(TextEditorViewModel textEditor)
        {
            if (textEditor.IsModified)
            {
                var result = this.DialogService.CancelableConfirm($"{Resources.Message_ConfirmSaveChanges}{Environment.NewLine}{textEditor.FileName}");
                switch (result)
                {
                    case true:
                        if (await this.SaveTextEditor(textEditor) is (false, _))
                            return false;
                        break;
                    case false:
                        break;
                    default:
                        return false;
                }
            }

            this.RemoveTextEditor(textEditor);
            return true;
        }

        [LogInterceptor]
        private void WakeUpTextEditor(TextEditorViewModel textEditor)
        {
            if (textEditor != null)
                this.ActiveTextEditor.Value = textEditor;
        }

        [LogInterceptor]
        private async Task<IEnumerable<(bool result, TextEditorViewModel textEditor)>> LoadTextEditor(IEnumerable<string> paths = null)
        {
            (bool result, IEnumerable<string> fileNames, string filter, Encoding encoding, bool isReadOnly) decideConditions(string root)
            {
                // 起点位置を設定する
                // - ディレクトリが指定されている場合は、それを起点とする
                // - ファイルが指定されている場合は、それを含む階層を起点とする
                // - いずれでも無い場合は、既定値とする
                if (Directory.Exists(root) == false)
                    root = File.Exists(root) ? Path.GetDirectoryName(root) : string.Empty;

                // ダイアログを表示し、ファイルのパスと読み込み条件を選択させる
                IEnumerable<string> fileNames = null;
                string filter = null;
                Encoding encoding = null;
                var isReadOnly = false;
                var ready = this.CommonDialogService.ShowDialog(
                    new OpenFileDialogParameters()
                    {
                        InitialDirectory = root,
                        Filter = CommonDialogHelper.CreateFileFilter(this.SyntaxService),
                        DefaultExtension = TextEditorViewModel.TEXT_EXTENSION,
                        Multiselect = true,
                    },
                    (dialog, parameters) =>
                    {
                        // 文字コードの選択欄を追加する
                        var d = (CommonFileDialog)dialog;
                        var encodingComboBox = CommonDialogHelper.ConvertToComboBox(this.SettingsService.System.AutoDetectEncoding ? null : this.SettingsService.System.Encoding);
                        encodingComboBox.Items.Insert(0, new CommonDialogHelper.EncodingComboBoxItem(null, Resources.Label_AutoDetect));
                        encodingComboBox.SelectedIndex++;
                        var encodingGroupBox = new CommonFileDialogGroupBox($"{Resources.Label_Encoding}(&E):");
                        encodingGroupBox.Items.Add(encodingComboBox);
                        d.Controls.Add(encodingGroupBox);
                    },
                    (dialog, parameters, result) =>
                    {
                        if (result == false)
                            return;

                        var d = (CommonFileDialog)dialog;
                        var p = (OpenFileDialogParameters)parameters;
                        fileNames = p.FileNames;
                        filter = p.FilterName;

                        // 選択された文字コードを取得する
                        var encodingGroupBox = (CommonFileDialogGroupBox)d.Controls.First();
                        var encodingComboBox = (CommonFileDialogComboBox)encodingGroupBox.Items.First();
                        encoding = ((CommonDialogHelper.EncodingComboBoxItem)encodingComboBox.Items[encodingComboBox.SelectedIndex]).Encoding;
                    });
                return (ready, fileNames, filter, encoding, isReadOnly);
            }

            if (paths == null)
            {
                // パスが指定されていない場合
                // - ファイルを選択させて読み込む

                var (result, fileNames, filter, encoding, isReadOnly) = decideConditions(null);
                if (result == false)
                    return Enumerable.Empty<(bool, TextEditorViewModel)>();

                var results = new List<(bool result, TextEditorViewModel textEditor)>();
                foreach (var path in fileNames)
                {
                    var definition = filter != null && this.SyntaxService.Definitions.ContainsKey(filter) ?
                        this.SyntaxService.Definitions[filter] :
                        this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetCommonExtensions().Contains(Path.GetExtension(path)));
                    results.Add(await this.ReadFile(path, encoding, definition, isReadOnly));
                }
                return results;
            }
            else
            {
                // パスが指定されている場合
                // - ファイルの場合は、そのまま読み込む
                // - ディレクトリの場合は、ファイルを選択させて読み込む

                var results = new List<(bool result, TextEditorViewModel textEditor)>();
                foreach (var path in paths.Where(path => File.Exists(path)))
                {
                    var encoding = this.SettingsService.System.AutoDetectEncoding ? null : this.SettingsService.System.Encoding;
                    var definition = this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetCommonExtensions().Contains(Path.GetExtension(path)));
                    results.Add(await this.ReadFile(path, encoding, definition));
                }
                foreach (var (result, fileNames, filter, encoding, isReadOnly) in paths.Where(path => Directory.Exists(path)).Select(path => decideConditions(path)))
                {
                    foreach (var path in fileNames)
                    {
                        var definition = filter != null && this.SyntaxService.Definitions.ContainsKey(filter) ?
                            this.SyntaxService.Definitions[filter] :
                            this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetCommonExtensions().Contains(Path.GetExtension(path)));
                        results.Add(await this.ReadFile(path, encoding, definition, isReadOnly));
                    }
                }
                return results;
            }
        }

        [LogInterceptor]
        private async Task<(bool result, TextEditorViewModel textEditor)> SaveTextEditor(TextEditorViewModel textEditor)
        {
            if (textEditor.IsNewFile || textEditor.IsReadOnly)
                return await this.SaveAsTextEditor(textEditor);
            else
                return await this.WriteFile(textEditor, textEditor.FileName, textEditor.Encoding, textEditor.SyntaxDefinition);
        }

        [LogInterceptor]
        private async Task<(bool result, TextEditorViewModel textEditor)> SaveAsTextEditor(TextEditorViewModel textEditor)
        {
            var path = textEditor.FileName;
            var filterName = textEditor.SyntaxDefinition?.Name;
            var encoding = textEditor.Encoding;

            var ready = this.CommonDialogService.ShowDialog(
                new SaveFileDialogParameters()
                {
                    InitialDirectory = textEditor.IsNewFile == false ? Path.GetDirectoryName(path) : null,
                    DefaultFileName = Path.GetFileName(path),
                    Filter = CommonDialogHelper.CreateFileFilter(this.SyntaxService),
                    DefaultExtension = TextEditorViewModel.TEXT_EXTENSION,
                },
                (dialog, parameters) =>
                {
                    // 文字コードの選択欄を追加する
                    var d = (CommonFileDialog)dialog;
                    var encodingComboBox = CommonDialogHelper.ConvertToComboBox(encoding);
                    var encodingGroupBox = new CommonFileDialogGroupBox($"{Resources.Label_Encoding}(&E):");
                    encodingGroupBox.Items.Add(encodingComboBox);
                    d.Controls.Add(encodingGroupBox);
                },
                (dialog, parameters, result) =>
                {
                    if (result == false)
                        return;

                    var d = (CommonFileDialog)dialog;
                    var p = (SaveFileDialogParameters)parameters;
                    path = p.FileName;
                    filterName = p.FilterName;

                    // 選択された文字コードを取得する
                    var encodingGroupBox = (CommonFileDialogGroupBox)d.Controls.First();
                    var encodingComboBox = (CommonFileDialogComboBox)encodingGroupBox.Items.First();
                    encoding = ((CommonDialogHelper.EncodingComboBoxItem)encodingComboBox.Items[encodingComboBox.SelectedIndex]).Encoding;
                });
            if (ready == false)
                return (false, textEditor);

            var definition = this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetCommonExtensions().Contains(Path.GetExtension(path)));
            return await this.WriteFile(textEditor, path, encoding, definition);
        }

        #endregion

        #region ファイル入出力

        [LogInterceptor]
        private async Task<(bool result, TextEditorViewModel textEditor)> ReadFile(string path, Encoding encoding, XshdSyntaxDefinition definition, bool isReadOnly = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("空のパスが指定されています。", nameof(path));

            var sameTextEditor = this.TextEditors.Where(m => m.IsNewFile == false).FirstOrDefault(m => m.FileName == path);
            if (sameTextEditor != null)
            {
                // 文字コードが異なる場合はリロードする
                if (sameTextEditor.Encoding != encoding)
                {
                    // 同名ファイルを占有しているコンテンツをアクティブにする
                    this.WakeUpTextEditor(sameTextEditor);

                    // 変更がある場合は確認する
                    if (sameTextEditor.IsModified &&
                        this.DialogService.Confirm($"{Resources.Message_ConfirmDiscardChanges}{Environment.NewLine}{sameTextEditor.FileName}") == false)
                    {
                        return (false, sameTextEditor);
                    }

                    // 指定された文字コードでリロードする
                    try
                    {
                        this.IsWorking.Value = true;
                        await sameTextEditor.Reload(encoding);
                        this.Logger.Log($"ファイルを再読み込みしました。: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Info);
                    }
                    catch (Exception e)
                    {
                        this.Logger.Log($"ファイルの再読み込みに失敗しました。: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Warn, e);
                        this.DialogService.Warn(e.Message);
                        return (false, sameTextEditor);
                    }
                    finally
                    {
                        this.IsWorking.Value = false;
                    }
                }

                // シンタックス定義を設定する
                // NOTE: フォールディングを更新させるため、意図的に変更を発生させる
                sameTextEditor.SyntaxDefinition = null;
                sameTextEditor.SyntaxDefinition = definition;

                this.DialogService.ToastNotify($"{Resources.Message_NotifyLoaded}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}[{sameTextEditor.Encoding.EncodingName}, {sameTextEditor.SyntaxDefinition?.Name ?? "Plain Text"}]");
                return (true, sameTextEditor);
            }
            else
            {
                // 他のウィンドウが同名ファイルを占有している場合は処理を委譲する
                foreach (var view in this.GetViews())
                {
                    var viewModel = (MainWindowViewModel)view.DataContext;
                    if (viewModel == this)
                        continue;

                    var existTextEditor = viewModel.TextEditors.FirstOrDefault(e => e.FileName == path);
                    if (existTextEditor == null)
                        continue;

                    viewModel.WakeUpTextEditor(existTextEditor);
                    viewModel.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));
                    return (false, null);
                }

                // ファイルサイズを確認する
                var info = new FileInfo(path);
                if (AppSettings.FileSizeThreshold <= info.Length &&
                    this.DialogService.Confirm(Resources.Message_ConfirmOpenLargeFile) == false)
                {
                    return (false, null);
                }

                // 可能であれば書き込み権限を取得する
                FileStream stream = null;
                if (isReadOnly == false)
                {
                    try
                    {
                        stream = info.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    }
                    catch (Exception e)
                    {
                        // ここでのエラーは無視する
                        this.Logger.Log($"ファイルの書き込み権限を取得できませんでした。読み取り権限のみで再取得します。: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Info, e);
                    }
                }

                // 取得できない場合は、読み取り権限のみを取得する
                if (stream == null)
                {
                    try
                    {
                        stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        this.DialogService.ToastWarn($"{Resources.Message_NotifyFileLocked}{Environment.NewLine}{Path.GetFileName(path)}");
                    }
                    catch (Exception e)
                    {
                        this.Logger.Log($"ファイルの読み取り権限の取得に失敗しました。: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Warn, e);
                        this.DialogService.Warn(e.Message);
                        return (false, null);
                    }
                }

                // ファイルを読み込む
                var textEditor = this.ActiveTextEditor.Value.IsNewFile && this.ActiveTextEditor.Value.IsModified == false ?
                    this.ActiveTextEditor.Value : this.AddTextEditor();
                try
                {
                    this.IsWorking.Value = true;
                    await textEditor.Load(stream, encoding);
                    this.Logger.Log($"ファイルを読み込みました。: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Info);
                }
                catch (Exception e)
                {
                    this.Logger.Log($"ファイルの読み込みに失敗しました。: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Warn, e);
                    this.DialogService.Warn(e.Message);
                    return (false, textEditor);
                }
                finally
                {
                    this.IsWorking.Value = false;
                }

                // シンタックス定義を設定する
                textEditor.SyntaxDefinition = definition;

                this.DialogService.ToastNotify($"{Resources.Message_NotifyLoaded}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}[{textEditor.Encoding.EncodingName}, {textEditor.SyntaxDefinition?.Name ?? "Plain Text"}]");
                return (true, textEditor);
            }
        }

        [LogInterceptor]
        private async Task<(bool result, TextEditorViewModel textEditor)> WriteFile(TextEditorViewModel textEditor, string path, Encoding encoding, XshdSyntaxDefinition definition)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("空のパスが渡されました。", nameof(path));

            var sameTextEditor = this.TextEditors.Where(m => m.IsNewFile == false).FirstOrDefault(m => m.FileName == path);
            if (sameTextEditor != null)
            {
                // 他のコンテンツが同一のパスからなるコンテンツを占有している場合は、保存せずに終了する
                if (sameTextEditor != textEditor)
                {
                    this.WakeUpTextEditor(sameTextEditor);
                    this.DialogService.ToastWarn($"{Resources.Message_NotifyFileInAnother}{Environment.NewLine}{sameTextEditor.FileName}");
                    return (false, sameTextEditor);
                }

                // ファイルに保存する
                try
                {
                    this.IsWorking.Value = true;
                    await sameTextEditor.Save(encoding);
                    this.Logger.Log($"ファイルを上書き保存しました。: Path={path}, Encoding={encoding.EncodingName}", Category.Info);
                }
                catch (Exception e)
                {
                    this.Logger.Log($"ファイルの上書き保存に失敗しました。: Path={path}, Encoding={encoding.EncodingName}", Category.Warn, e);
                    this.DialogService.Warn(e.Message);
                    return (false, sameTextEditor);
                }
                finally
                {
                    this.IsWorking.Value = false;
                }

                // シンタックス定義を設定する
                sameTextEditor.SyntaxDefinition = definition;

                this.DialogService.ToastNotify($"{Resources.Message_NotifySaved}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}[{sameTextEditor.Encoding.EncodingName}]");
                return (true, sameTextEditor);
            }
            else
            {
                // 他のウィンドウが同名ファイルを占有している場合は、保存せずに終了する
                foreach (var view in this.GetViews())
                {
                    var viewModel = (MainWindowViewModel)view.DataContext;
                    if (viewModel == this)
                        continue;

                    var existTextEditor = viewModel.TextEditors.FirstOrDefault(e => e.FileName == path);
                    if (existTextEditor == null)
                        continue;

                    viewModel.WakeUpTextEditor(existTextEditor);
                    viewModel.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));
                    viewModel.DialogService.ToastWarn($"{Resources.Message_NotifyFileLocked}{Environment.NewLine}{existTextEditor.FileName}");
                    return (false, null);
                }

                // ストリームを取得し、ファイルに保存する
                FileStream stream = null;
                try
                {
                    this.IsWorking.Value = true;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                    await textEditor.SaveAs(stream, encoding);
                    this.Logger.Log($"ファイルを新規保存しました。: Path={path}, Encoding={encoding.EncodingName}", Category.Info);
                }
                catch (Exception e)
                {
                    this.Logger.Log($"ファイルの新規保存に失敗しました。: Path={path}, Encoding={encoding.EncodingName}", Category.Warn, e);
                    this.DialogService.Warn(e.Message);
                    return (false, textEditor);
                }
                finally
                {
                    this.IsWorking.Value = false;
                }

                // シンタックス定義を設定する
                textEditor.SyntaxDefinition = definition;

                this.DialogService.ToastNotify($"{Resources.Message_NotifySaved}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}[{textEditor.Encoding.EncodingName}]");
                return (true, textEditor);
            }
        }

        #endregion
    }
}
