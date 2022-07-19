using Dragablz;
using Livet.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MyBase;
using MyBase.Logging;
using MyBase.Wpf.CommonDialogs;
using MyPad.Models;
using MyPad.Properties;
using MyPad.PubSub;
using Prism.Events;
using Prism.Ioc;
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

namespace MyPad.ViewModels;

/// <summary>
/// アプリケーションのメインウィンドウを制御する ViewModel を表します。
/// このクラスは <see cref="TextEditorViewModel"/> を内包します。
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    #region インジェクション

    // Constructor Injection
    public IContainerExtension Container { get; set; }
    public IEventAggregator EventAggregator { get; set; }

    // Dependency Injection
    private ILoggerFacade _logger;
    private IProductInfo _productInfo;
    private SettingsModel _settings;
    private SyntaxService _syntaxService;
    private SharedProperties _sharedProperties;
    [Dependency]
    public ICommonDialogService DialogService { get; set; }
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
    public SettingsModel Settings
    {
        get => this._settings;
        set => this.SetProperty(ref this._settings, value);
    }
    [Dependency]
    public SyntaxService SyntaxService
    {
        get => this._syntaxService;
        set => this.SetProperty(ref this._syntaxService, value);
    }
    [Dependency]
    public SharedProperties SharedProperties
    {
        get => this._sharedProperties;
        set => this.SetProperty(ref this._sharedProperties, value);
    }

    #endregion

    #region プロパティ

    private static int GlobalSequence = 0;

    private int? _sequence;
    public int Sequence => this._sequence ??= ++GlobalSequence;

    public InteractionMessenger Messenger { get; }

    public ReactiveCollection<TextEditorViewModel> TextEditors { get; }

    public ReactiveProperty<bool> IsPending { get; }
    public ReactiveProperty<TextEditorViewModel> ActiveTextEditor { get; }
    public ReactiveProperty<TextEditorViewModel> DiffSource { get; }
    public ReactiveProperty<TextEditorViewModel> DiffDestination { get; }
    public ReactiveProperty<FlowDocument> FlowDocument { get; }

    private List<IDisposable> CompositeContent { get; }
    public ReactiveProperty<bool> IsOpenPrintPreviewContent { get; }
    public ReactiveProperty<bool> IsOpenOptionContent { get; }
    public ReactiveProperty<bool> IsOpenMaintenanceContent { get; }
    public ReactiveProperty<bool> IsOpenAboutContent { get; }
    public ReactiveProperty<bool> IsOpenShortcutKeysContent { get; }
    public ReactiveProperty<bool> IsOpenDiffContent { get; }

    public ReactiveProperty<bool> IsFlyoutMode { get; }
    public ReactiveProperty<bool> IsEditMode { get; }

    public ReactiveCommand NewCommand { get; }
    public ReactiveCommand NewWindowCommand { get; }
    public ReactiveCommand OpenCommand { get; }
    public ReactiveCommand SaveCommand { get; }
    public ReactiveCommand SaveAsCommand { get; }
    public ReactiveCommand SaveAllCommand { get; }
    public ReactiveCommand CloseCommand { get; }
    public ReactiveCommand CloseAllCommand { get; }
    public ReactiveCommand CloseOtherCommand { get; }
    public ReactiveCommand ExitCommand { get; }
    public ReactiveCommand ExitApplicationCommand { get; }
    public ReactiveCommand PrintCommand { get; }
    public ReactiveCommand PrintDirectCommand { get; }
    public ReactiveCommand PrintPreviewCommand { get; }
    public ReactiveCommand OptionCommand { get; }
    public ReactiveCommand MaintenanceCommand { get; }
    public ReactiveCommand AboutCommand { get; }
    public ReactiveCommand ShortcutKeysCommand { get; }
    public ReactiveCommand DiffCommand { get; }
    public ReactiveCommand DiffUnmodifiedCommand { get; }
    public ReactiveCommand GoToLineCommand { get; }
    public ReactiveCommand ChangeEncodingCommand { get; }
    public ReactiveCommand ChangeSyntaxCommand { get; }
    public ReactiveCommand SwitchPomodoroTimerCommand { get; }

    public ReactiveCommand<DragEventArgs> DropHandler { get; }
    public ReactiveCommand<CancelEventArgs> ClosingHandler { get; }

    public Func<object> TextEditorFactory =>
        () => this.CreateTextEditor();
    public ItemActionCallback ClosingTextEditorHandler
       => new(async e =>
       {
           if (e.IsCancelled || e.DragablzItem?.DataContext is not TextEditorViewModel textEditor)
               return;

           // View の TextEditor インスタンスはドッキングウィンドウを行き来するたびに再生成される
           // その際、コントロールの破棄と生成の前後で ViewModel のインスタンスは同一である
           // したがって TextEditor の ViewModel は View に比べ生存期間が長いためリソースの解放は ViewModel 起点に行う
           e.Cancel();

           await this.InvokeCloseTextEditor(textEditor);
       });

    #endregion

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="container">DI コンテナ</param>
    /// <param name="eventAggregator">イベントアグリゲーター</param>
    [InjectionConstructor]
    [LogInterceptor]
    public MainWindowViewModel(IContainerExtension container, IEventAggregator eventAggregator)
    {
        // ----- インジェクション ------------------------------

        this.Container = container;
        this.EventAggregator = eventAggregator;


        // ----- プロパティの定義 ------------------------------

        this.Messenger = new();

        this.TextEditors = new ReactiveCollection<TextEditorViewModel>().AddTo(this.CompositeDisposable);
        BindingOperations.EnableCollectionSynchronization(this.TextEditors, new object());

        this.IsPending = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable);
        this.ActiveTextEditor = new ReactiveProperty<TextEditorViewModel>().AddTo(this.CompositeDisposable);
        this.DiffSource = new ReactiveProperty<TextEditorViewModel>().AddTo(this.CompositeDisposable);
        this.DiffDestination = new ReactiveProperty<TextEditorViewModel>().AddTo(this.CompositeDisposable);
        this.FlowDocument = new ReactiveProperty<FlowDocument>().AddTo(this.CompositeDisposable);

        this.CompositeContent = new();
        this.IsOpenPrintPreviewContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable).AddTo(this.CompositeContent);
        this.IsOpenOptionContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable).AddTo(this.CompositeContent);
        this.IsOpenMaintenanceContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable).AddTo(this.CompositeContent);
        this.IsOpenAboutContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable).AddTo(this.CompositeContent);
        this.IsOpenShortcutKeysContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable).AddTo(this.CompositeContent);
        this.IsOpenDiffContent = new ReactiveProperty<bool>().AddTo(this.CompositeDisposable).AddTo(this.CompositeContent);

        this.IsFlyoutMode = new[] {
                this.IsOpenPrintPreviewContent,
                this.IsOpenOptionContent,
                this.IsOpenMaintenanceContent,
                this.IsOpenAboutContent,
                this.IsOpenShortcutKeysContent,
                this.IsOpenDiffContent,
            }
            .CombineLatest(_ => _.Any(_ => _))
            .ToReactiveProperty()
            .AddTo(this.CompositeDisposable);
        this.IsEditMode = new[] {
                this.IsPending,
                this.IsFlyoutMode,
            }
            .CombineLatestValuesAreAllFalse()
            .ToReactiveProperty()
            .AddTo(this.CompositeDisposable);


        // ----- 変更通知の購読 ------------------------------

        void closeContent(IEnumerable<ReactiveProperty<bool>> except = null)
            => this.CompositeContent
                .OfType<ReactiveProperty<bool>>()
                .Except(except ?? Enumerable.Empty<ReactiveProperty<bool>>())
                .Where(p => p.Value)
                .ForEach(p => p.Value = false);

        this.IsOpenPrintPreviewContent
            .Where(isOpen => isOpen)
            .Subscribe(_ =>
            {
                closeContent(new[] { this.IsOpenPrintPreviewContent });
                this.Logger.Log($"印刷プレビューを表示します。", Category.Info);
            })
            .AddTo(this.CompositeDisposable);

        this.IsOpenOptionContent
            .Where(isOpen => isOpen)
            .Subscribe(_ =>
            {
                closeContent(new[] { this.IsOpenOptionContent });
                this.Logger.Log($"オプションを表示します。", Category.Info);
            })
            .AddTo(this.CompositeDisposable);

        // オプションを閉じた際に設定ファイルを更新する
        this.IsOpenOptionContent
            .Inverse()
            .Where(isClose => isClose)
            .Subscribe(_ =>
            {
                this.Settings?.Save();
            })
            .AddTo(this.CompositeDisposable);

        this.IsOpenMaintenanceContent
            .Where(isOpen => isOpen)
            .Subscribe(_ =>
            {
                closeContent(new[] { this.IsOpenMaintenanceContent });
                this.Logger.Log($"メンテナンスを表示します。", Category.Info);
            })
            .AddTo(this.CompositeDisposable);

        this.IsOpenAboutContent
            .Where(isOpen => isOpen)
            .Subscribe(_ =>
            {
                closeContent(new[] { this.IsOpenAboutContent });
                this.Logger.Log($"バージョン情報を表示します。", Category.Info);
            })
            .AddTo(this.CompositeDisposable);

        this.IsOpenShortcutKeysContent
            .Where(isOpen => isOpen)
            .Subscribe(_ =>
            {
                closeContent(new[] { this.IsOpenShortcutKeysContent });
                this.Logger.Log($"ショートカットキー一覧を表示します。", Category.Info);
            })
            .AddTo(this.CompositeDisposable);

        this.IsOpenDiffContent
            .Where(isOpen => isOpen)
            .Subscribe(_ =>
            {
                closeContent(new[] { this.IsOpenDiffContent });
                this.Logger.Log($"差分比較を表示します。: Source={this.DiffSource.Value?.FileName}, Destination={this.DiffDestination.Value?.FileName}", Category.Info);
            })
            .AddTo(this.CompositeDisposable);

        // 差分比較を閉じた際に比較用ドキュメントを解放する
        this.IsOpenDiffContent
            .Inverse()
            .Where(isClose => isClose)
            .Subscribe(_ =>
            {
                this.DiffSource.Value = null;
                this.DiffDestination.Value = null;
            })
            .AddTo(this.CompositeDisposable);

        // 印刷プレビューの表示前に FlowDocument を生成する
        this.IsOpenPrintPreviewContent
            .Subscribe(async isOpen => this.FlowDocument.Value = isOpen ? await this.ActiveTextEditor.Value.CreateFlowDocument() : null)
            .AddTo(this.CompositeDisposable);


        // ----- コマンドの定義 ------------------------------

        this.NewCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() => this.WakeUpTextEditor(this.AddTextEditor()))
            .AddTo(this.CompositeDisposable);

        this.NewWindowCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() => this.Container.Resolve<Views.MainWindow>().Show())
            .AddTo(this.CompositeDisposable);

        this.OpenCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                var results = await this.Load();
                var textEditor = results.LastOrDefault(tuple => tuple.textEditor != null).textEditor;
                if (textEditor != null)
                    this.WakeUpTextEditor(textEditor);
            })
            .AddTo(this.CompositeDisposable);

        this.SaveCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                if (this.ActiveTextEditor.Value.IsModified || this.ActiveTextEditor.Value.IsNewFile)
                    await this.Save(this.ActiveTextEditor.Value);
            })
            .AddTo(this.CompositeDisposable);

        this.SaveAsCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () => await this.SaveAs(this.ActiveTextEditor.Value))
            .AddTo(this.CompositeDisposable);

        this.SaveAllCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                foreach (var target in this.TextEditors.Where(e => e.IsReadOnly == false))
                {
                    this.WakeUpTextEditor(target);
                    if (await this.Save(target) == false)
                        return;
                }
            })
            .AddTo(this.CompositeDisposable);

        this.CloseCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () => await this.InvokeCloseTextEditor(this.ActiveTextEditor.Value))
            .AddTo(this.CompositeDisposable);

        this.CloseAllCommand = this.IsEditMode.ToReactiveCommand()
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
                    this.WakeUpTextEditor(this.AddTextEditor());
            })
            .AddTo(this.CompositeDisposable);

        this.CloseOtherCommand = this.IsEditMode.ToReactiveCommand()
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

        this.ExitCommand = new ReactiveCommand()
            .WithSubscribe(async () => await this.InvokeClose())
            .AddTo(this.CompositeDisposable);

        this.ExitApplicationCommand = new ReactiveCommand()
            .WithSubscribe(() => this.EventAggregator.GetEvent<ExitApplicationEvent>().Publish())
            .AddTo(this.CompositeDisposable);

        // プレビュー画面からの印刷用コマンド
        this.PrintCommand = this.IsPending.Inverse().ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                try
                {
                    this.IsPending.Value = true;

                    var target = this.ActiveTextEditor.Value;
                    this.Logger.Log($"印刷ダイアログを表示します。tab#{target.Sequence} win#{this.Sequence}", Category.Info);

                    var result = this.DialogService.ShowDialog(
                        new PrintDialogParameters()
                        {
                            Title = this.ProductInfo.Description,
                            FlowDocument = this.FlowDocument.Value ?? await target.CreateFlowDocument()
                        });
                    if (result)
                        this.Logger.Log($"印刷を実行しました。(OSやハードウェアの要因で処理がキャンセルされた可能性もあります。) tab#{target.Sequence} win#{this.Sequence}", Category.Info);
                    else
                        this.Logger.Log($"印刷はキャンセルされました。tab#{target.Sequence} win#{this.Sequence}", Category.Info);
                }
                finally
                {
                    this.IsOpenPrintPreviewContent.Value = false;
                    this.IsPending.Value = false;
                }
            })
            .AddTo(this.CompositeDisposable);

        // 直接印刷用のコマンド
        this.PrintDirectCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() =>
            {
                if (this.PrintCommand.CanExecute())
                    this.PrintCommand.Execute();
            })
            .AddTo(this.CompositeDisposable);

        this.PrintPreviewCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() => this.IsOpenPrintPreviewContent.Value = true)
            .AddTo(this.CompositeDisposable);

        this.OptionCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() => this.IsOpenOptionContent.Value = true)
            .AddTo(this.CompositeDisposable);

        this.MaintenanceCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() => this.IsOpenMaintenanceContent.Value = true)
            .AddTo(this.CompositeDisposable);

        this.AboutCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() => this.IsOpenAboutContent.Value = true)
            .AddTo(this.CompositeDisposable);

        this.ShortcutKeysCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(() => this.IsOpenShortcutKeysContent.Value = true)
            .AddTo(this.CompositeDisposable);

        this.DiffCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                string diffSourcePath = null, diffDestinationPath = null;
                try
                {
                    this.IsPending.Value = true;

                    var textEditors = MvvmHelper.GetMainWindowViewModels().SelectMany(viewModel => viewModel.TextEditors);
                    var result = false;
                    (result, diffSourcePath, diffDestinationPath) = await this.DialogService.SelectDiffFiles(textEditors.Select(e => e.FileName), this.ActiveTextEditor.Value.FileName);
                    if (result == false)
                        return;

                    this.DiffSource.Value = textEditors.First(e => e.FileName == diffSourcePath);
                    this.DiffDestination.Value = textEditors.First(e => e.FileName == diffDestinationPath);
                }
                catch (Exception e)
                {
                    this.Logger.Log($"差分を比較するファイルの読み込みに失敗しました。: SourcePath={diffSourcePath ?? "notset"}, DestinationPath={diffDestinationPath ?? "notset"}", Category.Error, e);
                    this.DialogService.Warn(e.Message);

                    this.DiffSource.Value = null;
                    this.DiffDestination.Value = null;
                    return;
                }
                finally
                {
                    this.IsPending.Value = false;
                }

                this.IsOpenDiffContent.Value = true;
            })
            .AddTo(this.CompositeDisposable);

        this.DiffUnmodifiedCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                this.DiffSource.Value = await this.ActiveTextEditor.Value.CloneFromFile();
                this.DiffDestination.Value = this.ActiveTextEditor.Value;
                this.IsOpenDiffContent.Value = true;
            })
            .AddTo(this.CompositeDisposable);

        this.GoToLineCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                try
                {
                    this.IsPending.Value = true;

                    var target = this.ActiveTextEditor.Value;
                    var (result, line) = await this.DialogService.ChangeLine(target);
                    if (result == false)
                        return;

                    target.Line = line;
                    await this.Messenger.RaiseAsync(new InteractionMessage(nameof(Views.MainWindow.ScrollToCaret)));
                    this.Logger.Log($"指定行へ移動しました。tab#{target.Sequence} win#{this.Sequence}: Line={line}", Category.Info);
                }
                finally
                {
                    this.IsPending.Value = false;
                }
            })
            .AddTo(this.CompositeDisposable);

        this.ChangeEncodingCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                try
                {
                    this.IsPending.Value = true;

                    var target = this.ActiveTextEditor.Value;
                    var (result, encoding) = await this.DialogService.ChangeEncoding(target);
                    if (result == false)
                        return;

                    if (target.IsNewFile)
                        target.Encoding = encoding;
                    else
                        await this.CreateInstance(target.FileName, encoding, target.IsReadOnly);
                    this.Logger.Log($"文字コードを変更しました。tab#{target.Sequence} win#{this.Sequence}: Encoding={encoding.EncodingName}", Category.Info);
                }
                finally
                {
                    this.IsPending.Value = false;
                }
            })
            .AddTo(this.CompositeDisposable);

        this.ChangeSyntaxCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                try
                {
                    this.IsPending.Value = true;

                    var target = this.ActiveTextEditor.Value;
                    var (result, syntax) = await this.DialogService.ChangeSyntax(target);
                    if (result == false)
                        return;

                    var definition = string.IsNullOrEmpty(syntax) ? null :
                        this.SyntaxService.Definitions.ContainsKey(syntax) ? this.SyntaxService.Definitions[syntax] : null;
                    target.SyntaxDefinition = definition;
                    this.Logger.Log($"シンタックス定義を変更しました。tab#{target.Sequence} win#{this.Sequence}: Syntax={syntax}", Category.Info);
                }
                finally
                {
                    this.IsPending.Value = false;
                }
            })
            .AddTo(this.CompositeDisposable);

        this.SwitchPomodoroTimerCommand = this.IsEditMode.ToReactiveCommand()
            .WithSubscribe(async () =>
            {
                try
                {
                    this.IsPending.Value = true;

                    if (this.SharedProperties.IsInPomodoro.Value)
                    {
                        if (this.DialogService.Confirm(Resources.Message_ConfirmStopPomodoro) == false)
                            return;
                    }
                    else
                    {
                        var (result, pomodoroDuration, pomodoroBreakDuration) = await this.DialogService.ChangePomodoroTimer(this.Settings.Misc);
                        if (result == false)
                            return;
                        this.Settings.Misc.PomodoroDuration = pomodoroDuration;
                        this.Settings.Misc.PomodoroBreakDuration = pomodoroBreakDuration;
                    }

                    this.EventAggregator.GetEvent<SwitchPomodoroTimerEvent>().Publish();
                }
                finally
                {
                    this.IsPending.Value = false;
                }
            })
            .AddTo(this.CompositeDisposable);

        this.DropHandler = this.IsEditMode.ToReactiveCommand<DragEventArgs>()
            .WithSubscribe(e =>
            {
                if (e.Data.GetData(DataFormats.FileDrop) is IEnumerable<string> paths && paths.Any())
                {
                    this.Logger.Log($"ファイルがドロップされました。: Paths=[{string.Join(", ", paths)}]", Category.Info);
                    _ = this.InvokeLoad(paths);
                    e.Handled = true;
                }
            })
            .AddTo(this.CompositeDisposable);

        this.ClosingHandler = new ReactiveCommand<CancelEventArgs>()
            .WithSubscribe(async e =>
            {
                // INFO: Closing イベント内で非同期処理後にイベントをキャンセルできない問題への対応 (ViewModel)
                // 事前に View 側に ViewModel の Dispose を検知し Close する処理を組んでおく。
                // ViewModel は常にイベントをキャンセルした状態で処理を行っていく。
                // Close の要件を満たした場合は Dispose メソッドを実行する。
                e.Cancel = true;
                await this.InvokeClose();
            })
            .AddTo(this.CompositeDisposable);
    }

    #region 公開メソッド

    /// <summary>
    /// 指定されたパスのファイルを読み込みます。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>非同期タスク</returns>
    [LogInterceptor]
    public async Task InvokeLoad(string path)
    {
        await this.InvokeLoad(path, null);
    }

    /// <summary>
    /// 指定されたパスのファイルを読み込みます。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <param name="encoding">文字コード</param>
    /// <returns>非同期タスク</returns>
    [LogInterceptor]
    public async Task InvokeLoad(string path, Encoding encoding)
    {
        // View 起点で呼ばれるとは限らないため ViewModel で Activate を実行する
        this.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));

        var definition = this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetExtensions().Contains(Path.GetExtension(path)));
        var (result, textEditor) = await this.CreateInstance(path, encoding);
        if (result)
        {
            textEditor.SyntaxDefinition = null;
            textEditor.SyntaxDefinition = definition;
        }
        if (textEditor != null)
            this.WakeUpTextEditor(textEditor);
    }

    /// <summary>
    /// 指定されたパスのファイルを読み込みます。
    /// </summary>
    /// <param name="paths">ファイルパス</param>
    /// <returns>非同期タスク</returns>
    [LogInterceptor]
    public async Task InvokeLoad(IEnumerable<string> paths)
    {
        // View 起点で呼ばれるとは限らないため ViewModel で Activate を実行する
        this.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));

        var results = await this.Load(paths);
        var textEditor = results.LastOrDefault(tuple => tuple.textEditor != null).textEditor;
        if (textEditor != null)
            this.WakeUpTextEditor(textEditor);
    }

    /// <summary>
    /// ウィンドウの終了を試行します。
    /// 内包するすべての <see cref="TextEditorViewModel"/> が終了要求に応じた場合、ウィンドウは終了します。
    /// </summary>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    public async Task<bool> InvokeClose()
    {
        // View 起点で呼ばれるとは限らないため ViewModel で Activate を実行する
        this.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));

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

    /// <summary>
    /// テキストエディタの終了を試行します。
    /// </summary>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    public async Task<bool> InvokeCloseTextEditor(TextEditorViewModel textEditor)
    {
        var result = await this.TryCloseTextEditor(textEditor);
        if (this.TextEditors.Any() == false)
            this.WakeUpTextEditor(this.AddTextEditor());
        return result;
    }

    #endregion

    #region ファイル入出力

    /// <summary>
    /// ユーザインタラクションを介してパスを指定し、ファイルを読み込み <see cref="TextEditorViewModel"/> クラスのインスタンスを生成します。
    /// </summary>
    /// <returns>
    /// ファイルが読み込まれてインスタンスが生成されたかどうかを示す値と <see cref="TextEditorViewModel"/> クラスのインスタンスをイテレータで返却します。
    /// ファイルを読み込めない場合は (<see cref="false"/>, <see cref="null"/>) を返し、すでに他のタブまたはウィンドウで開かれているファイルであれば (<see cref="false"/>, 既存の <see cref="TextEditorViewModel"/> インスタンス) を返します。
    /// </returns>
    [LogInterceptor]
    private async Task<IEnumerable<(bool result, TextEditorViewModel textEditor)>> Load()
    {
        return await this.Load(null);
    }

    /// <summary>
    /// 指定されたパスのファイルを読み込み <see cref="TextEditorViewModel"/> クラスのインスタンスを生成します。
    /// </summary>
    /// <param name="paths">ファイルパス</param>
    /// <returns>
    /// ファイルが読み込まれてインスタンスが生成されたかどうかを示す値と <see cref="TextEditorViewModel"/> クラスのインスタンスをイテレータで返却します。
    /// ファイルを読み込めない場合は (<see cref="false"/>, <see cref="null"/>) を返し、すでに他のタブまたはウィンドウで開かれているファイルであれば (<see cref="false"/>, 既存の <see cref="TextEditorViewModel"/> インスタンス) を返します。
    /// </returns>
    [LogInterceptor]
    private async Task<IEnumerable<(bool result, TextEditorViewModel textEditor)>> Load(IEnumerable<string> paths)
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
            var ready = this.DialogService.ShowDialog(
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
                    var encodingComboBox = CommonDialogHelper.CreateEncodingComboBox(this.Settings.System.AutoDetectEncoding ? null : this.Settings.System.Encoding);
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
                    this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetExtensions().Contains(Path.GetExtension(path)));
                var (r, t) = await this.CreateInstance(path, encoding, isReadOnly);
                if (r)
                {
                    t.SyntaxDefinition = null;
                    t.SyntaxDefinition = definition;
                }
                results.Add((r, t));
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
                var encoding = this.Settings.System.AutoDetectEncoding ? null : this.Settings.System.Encoding;
                var definition = this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetExtensions().Contains(Path.GetExtension(path)));
                var (r, t) = await this.CreateInstance(path, encoding);
                if (r)
                {
                    t.SyntaxDefinition = null;
                    t.SyntaxDefinition = definition;
                }
                results.Add((r, t));
            }
            foreach (var (result, fileNames, filter, encoding, isReadOnly) in paths.Where(path => Directory.Exists(path)).Select(path => decideConditions(path)))
            {
                if (result == false)
                    continue;

                foreach (var path in fileNames)
                {
                    var definition = filter != null && this.SyntaxService.Definitions.ContainsKey(filter) ?
                        this.SyntaxService.Definitions[filter] :
                        this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetExtensions().Contains(Path.GetExtension(path)));
                    var (r, t) = await this.CreateInstance(path, encoding, isReadOnly);
                    if (r)
                    {
                        t.SyntaxDefinition = null;
                        t.SyntaxDefinition = definition;
                    }
                    results.Add((r, t));
                }
            }
            return results;
        }
    }

    /// <summary>
    /// 指定されたパスのファイルを読み込み <see cref="TextEditorViewModel"/> クラスのインスタンスを生成します。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <param name="encoding">文字コード (この値が null の場合、文字コードは自動判定されます。)</param>
    /// <param name="isReadOnly">読み取り専用</param>
    /// <returns>
    /// ファイルが読み込まれてインスタンスが生成されたかどうかを示す値と <see cref="TextEditorViewModel"/> クラスのインスタンスをイテレータで返却します。
    /// ファイルを読み込めない場合は (<see cref="false"/>, <see cref="null"/>) を返し、すでに他のタブまたはウィンドウで開かれているファイルであれば (<see cref="false"/>, 既存の <see cref="TextEditorViewModel"/> インスタンス) を返します。
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    [LogInterceptor]
    private async Task<(bool result, TextEditorViewModel textEditor)> CreateInstance(string path, Encoding encoding, bool isReadOnly = false)
    {
        this.Logger.Debug($"ファイルを読み込みます。: Path={path}, Encoding={encoding?.EncodingName ?? "<null>"}, IsReadOnly={isReadOnly}");

        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

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
                    this.IsPending.Value = true;
                    await sameTextEditor.Reload(encoding);
                    this.Logger.Log($"ファイルを再読み込みしました。tab#{sameTextEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Info);
                }
                catch (Exception e)
                {
                    this.Logger.Log($"ファイルの再読み込みに失敗しました。tab#{sameTextEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Error, e);
                    this.DialogService.Warn(e.Message);
                    return (false, sameTextEditor);
                }
                finally
                {
                    this.IsPending.Value = false;
                }
            }

            this.DialogService.ToastNotify($"{Resources.Message_NotifyLoaded}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}{sameTextEditor.Encoding.EncodingName}");
            return (true, sameTextEditor);
        }
        else
        {
            // 他のウィンドウが同名ファイルを占有している場合は処理を委譲する
            foreach (var viewModel in MvvmHelper.GetMainWindowViewModels())
            {
                var existTextEditor = viewModel?.TextEditors.FirstOrDefault(e => e.FileName == path);
                if (existTextEditor == null)
                    continue;

                viewModel.WakeUpTextEditor(existTextEditor);
                viewModel.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));
                return (false, null);
            }

            // ファイルサイズを確認する
            var info = new FileInfo(path);
            if (AppSettingsReader.HugeSizeThreshold <= info.Length &&
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
                catch
                {
                    this.Logger.Log($"ファイルの書き込み権限を取得できませんでした。読み取り権限のみで再取得します。win#{this.Sequence}: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Info);
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
                    this.Logger.Log($"ファイルの読み取り権限の取得に失敗しました。win#{this.Sequence}: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}", Category.Error, e);
                    this.DialogService.Warn(e.Message);
                    return (false, null);
                }
            }

            // ファイルを読み込む
            var textEditor = this.ActiveTextEditor.Value?.IsNewFile == true && this.ActiveTextEditor.Value.IsModified == false ?
                this.ActiveTextEditor.Value : this.AddTextEditor();
            try
            {
                this.IsPending.Value = true;
                await textEditor.Load(stream, encoding);
                this.Logger.Log($"ファイルを読み込みました。tab#{textEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}, IsReadOnly={textEditor.IsReadOnly}", Category.Info);
            }
            catch (Exception e)
            {
                this.Logger.Log($"ファイルの読み込みに失敗しました。tab#{textEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding?.EncodingName ?? "Auto"}, IsReadOnly={textEditor.IsReadOnly}", Category.Error, e);
                this.DialogService.Warn(e.Message);
                return (false, textEditor);
            }
            finally
            {
                this.IsPending.Value = false;
            }

            this.DialogService.ToastNotify($"{Resources.Message_NotifyLoaded}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}{textEditor.Encoding.EncodingName}");
            return (true, textEditor);
        }
    }

    /// <summary>
    /// <see cref="TextEditorViewModel"/> が保持するドキュメントデータをファイルに保存します。
    /// インスタンスの状態に応じてユーザインタラクションが発生します。
    /// </summary>
    /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
    /// <returns>
    /// ファイルに保存されたかどうかを示す値を返却します。
    /// ファイルに保存できない場合や、すでに他のタブまたはウィンドウで開かれているファイルであれば <see cref="false"/> を返します。
    /// </returns>
    [LogInterceptor]
    private async Task<bool> Save(TextEditorViewModel textEditor)
    {
        if (textEditor.IsNewFile || textEditor.IsReadOnly)
            return await this.SaveAs(textEditor);
        else
            return await this.SaveAs(textEditor, textEditor.FileName, textEditor.Encoding);
    }

    /// <summary>
    /// ユーザインタラクションを介してパスを指定し、<see cref="TextEditorViewModel"/> が保持するドキュメントデータをファイルに保存します。
    /// </summary>
    /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
    /// <returns>
    /// ファイルに保存されたかどうかを示す値を返却します。
    /// ファイルに保存できない場合や、すでに他のタブまたはウィンドウで開かれているファイルであれば <see cref="false"/> を返します。
    /// </returns>
    [LogInterceptor]
    private async Task<bool> SaveAs(TextEditorViewModel textEditor)
    {
        var path = textEditor.FileName;
        var filterName = textEditor.SyntaxDefinition?.Name;
        var encoding = textEditor.Encoding;

        var ready = this.DialogService.ShowDialog(
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
                var encodingComboBox = CommonDialogHelper.CreateEncodingComboBox(encoding);
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
            return false;

        var definition = this.SyntaxService.Definitions.Values.FirstOrDefault(d => d.GetExtensions().Contains(Path.GetExtension(path)));
        var result = await this.SaveAs(textEditor, path, encoding);
        if (result) textEditor.SyntaxDefinition = definition;
        return result;
    }

    /// <summary>
    /// <see cref="TextEditorViewModel"/> が保持するドキュメントデータをファイルに保存します。
    /// </summary>
    /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
    /// <param name="path">ファイルパス</param>
    /// <param name="encoding">文字コード</param>
    /// <returns>
    /// ファイルに保存されたかどうかを示す値を返却します。
    /// ファイルに保存できない場合や、すでに他のタブまたはウィンドウで開かれているファイルであれば <see cref="false"/> を返します。
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    [LogInterceptor]
    private async Task<bool> SaveAs(TextEditorViewModel textEditor, string path, Encoding encoding)
    {
        this.Logger.Debug($"ファイルに書き出します。: TextEditor={textEditor?.FileName ?? "<null>"}, Path={path}, Encoding={encoding?.EncodingName ?? "<null>"}");

        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        var sameTextEditor = this.TextEditors.Where(m => m.IsNewFile == false).FirstOrDefault(m => m.FileName == path);
        if (sameTextEditor != null)
        {
            // 他のコンテンツが同一のパスからなるコンテンツを占有している場合は、保存せずに終了する
            if (sameTextEditor != textEditor)
            {
                this.WakeUpTextEditor(sameTextEditor);
                this.DialogService.ToastWarn($"{Resources.Message_NotifyFileInAnother}{Environment.NewLine}{sameTextEditor.FileName}");
                return false;
            }

            // ファイルに保存する
            try
            {
                this.IsPending.Value = true;
                await sameTextEditor.Save(encoding);
                this.Logger.Log($"ファイルを上書き保存しました。tab#{sameTextEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding.EncodingName}", Category.Info);
            }
            catch (Exception e)
            {
                this.Logger.Log($"ファイルの上書き保存に失敗しました。tab#{sameTextEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding.EncodingName}", Category.Error, e);
                this.DialogService.Warn(e.Message);
                return false;
            }
            finally
            {
                this.IsPending.Value = false;
            }

            this.DialogService.ToastNotify($"{Resources.Message_NotifySaved}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}[{sameTextEditor.Encoding.EncodingName}]");
            return true;
        }
        else
        {
            // 他のウィンドウが同名ファイルを占有している場合は、保存せずに終了する
            foreach (var viewModel in MvvmHelper.GetMainWindowViewModels())
            {
                var existTextEditor = viewModel?.TextEditors.FirstOrDefault(e => e.FileName == path);
                if (existTextEditor == null)
                    continue;

                viewModel.WakeUpTextEditor(existTextEditor);
                viewModel.Messenger.Raise(new InteractionMessage(nameof(Views.MainWindow.Activate)));
                viewModel.DialogService.ToastWarn($"{Resources.Message_NotifyFileLocked}{Environment.NewLine}{existTextEditor.FileName}");
                return false;
            }

            // ストリームを取得し、ファイルに保存する
            FileStream stream = null;
            try
            {
                this.IsPending.Value = true;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                stream = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                await textEditor.SaveAs(stream, encoding);
                this.Logger.Log($"ファイルを新規保存しました。tab#{textEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding.EncodingName}", Category.Info);
            }
            catch (Exception e)
            {
                this.Logger.Log($"ファイルの新規保存に失敗しました。tab#{textEditor.Sequence} win#{this.Sequence}: Path={path}, Encoding={encoding.EncodingName}", Category.Error, e);
                this.DialogService.Warn(e.Message);
                return false;
            }
            finally
            {
                this.IsPending.Value = false;
            }

            this.DialogService.ToastNotify($"{Resources.Message_NotifySaved}{Environment.NewLine}{Path.GetFileName(path)}{Environment.NewLine}[{textEditor.Encoding.EncodingName}]");
            return true;
        }
    }

    #endregion

    #region テキストエディタの制御

    /// <summary>
    /// <see cref="TextEditorViewModel"/> クラスの新しいインスタンスを生成します。
    /// </summary>
    /// <returns>生成された <see cref="TextEditorViewModel"/> クラスのインスタンス</returns>
    [LogInterceptor]
    private TextEditorViewModel CreateTextEditor()
    {
        var textEditor = this.Container.Resolve<TextEditorViewModel>();
        this.Logger.Log($"タブを生成しました。tab#{textEditor.Sequence} win#{this.Sequence}", Category.Info);
        return textEditor;
    }

    /// <summary>
    /// <see cref="TextEditorViewModel"/> クラスの新しいインスタンスを生成し、<see cref="TextEditors"/> に追加します。
    /// </summary>
    /// <returns>生成された <see cref="TextEditorViewModel"/> クラスのインスタンス</returns>
    [LogInterceptor]
    private TextEditorViewModel AddTextEditor()
    {
        var textEditor = this.CreateTextEditor();
        this.TextEditors.Add(textEditor);
        return textEditor;
    }

    /// <summary>
    /// 指定された <see cref="TextEditorViewModel"/> インスタンスの終了を要求します。
    /// インスタンスの状態に応じてユーザインタラクションが発生します。
    /// </summary>
    /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private async Task<bool> TryCloseTextEditor(TextEditorViewModel textEditor)
    {
        if (textEditor.IsModified)
        {
            var result = this.DialogService.CancelableConfirm($"{Resources.Message_ConfirmSaveChanges}{Environment.NewLine}{textEditor.FileName}");
            switch (result)
            {
                case true:
                    if (await this.Save(textEditor) == false)
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

    /// <summary>
    /// 指定されたインスタンスを <see cref="TextEditors"/> から取り除き、リソースを解放します。
    /// </summary>
    /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private bool RemoveTextEditor(TextEditorViewModel textEditor)
    {
        if (this.TextEditors.Contains(textEditor) == false)
            return false;

        this.TextEditors.Remove(textEditor);
        textEditor.Dispose();
        this.Logger.Log($"タブを破棄しました。tab#{textEditor.Sequence} win#{this.Sequence}", Category.Info);
        return true;
    }

    /// <summary>
    /// 指定されたインスタンスをアクティブなテキストエディタに設定します。
    /// </summary>
    /// <param name="textEditor"><see cref="TextEditorViewModel"/> クラスのインスタンス</param>
    [LogInterceptor]
    private void WakeUpTextEditor(TextEditorViewModel textEditor)
    {
        this.ActiveTextEditor.Value = textEditor ?? throw new ArgumentNullException(nameof(textEditor));
    }

    #endregion
}
