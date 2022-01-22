using MyBase;
using MyBase.Logging;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MyPad
{
    /// <summary>
    /// ハンドルされていない例外の発生を監視します。
    /// </summary>
    public static class UnhandledExceptionObserver
    {
        private static bool _isObserved;
        private static bool _canOpenConfirm = true;

        private static ILoggerFacade Logger { get; set; }
        private static IProductInfo ProductInfo { get; set; }

        /// <summary>
        /// 指定されたアプリケーションに対してハンドルされていない例外の発生を監視します。
        /// </summary>
        /// <param name="application">アプリケーション</param>
        /// <param name="logger">ロガー</param>
        /// <param name="productInfo">プロダクト情報</param>
        public static void Observe(Application application, ILoggerFacade logger, IProductInfo productInfo)
        {
            if (_isObserved)
                throw new InvalidOperationException();

            _isObserved = true;

            Logger = logger;
            ProductInfo = productInfo;

            application.DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// 指定されたアプリケーションに対してハンドルされていない例外の発生を監視を解除します。
        /// </summary>
        /// <param name="application">アプリケーション</param>
        public static void Unobserve(Application application)
        {
            _isObserved = false;

            Logger = null;
            ProductInfo = null;

            application.DispatcherUnhandledException -= App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// UI スレッドで例外が発生したときに行う処理を定義します。
        /// </summary>
        /// <param name="sender">イベントの発生源</param>
        /// <param name="e">イベントの情報</param>
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ContinueOrExit(e.Exception);
        }

        /// <summary>
        /// バックグラウンドタスクで例外が発生したときに行う処理を定義します。
        /// </summary>
        /// <param name="sender">イベントの発生源</param>
        /// <param name="e">イベントの情報</param>
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            ContinueOrExit(e.Exception?.InnerException);
        }

        /// <summary>
        /// 捕捉されていない例外が発生したときに行う処理を定義します。
        /// </summary>
        /// <param name="sender">イベントの発生源</param>
        /// <param name="e">イベントの情報</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Terminate(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// 例外の発生を通知し、続行の可否をユーザに委ねます。
        /// </summary>
        /// <param name="e">例外の情報</param>
        private static void ContinueOrExit(Exception e)
        {
            var guid = Guid.NewGuid().ToString()[..6];
            try
            {
                Logger?.Log($"[{guid}] ハンドルされていない例外が発生しました。", Category.Fatal, e);
            }
            catch
            {
            }

            if (_canOpenConfirm == false)
                return;

            try
            {
                _canOpenConfirm = false;

                var message = new StringBuilder();
                message.AppendLine($"ハンドルされていない例外が発生しました。");
                message.AppendLine($"エラーを無視してプログラムを続行しますか？");
                message.AppendLine();
                message.AppendLine($"{e?.Message}");
                if (MessageBox.Show(message.ToString(), ProductInfo?.Product ?? "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _canOpenConfirm = true;
                    return;
                }
            }
            catch
            {
            }

            Terminate(e, guid);
        }

        /// <summary>
        /// 例外の発生を通知し、アプリケーションを終了します。
        /// </summary>
        /// <param name="e">例外の情報</param>
        /// <param name="guid">GUID</param>
        private static void Terminate(Exception e, string guid = null)
        {
            try
            {
                if (string.IsNullOrEmpty(guid))
                    Logger?.Log("アプリケーションを終了します。", Category.Fatal);
                else
                    Logger?.Log($"[{guid}] アプリケーションを終了します。", Category.Fatal);
            }
            catch
            {
            }

            try
            {
                var message = new StringBuilder();
                message.AppendLine($"ハンドルされていない例外が発生しました。");
                message.AppendLine($"アプリケーションを終了します。");
                message.AppendLine();
                message.AppendLine($"{e?.GetType()?.Name}");
                message.AppendLine($"[Method] {e?.TargetSite?.DeclaringType?.Name}.{e?.TargetSite?.Name}");
                message.AppendLine($"[Module] {e?.TargetSite?.Module?.Name}");
                message.AppendLine();
                message.AppendLine($"{e?.Message}");
                MessageBox.Show(message.ToString(), ProductInfo?.Product ?? "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
            }

            Environment.Exit(1);
        }
    }
}
