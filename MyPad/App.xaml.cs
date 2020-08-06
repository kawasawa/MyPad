using Plow;
using Plow.Wpf.CommonDialogs;
using Prism.Ioc;
using Prism.Logging;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using Prism.Unity;
using QuickConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Unity;
using Vanara.PInvoke;
using WPFLocalizeExtension.Providers;

namespace MyPad
{
    /// <summary>
    /// アプリケーションのエントリーポイントとなるクラスを表します。
    /// </summary>
    public partial class App : PrismApplication
    {
        /// <summary>
        /// ロガーを取得します。
        /// </summary>
        public ILoggerFacade Logger { get; }

        /// <summary>
        /// プロダクト情報を取得します。
        /// </summary>
        public IProductInfo ProductInfo { get; }

        /// <summary>
        /// プロセス情報を取得します。
        /// </summary>
        public Process CurrentProcess { get; }

        /// <summary>
        /// コマンドライン引数を取得します。
        /// </summary>
        public IEnumerable<string> CommandLineArgs { get; private set; }

        /// <summary>
        /// アプリケーションの識別子を取得します。
        /// </summary>
        public string Identifier => $"__{this.ProductInfo.Company}:{this.ProductInfo.Product}:{this.ProductInfo.Version}__";

        /// <summary>
        /// ログファイルが出力されるディレクトリのパスを取得します。
        /// </summary>
        public string LogDirectoryPath => Path.Combine(this.ProductInfo.Local, "log");

        /// <summary>
        /// このクラスの新しいインスタンスを生成します。
        /// </summary>
        public App()
        {
            this.Logger = new CompositeLogger(
                new DebugLogger(),
                new NLogger() {
                    PublisherType = typeof(ILoggerFacadeExtension),
                    ConfigurationFactory = () =>
                    {
                        var layout = new NLog.Layouts.CsvLayout();
                        layout.Delimiter = NLog.Layouts.CsvColumnDelimiterMode.Tab;
                        layout.Quoting = NLog.Layouts.CsvQuotingMode.Nothing;
                        layout.WithHeader = false;
                        layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${date:format=yyyy/MM/dd HH\\:mm\\:ss}"));
                        layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${callsite}"));
                        layout.Columns.Add(new NLog.Layouts.CsvColumn(string.Empty, "${message}"));

                        var target = new NLog.Targets.FileTarget("log");
                        target.Encoding = Encoding.UTF8;
                        target.Footer = "${newline}";
                        target.FileName = "${var:DIR}/${var:CTG}.log";
                        target.ArchiveFileName = "${var:DIR}/archive/{#}.${var:CTG}.log";
                        target.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
                        target.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date;
                        target.MaxArchiveFiles = 10;
                        target.Layout = layout;

                        var config = new NLog.Config.LoggingConfiguration();
                        config.AddTarget(target);
                        config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, target));
                        return config;
                    },
                    CreateLoggerHook = (logger, category) =>
                    {
                        var categoryName = category switch
                        {
                            Category.Debug => nameof(NLog.LogLevel.Debug),
                            Category.Info => nameof(NLog.LogLevel.Info),
                            Category.Warn => nameof(NLog.LogLevel.Warn),
                            Category.Exception => nameof(NLog.LogLevel.Error),
                            _ => nameof(NLog.LogLevel.Debug),
                        };
                        logger.Factory.Configuration.Variables.Add("DIR", this.LogDirectoryPath);
                        logger.Factory.Configuration.Variables.Add("CTG", categoryName);
                        logger.Factory.ReconfigExistingLoggers();
                    },
                });
            this.ProductInfo = new ProductInfo();
            this.CurrentProcess = Process.GetCurrentProcess();
            UnhandledExceptionObserver.Observe(this, this.Logger, this.ProductInfo);
            Initializer.InitEncoding();
            Initializer.InitQuickConverter();
        }

        /// <summary>
        /// アプリケーションの開始直後に行う処理を定義します。
        /// </summary>
        /// <param name="e">イベントの情報</param>
        [LogInterceptor]
        protected override void OnStartup(StartupEventArgs e)
        {
            this.CommandLineArgs = e.Args;
            this.Logger.Log($"アプリケーションを開始しました。: Process={this.CurrentProcess.Id}, Args=[{string.Join(", ", e.Args)}]", Category.Info);

            var handle = this.GetOtherProcessHandle(this.CurrentProcess);
            if (handle.IsNull == false)
            {
                this.SendValues(this.CurrentProcess, handle, e.Args);
                this.Shutdown(0);
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var info = new DirectoryInfo(this.ProductInfo.Temporary);
                    if (info.Exists == false)
                        return;

                    // 残存する一時ファイルのうち指定の日数を超えたものを削除する
                    var basis = DateTime.Now.AddDays(-1 * AppSettings.TempsLifetime);
                    info.EnumerateFiles()
                        .Where(i => i.LastWriteTime < basis)
                        .ForEach(i => File.Delete(i.FullName));
                    this.Logger.Log("一時ファイルを削除しました。(システム開始時)", Category.Debug);
                }
                catch (Exception ex)
                {
                    this.Logger.Log("一時ファイルの削除に失敗しました。(システム開始時)", Category.Warn, ex);
                }
            });

            base.OnStartup(e);
        }

        /// <summary>
        /// View から ViewModel を生成するための規則を定義します。
        /// </summary>
        [LogInterceptor]
        protected override void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory((view, viewModelType) => {
                // 多言語化の初期設定を行う
                if (view is DependencyObject obj)
                    Initializer.InitWPFLocalizeExtension(obj);

                // ViewModel のインスタンスを生成する
                var viewModel = this.Container.Resolve(viewModelType);
                this.Logger.Log($"ViewModel のインスタンスが生成されました。: Type={viewModelType.FullName}", Category.Debug);
                return viewModel;
            });
        }

        /// <summary>
        /// DI コンテナに登録されるオブジェクトを定義します。
        /// </summary>
        /// <param name="containerRegistry">DI コンテナ</param>
        [LogInterceptor]
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // シングルトン
            containerRegistry.RegisterInstance(this.Logger);
            containerRegistry.RegisterInstance(this.ProductInfo);
            containerRegistry.RegisterSingleton<ICommonDialogService, CommonDialogService>();
            containerRegistry.RegisterSingleton<Models.SettingsService>();
            containerRegistry.RegisterSingleton<Models.SyntaxService>();

            // ファクトリー
            containerRegistry.Register<ViewModels.TextEditorViewModel>();
            containerRegistry.Register<Views.MainWindow.InterTabClientWrapper>();

            // ダイアログ
            containerRegistry.RegisterDialogWindow<PrismDialogWindowWrapper>();
            containerRegistry.RegisterDialog<Views.Dialogs.NotifyDialog, ViewModels.Dialogs.NotifyDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.WarnDialog, ViewModels.Dialogs.WarnDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.ConfirmDialog, ViewModels.Dialogs.ConfirmDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.CancelableConfirmDialog, ViewModels.Dialogs.CancelableConfirmDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.ChangeLineDialog, ViewModels.Dialogs.ChangeLineDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.ChangeEncodingDialog, ViewModels.Dialogs.ChangeEncodingDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.ChangeSyntaxDialog, ViewModels.Dialogs.ChangeSyntaxDialogViewModel>();
            containerRegistry.RegisterDialog<Views.Dialogs.SelectDiffFilesDialog, ViewModels.Dialogs.SelectDiffFilesDialogViewModel>();
        }

        /// <summary>
        /// アプリケーションの起動時に表示されるメインウィンドウを生成します。
        /// </summary>
        /// <returns>ウィンドウのインスタンス</returns>
        [LogInterceptor]
        protected override Window CreateShell()
        {
            this.Container.Resolve<Models.SettingsService>().Load();
            this.Container.Resolve<Models.SyntaxService>().Initialize(this.Container.Resolve<Models.SettingsService>().IsDifferentVersion());
            this.Container.Resolve<IRegionManager>().Regions.CollectionChanged += (sender, e) =>
            {
                for (var i = 0; i < (e.NewItems?.Count ?? 0); i++)
                {
                    if (e.NewItems[i] is IRegion region)
                        this.Logger.Log($"Region が追加されました。: Name={region.Name}", Category.Debug);
                }
            };
            var shell = this.Container.Resolve<Views.Workspace>();
            shell.Title = this.Identifier;
            shell.Closed += (sender, e) => this.Container?.Resolve<Models.SettingsService>()?.Save();
            return shell;
        }

        /// <summary>
        /// アプリケーションの終了時に行う処理を定義します。
        /// </summary>
        /// <param name="e">イベントの情報</param>
        [LogInterceptor]
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            this.Logger.Log($"アプリケーションを終了しました。: Process={this.CurrentProcess.Id}, ExitCode={e.ApplicationExitCode}", Category.Info);
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

            var result = HWND.NULL;
            User32.EnumWindows(new User32.EnumWindowsProc((hWnd, _) =>
            {
                try
                {
                    // プロセスの情報を比較する
                    User32.GetWindowThreadProcessId(hWnd, out var lpdwProcessID);
                    var process = Process.GetProcessById((int)lpdwProcessID);
                    if (sourceProcess.Id == lpdwProcessID)
                        return true;
                    if (sourceProcess.ProcessName != process.ProcessName)
                        return true;

                    // ウィンドウテキストを比較する
                    // 本アプリケーションでは実質的に識別子として使用される
                    var lpString = new StringBuilder(256);
                    User32.GetWindowText(hWnd, lpString, lpString.Capacity);
                    if (lpString.ToString().Contains(this.Identifier) == false)
                        return true;

                    // ハンドルを保持する
                    result = hWnd;
                    return false;
                }
                catch
                {
                    return true;
                }
            }), IntPtr.Zero);
            return result;
        }

        /// <summary>
        /// 指定されたウィンドウハンドルに対してデータを送信します。
        /// </summary>
        /// <param name="sourceProcess">送信元のプロセス</param>
        /// <param name="destinationHandle">送信先のウィンドウハンドル</param>
        /// <param name="values">送信されるデータ</param>
        /// <param name="separator">データの区切りを示す文字</param>
        /// <returns>ウィンドウプロシージャの戻り値</returns>
        [LogInterceptor]
        private IntPtr SendValues(Process sourceProcess, HWND destinationHandle, IEnumerable<string> values, char separator = '\t')
        {
            var data = values.Any() ? string.Join(separator, values) : string.Empty;
            var structure = new COPYDATASTRUCT
            {
                dwData = IntPtr.Zero,
                cbData = Encoding.UTF8.GetByteCount(data) + 1,
                lpData = data,
            };

            var lParam = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            Marshal.StructureToPtr(structure, lParam, false);

            var result = User32.SendMessage(destinationHandle, (uint)User32.WindowMessage.WM_COPYDATA, sourceProcess.Handle, lParam);
            this.Logger.Log($"メッセージを送信しました。: hWnd=[0x{ destinationHandle.DangerousGetHandle().ToString("X")}], Values=[{string.Join(", ", values)}]", Category.Debug);
            return result;
        }

        /// <summary>
        /// 初期化処理を行うためのメソッドを提供します。
        /// </summary>
        private static class Initializer
        {
            /// <summary>
            /// 文字コードの初期設定を行います。
            /// </summary>
            public static void InitEncoding()
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }

            /// <summary>
            /// <see cref="QuickConverter"/> の初期設定を行います。
            /// </summary>
            public static void InitQuickConverter()
            {
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
                EquationTokenizer.AddExtensionMethods(typeof(System.Linq.Enumerable));           // System.Linq             : System.Linq.dll
            }

            /// <summary>
            /// 指定された View のインスタンスに対する <see cref="WPFLocalizeExtension"/> の初期設定を行います。
            /// </summary>
            /// <param name="view">View のインスタンス</param>
            public static void InitWPFLocalizeExtension(DependencyObject view)
            {
                ResxLocalizationProvider.SetDefaultAssembly(view, nameof(MyPad));
                ResxLocalizationProvider.SetDefaultDictionary(view, nameof(MyPad.Properties.Resources));
            }
        }

        /// <summary>
        /// Prism によって表示されるダイアログウィンドウの基底クラスを置き換えます。
        /// </summary>
        private class PrismDialogWindowWrapper : MahApps.Metro.Controls.MetroWindow, IDialogWindow
        {
            IDialogResult IDialogWindow.Result { get; set; }

            public PrismDialogWindowWrapper()
            {
                this.Loaded += this.Window_Loaded;
            }

            private void Window_Loaded(object sender, RoutedEventArgs e)
            {
                if (this.DataContext is IDialogAware dialogAware)
                    this.Title = dialogAware.Title;
                this.Loaded -= this.Window_Loaded;
            }
        }
    }
}
