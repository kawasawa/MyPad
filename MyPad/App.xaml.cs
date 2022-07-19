using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using MyBase;
using MyBase.Logging;
using MyBase.Wpf.CommonDialogs;
using Prism;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Unity;
using QuickConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Unity;
using Vanara.PInvoke;
using WPFLocalizeExtension.Providers;

namespace MyPad;

/// <summary>
/// アプリケーションのエントリーポイントとなるクラスを表します。
/// </summary>
/// <remarks>
/// <see cref="PrismApplicationBase"/> に用意された初期設定用のメソッドが下記の順に呼び出されます。
///
/// <see cref="PrismApplicationBase.OnStartup"/>
///  ├ <see cref="PrismApplicationBase.ConfigureViewModelLocator"/>
///  ├ <see cref="PrismApplicationBase.Initialize"/>
///  │  ├ <see cref="PrismApplicationBase.CreateContainerExtension"/>
///  │  ├ <see cref="PrismApplicationBase.CreateModuleCatalog"/>
///  │  ├ <see cref="PrismApplicationBase.RegisterRequiredTypes"/>
///  │  ├ <see cref="PrismApplicationBase.RegisterTypes"/>
///  │  ├ <see cref="PrismApplicationBase.ConfigureModuleCatalog"/>
///  │  ├ <see cref="PrismApplicationBase.ConfigureRegionAdapterMappings"/>
///  │  ├ <see cref="PrismApplicationBase.ConfigureDefaultRegionBehaviors"/>
///  │  ├ <see cref="PrismApplicationBase.RegisterFrameworkExceptionTypes"/>
///  │  ├ <see cref="PrismApplicationBase.CreateShell"/>
///  │  ├ <see cref="PrismApplicationBase.InitializeShell"/>
///  │  └ <see cref="PrismApplicationBase.InitializeModules"/>
///  └ <see cref="PrismApplicationBase.OnInitialized"/>
/// </remarks>
public partial class App : PrismApplication
{
    private string _identifier;
    private Window _initialWindow;
    private IEnumerable<string> _commandLineArgs = Enumerable.Empty<string>();

    /// <summary>
    /// ロガー
    /// </summary>
    public ILoggerFacade Logger { get; }

    /// <summary>
    /// プロダクト情報
    /// </summary>
    public AppProductInfo ProductInfo { get; }

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    public App()
    {
        // 関連するインスタンスを生成する
        this.Logger = new CompositeLogger(
            new DebugLogger(),
            new AppLogger()
            {
                PublisherType = typeof(ILoggerFacadeExtensions),
                ConfigurationFactory = () =>
                {
                    var computerInfo = new ComputerInfo();
                    var headerText = new StringBuilder();
                    headerText.AppendLine("************************************************************");
                    headerText.AppendLine($"{this.ProductInfo.Product} - ${{var:CTG}} Log");
                    headerText.AppendLine($"CLR: {Environment.Version}");
                    headerText.AppendLine($"OS : {Environment.OSVersion}");
                    headerText.AppendLine("CPU: ${environment:PROCESSOR_IDENTIFIER}, ${environment:PROCESSOR_ARCHITECTURE}, ${environment:NUMBER_OF_PROCESSORS}-core");
                    headerText.AppendLine($"MEM: {computerInfo.TotalPhysicalMemory / 1048576} MB");
                    headerText.AppendLine("************************************************************");

                    var header = new NLog.Layouts.CsvLayout
                    {
                        Delimiter = NLog.Layouts.CsvColumnDelimiterMode.Tab,
                        Quoting = NLog.Layouts.CsvQuotingMode.Nothing
                    };
                    header.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, headerText.ToString()));

                    var layout = new NLog.Layouts.CsvLayout
                    {
                        Delimiter = NLog.Layouts.CsvColumnDelimiterMode.Tab,
                        Quoting = NLog.Layouts.CsvQuotingMode.Nothing,
                        Header = header
                    };
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${longdate}"));
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${environment-user}"));
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, $"{this.ProductInfo.Version}"));
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${processid}"));
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${threadid}"));
                    layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${message}"));

                    var file = new NLog.Targets.FileTarget
                    {
                        Encoding = Encoding.UTF8,
                        Footer = "${newline}",
                        FileName = "${var:DIR}/${var:CTG}.log",
                        ArchiveFileName = "${var:DIR}/archive/{#}.${var:CTG}.log",
                        ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                        ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                        MaxArchiveFiles = 10,
                        Layout = layout
                    };

                    var memory = new NLog.Targets.MemoryTarget
                    {
                        Layout = layout
                    };

                    var config = new NLog.Config.LoggingConfiguration();
                    config.AddTarget(nameof(file), file);
                    config.AddTarget(nameof(memory), memory);
                    config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, file));
                    config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, memory));
                    config.Variables.Add("DIR", this.ProductInfo.LogDirectoryPath);
                    return config;
                },
                CreateLoggerHook = (logger, category) =>
                {
                    // ログの種類ごとにファイルを切り替える
                    logger.Factory.Configuration.Variables.Add("CTG", category.ToString());
                    logger.Factory.ReconfigExistingLoggers();
                },
            });
        this.ProductInfo = new AppProductInfo();

        // イベントを購読する
        UnhandledExceptionObserver.Observe(this, this.Logger, this.ProductInfo);
        SystemEvents.SessionEnding += this.SystemEvents_SessionEnding;
    }

    /// <summary>
    /// アプリケーションの開始時に行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    protected override void OnStartup(StartupEventArgs e)
    {
        this._identifier = $"__{this.ProductInfo.Company}:{this.ProductInfo.Product}:{this.ProductInfo.Version}__";
        this._commandLineArgs = e.Args;

        this.Logger.Log($"アプリケーションを開始しました。", Category.Info);
        this.Logger.Debug($"アプリケーションを開始しました。: Args=[{string.Join(", ", e.Args)}]");
        base.OnStartup(e);
    }

    /// <summary>
    /// View と ViewModel のワイヤリングを構成します。
    /// </summary>
    /// <remarks>
    /// このメソッドをオーバーライドすることで、ViewModel のインスタンスファクトリーを置き換えることができます。
    /// </remarks>
    [LogInterceptor]
    protected override void ConfigureViewModelLocator()
    {
        // ViewModelLocator はいくつかの方法で ViewModel のインスタンスの取得を試行しています。
        // A. 生成済みのインスタンスを取得する
        //   -> ViewModelLocationProvider._factories に ViewModel のインスタンスが存在すればそれを取得する。
        // B. 新しいインスタンスを生成する
        //   a. ViewModelLocationProvider._typeFactories に ViewModel の型が登録されていればそれを取得する。
        //   b. ViewModelLocationProvider._defaultViewTypeToViewModelTypeResolver によって ViewModel の型を推定する。
        //   -> ViewModelLocationProvider._defaultViewModelFactory または ViewModelLocationProvider._defaultViewModelFactoryWithViewParameter によって
        //      ViewModel の型からインスタンスを生成する。

        base.ConfigureViewModelLocator();

        // デバッグのためのタイプリゾルバーを設定する
        // 既定で用意される ViewModelLocationProvider._defaultViewTypeToViewModelTypeResolver を置き換える
        ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(PrismConverter.ViewTypeToViewModelType);

        // 多言語化の設定を一元化するためのインスタンスファクトリーを設定する
        // PrismInitializationExtensions.ConfigureViewModelLocator() で設定される既定のファクトリーメソッドを置き換える
        ViewModelLocationProvider.SetDefaultViewModelFactory((view, viewModelType) =>
        {
            // View に対して多言語化の初期設定を行う
            if (view is DependencyObject obj)
            {
                ResxLocalizationProvider.SetDefaultAssembly(obj, obj.GetType().Assembly.GetName().Name);
                ResxLocalizationProvider.SetDefaultDictionary(obj, "Resources");
            }

            // ViewModel のインスタンスを生成する
            return this.Container.Resolve(viewModelType);
        });
    }

    /// <summary>
    /// DI コンテナのインスタンスを生成します。
    /// </summary>
    /// <returns>DI コンテナ</returns>
    /// <remarks>
    /// このメソッドをオーバーライドすることで、
    /// <see cref="UnityContainerExtension"/> に渡される <see cref="IUnityContainer"/> のインスタンスを置き換えることができます。
    /// </remarks>
    [LogInterceptor]
    protected override IContainerExtension CreateContainerExtension()
    {
        var container = new UnityContainer();
        container.EnableDebugDiagnostic();
        return new UnityContainerExtension(container);
    }

    /// <summary>
    /// DI コンテナにオブジェクトを追加します。
    /// </summary>
    /// <param name="containerRegistry">DI コンテナの登録インターフェース</param>
    /// <remarks>
    /// このメソッドをオーバーライドすることで、アプリケーション固有のオブジェクトを DI コンテナに追加することができます。
    /// なお、既定のシングルトンや型マッピングは <see cref="PrismInitializationExtensions.RegisterRequiredTypes"/> にて設定されます。
    /// </remarks>
    [LogInterceptor]
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // シングルトン
        containerRegistry.RegisterSingleton<Models.SettingsModel>();
        containerRegistry.RegisterSingleton<Models.SyntaxService>();
        containerRegistry.RegisterSingleton<ViewModels.SharedProperties>();
        containerRegistry.RegisterSingleton<ICommonDialogService, CommonDialogService>();
        containerRegistry.RegisterInstance<IProductInfo>(this.ProductInfo);
        containerRegistry.RegisterInstance(this.ProductInfo);
        containerRegistry.RegisterInstance(this.Logger);

        // ダイアログ
        containerRegistry.RegisterDialogWindow<Views.PrismDialogWindow>();
        containerRegistry.RegisterDialog<Views.Dialogs.NotifyDialog, ViewModels.Dialogs.NotifyDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.WarnDialog, ViewModels.Dialogs.WarnDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.ConfirmDialog, ViewModels.Dialogs.ConfirmDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.CancelableConfirmDialog, ViewModels.Dialogs.CancelableConfirmDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.ChangePomodoroTimerDialog, ViewModels.Dialogs.ChangePomodoroTimerDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.ChangeLineDialog, ViewModels.Dialogs.ChangeLineDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.ChangeEncodingDialog, ViewModels.Dialogs.ChangeEncodingDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.ChangeSyntaxDialog, ViewModels.Dialogs.ChangeSyntaxDialogViewModel>();
        containerRegistry.RegisterDialog<Views.Dialogs.SelectDiffFilesDialog, ViewModels.Dialogs.SelectDiffFilesDialogViewModel>();

        // ファクトリ
        containerRegistry.Register<ViewModels.TextEditorViewModel>();
        containerRegistry.Register<Views.MainWindow.InterTabClientWrapper>();
    }

    /// <summary>
    /// アプリケーションのメインウィンドウを生成します。
    /// </summary>
    /// <returns>ウィンドウのインスタンス</returns>
    /// <remarks>
    /// このメソッドが返却するインスタンスは <see cref="InitializeShell"/> に渡されます。
    /// null を返却した場合 <see cref="InitializeShell"/> は呼び出されません。
    /// </remarks>
    [LogInterceptor]
    protected override Window CreateShell()
    {
        // 多重起動の検証を行う
        var validateMultipleLaunch = Task.Run(() =>
        {
            var process = Process.GetCurrentProcess();
            var handle = this.GetOtherProcessHandle(process);
            if (handle.IsNull == false)
            {
                // 起動中のプロセスが見つかった場合はコマンドライン引数を引き渡す
                this.SendData(process, handle, this._commandLineArgs);
                return false;
            }
            return true;
        });

        // 設定情報を初期化する
        this.Container.Resolve<Models.SettingsModel>().Load();

        // ワークスペースを生成する
        var shell = this.Container.Resolve<Workspace>();

        // 初期ウィンドウを生成する
        var window = this.Container.Resolve<Views.MainWindow>();
        window.IsInitialWindow = true;

        // 多重起動中の場合は本プロセスを終了する
        validateMultipleLaunch.Wait();
        if (validateMultipleLaunch.Result == false)
        {
            this.Shutdown(0);
            return null;
        }

        this._initialWindow = window;
        return shell;
    }

    /// <summary>
    /// メインウィンドウの初期処理を行います。
    /// </summary>
    /// <param name="shell">ウィンドウのインスタンス</param>
    /// <remarks>
    /// このメソッドをオーバーライドすることで、MainWindow の表示前に処理を割り込ませることができます。
    /// なお、引数で受け取るインスタンスは <see cref="PrismApplicationBase.InitializeShell"/> によって MainWindow に設定されます。
    /// </remarks>
    [LogInterceptor]
    protected override void InitializeShell(Window shell)
    {
        var settings = this.Container.Resolve<Models.SettingsModel>();

        // ワークスペースを初期化する
        base.InitializeShell(shell);
        shell.Title = this._identifier;
        shell.Closed += (sender, e) => settings.Save();

        // バージョンの更新状況を確認する
        var isDifferentVersion = settings.IsDifferentVersion;
        if (isDifferentVersion)
        {
            this.Logger.Debug($"アプリケーションのバージョンが更新されました。: Old={settings.Version}, New={this.ProductInfo.Version}");
            settings.Save();
        }

        // シンタックス定義を初期化する
        this.Container.Resolve<Models.SyntaxService>().CreateDefinitionFiles(isDifferentVersion);

        // 一時フォルダを生成する
        this.ProductInfo.CreateTempDirectory();
        this.Logger.Debug($"このプロセスが使用する一時フォルダのパスが決定しました。: Path={this.ProductInfo.TempDirectoryPath}");

        // 残存する一時フォルダに関する処理を行う
        var cachedDirectories = new DirectoryInfo(this.ProductInfo.Temporary)
            .EnumerateDirectories()
            .Where(i => i.FullName != this.ProductInfo.TempDirectoryPath)
            .Select(i =>
            {
                var result = DateTime.TryParseExact(Path.GetFileName(i.FullName), "yyyyMMddHHmmssfff", CultureInfo.CurrentCulture, DateTimeStyles.None, out var value);
                return (result, value, info: i);
            });
        if (cachedDirectories.Any())
        {
            const int LOOP_DELAY = 500;
            try
            {
                // 残存する一時フォルダの隠し属性を外す
                foreach (var info in cachedDirectories.Select(t => t.info))
                    info.Attributes &= ~FileAttributes.Hidden;

                // 指定の期間を超えたものを削除する
                var basis = Process.GetCurrentProcess().StartTime.AddDays(-1 * AppSettingsReader.CacheLifetime);
                foreach (var info in cachedDirectories
                    .Where(t => t.result == false || t.value < basis || t.info.EnumerateFileSystemInfos().Any() == false)
                    .Select(t => t.info))
                {
                    Directory.Delete(info.FullName, true);
                    while (Directory.Exists(info.FullName))
                        Thread.Sleep(LOOP_DELAY);
                }
                this.Logger.Debug($"保存期限を過ぎた一時フォルダを削除しました。");
            }
            catch (Exception ex)
            {
                this.Logger.Log("保存期限を過ぎた一時フォルダの削除に失敗しました。", Category.Warn, ex);
            }
        }

        // 初期ウィンドウの表示直後の処理を定義する
        void window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = (Views.MainWindow)sender;
            window.Loaded -= window_Loaded;

            // コマンドライン引数を引き渡す
            if (this._commandLineArgs.Any())
                _ = window.ViewModel.InvokeLoad(this._commandLineArgs);
        }
        void window_ContentRendered(object sender, EventArgs e)
        {
            var window = (Views.MainWindow)sender;
            window.ContentRendered -= window_ContentRendered;

            // バージョンが更新された場合は通知する
            if (isDifferentVersion)
            {
                window.ViewModel.AboutCommand.Execute();
                ViewModels.IDialogServiceExtensions.ToastNotify(
                    window.ViewModel.DialogService,
                    string.Format(
                        MyPad.Properties.Resources.Message_NotifyWelcome,
                        this.ProductInfo.Product,
                        $"{this.ProductInfo.Version.Major}.{this.ProductInfo.Version.Minor}.{this.ProductInfo.Version.Build}"));
            }

            // （改めて）残存する一時フォルダをチェックし、存在する場合は通知する
            if (cachedDirectories.Any())
            {
                ViewModels.IDialogServiceExtensions.Notify(
                    window.ViewModel.DialogService,
                    MyPad.Properties.Resources.Message_NotifyCachedFilesRemain);
                Process.Start("explorer.exe", this.ProductInfo.Temporary);
            }
        }
        this._initialWindow.Loaded += window_Loaded;
        this._initialWindow.ContentRendered += window_ContentRendered;
    }

    /// <summary>
    /// 初期化のフローにおける最後の処理を行います。
    /// </summary>
    /// <remarks>
    /// このメソッドをオーバーライドすることで、Prism の初期化処理の最終ステップに処理を割り込ませることができます。
    /// なお、<see cref="PrismApplicationBase.OnInitialized"/> によって MainWindow.Show() が呼び出されます。
    /// </remarks>
    [LogInterceptor]
    protected override void OnInitialized()
    {
        base.OnInitialized();
        this._initialWindow?.Show();
    }

    /// <summary>
    /// アプリケーションの終了時に行う処理を定義します。
    /// </summary>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    protected override void OnExit(ExitEventArgs e)
    {
        // 一時フォルダに関する処理を行う
        if (Directory.Exists(this.ProductInfo.TempDirectoryPath))
        {
            const int LOOP_DELAY = 500;
            try
            {
                Directory.Delete(this.ProductInfo.TempDirectoryPath, true);
                while (Directory.Exists(this.ProductInfo.TempDirectoryPath))
                    Thread.Sleep(LOOP_DELAY);
                this.Logger.Debug($"一時フォルダを削除しました。: Path={this.ProductInfo.TempDirectoryPath}");
            }
            catch (Exception ex)
            {
                this.Logger.Log($"一時フォルダの削除に失敗しました。: Path={this.ProductInfo.TempDirectoryPath}", Category.Warn, ex);
            }
        }

        // アプリケーションを終了する
        base.OnExit(e);

        // イベントの購読を解除する
        SystemEvents.SessionEnding -= this.SystemEvents_SessionEnding;
        UnhandledExceptionObserver.Unobserve(this);

        // ログに記録する
        this.Logger.Log($"アプリケーションを終了しました。", Category.Info);
        this.Logger.Debug($"アプリケーションを終了しました。: ExitCode={e.ApplicationExitCode}");
    }

    /// <summary>
    /// ユーザがサインアウトまたはシャットダウンするときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionEndReasons.Logoff:
                this.Logger.Log($"ユーザがサインアウトしようとしています。OS によってアプリケーションは強制終了されます。", Category.Info);
                break;
            case SessionEndReasons.SystemShutdown:
                this.Logger.Log($"ユーザがシャットダウンしようとしています。OS によってアプリケーションは強制終了されます。", Category.Info);
                break;
        }
        this.Logger.Debug($"ユーザが OS のセッションを終了しようとしています。OS によってアプリケーションは強制終了されます。: SessionEndReasons={e.Reason}");
    }

    /// <summary>
    /// 別プロセスで起動中の同一アプリケーションのウィンドウハンドルを取得します。
    /// </summary>
    /// <param name="sourceProcess">比較元のプロセス</param>
    /// <returns>ウィンドウハンドル</returns>
    [LogInterceptor]
    private HWND GetOtherProcessHandle(Process sourceProcess)
    {
        // 本アプリはタスクバー上の通知アイコンを内包した仮ウィンドウが MainWindow に指定されている。
        // このウィンドウは描画されないため Process.MainWindowHandle からハンドルを取得できない。
        // すべてのハンドルを列挙し、ウィンドウテキストが一致するハンドルから特定する。

        var (hWnd, lpdwProcessId) = (HWND.NULL, 0u);
        var result = User32.EnumWindows(new User32.EnumWindowsProc((_hWnd, _lParam) =>
        {
            try
            {
                // プロセスの情報を比較する
                _ = User32.GetWindowThreadProcessId(_hWnd, out var _lpdwProcessId);
                if (sourceProcess.Id == _lpdwProcessId)
                    return true;
                var process = Process.GetProcessById((int)_lpdwProcessId);
                if (sourceProcess.ProcessName != process.ProcessName)
                    return true;

                // ウィンドウテキストを比較する
                // 本アプリケーションでは実質的に識別子として使用される
                var lpString = new StringBuilder(256);
                _ = User32.GetWindowText(_hWnd, lpString, lpString.Capacity);
                if (lpString.ToString().Contains(this._identifier) == false)
                    return true;

                // ハンドルを保持する
                (hWnd, lpdwProcessId) = (_hWnd, _lpdwProcessId);
                return false;
            }
            catch
            {
                return true;
            }
        }), IntPtr.Zero);

        if (result)
            this.Logger.Debug($"起動中の同一アプリケーションは存在しませんでした。: ProcessName={sourceProcess?.ProcessName}");
        else
            this.Logger.Debug($"起動中の同一アプリケーションのウィンドウハンドルを取得しました。: ProcessName={sourceProcess?.ProcessName}, lpdwProcessID={lpdwProcessId}, hWnd=0x{hWnd.DangerousGetHandle():X)}");
        return hWnd;
    }

    /// <summary>
    /// 指定されたウィンドウハンドルに対してデータを送信します。
    /// </summary>
    /// <param name="sourceProcess">送信元のプロセス</param>
    /// <param name="destinationHandle">送信先のウィンドウハンドル</param>
    /// <param name="data">送信されるデータ</param>
    /// <param name="separator">データの区切りを示す文字</param>
    /// <returns>ウィンドウプロシージャの戻り値</returns>
    [LogInterceptor]
    private IntPtr SendData(Process sourceProcess, HWND destinationHandle, IEnumerable<string> data, char separator = '\t')
    {
        var lpData = data.Any() ? string.Join(separator, data) : string.Empty;
        var structure = new Win32.COPYDATASTRUCT
        {
            dwData = IntPtr.Zero,
            cbData = Encoding.UTF8.GetByteCount(lpData) + 1,
            lpData = lpData,
        };

        var lParam = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
        Marshal.StructureToPtr(structure, lParam, false);
        var msg = User32.WindowMessage.WM_COPYDATA;
        this.Logger.Debug($"ウィンドウメッセージを送信します。: hWnd=0x{destinationHandle.DangerousGetHandle():X}, msg={msg}, data=[{string.Join(", ", data)}]");

        return User32.SendMessage(destinationHandle, (uint)msg, sourceProcess.Handle, lParam);
    }

    /// <summary>
    /// <see cref="QuickConverter"/> の初期設定を行います。
    /// </summary>
    [ModuleInitializer]
    public static void ConfigureQuickConverter()
    {
        try
        {
#pragma warning disable IDE0049
            EquationTokenizer.AddNamespace(typeof(System.Object));                           // System                  : System.Runtime.dll
            EquationTokenizer.AddNamespace(typeof(System.IO.Path));                          // System.IO               : System.Runtime.dll
            EquationTokenizer.AddNamespace(typeof(System.Text.Encoding));                    // System.Text             : System.Runtime.dll
            EquationTokenizer.AddNamespace(typeof(System.Reflection.Assembly));              // System.Reflection       : System.Runtime.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.Point));                    // System.Windows          : WindowsBase.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.UIElement));                // System.Windows          : PresentationCore.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.Window));                   // System.Windows          : PresentationFramework.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.Input.Key));                // System.Windows.Input    : WindowsBase.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.Input.Cursor));             // System.Windows.Input    : PresentationCore.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.Input.KeyboardNavigation)); // System.Windows.Input    : PresentationFramework.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.Controls.Control));         // System.Windows.Controls : PresentationFramework.dll
            EquationTokenizer.AddNamespace(typeof(System.Windows.Media.Brush));              // System.Windows.Media    : PresentationFramework.dll
            EquationTokenizer.AddNamespace(typeof(System.Linq.Enumerable));                  // System.Linq             : System.Linq.dll
            EquationTokenizer.AddNamespace(typeof(ControlzEx.Theming.Theme));                // ControlzEx.Theming      : ControlzEx.dll
            EquationTokenizer.AddExtensionMethods(typeof(System.Linq.Enumerable));
            EquationTokenizer.AddExtensionMethods(typeof(MyPad.KeyGestureExtensions));
#pragma warning restore IDE0049
        }
        catch (Exception e)
        {
            Debug.WriteLine($"ERROR: QuickConverter の初期設定に失敗しました。: Message={e.Message}");
            throw;
        }
    }
}
