using Hardcodet.Wpf.TaskbarNotification;
using MyBase;
using MyBase.Logging;
using MyPad.PubSub;
using MyPad.ViewModels;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using Unity;
using Vanara.PInvoke;

namespace MyPad.Views;

/// <summary>
/// アプリケーションのメインプロセスに相当するウィンドウを表します。
/// </summary>
public partial class Workspace : Window
{
    #region インジェクション

    // Constructor Injection
    public IEventAggregator EventAggregator { get; set; }

    // Dependency Injection
    [Dependency]
    public IContainerExtension Container { get; set; }
    [Dependency]
    public IRegionManager RegionManager { get; set; }
    [Dependency]
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public IProductInfo ProductInfo { get; set; }
    [Dependency]
    public SharedDataStore SharedDataStore { get; set; }

    #endregion

    #region プロパティ

    private HwndSource _handleSource;

    #endregion

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="eventAggregator">イベントアグリゲーター</param>
    [InjectionConstructor]
    [LogInterceptor]
    public Workspace(IEventAggregator eventAggregator)
    {
        this.InitializeComponent();
        this.EventAggregator = eventAggregator;

        void createWindow() => this.CreateWindow().Show();
        this.EventAggregator.GetEvent<CreateWindowEvent>().Subscribe(createWindow);
        void showBalloon((string title, string message) payload)
        {
            if (this.TaskbarIcon.IsVisible)
            {
                this.TaskbarIcon.ShowBalloonTip(payload.title, payload.message, BalloonIcon.Info);
                return;
            }

            var binding = BindingOperations.GetBinding(this.TaskbarIcon, TaskbarIcon.VisibilityProperty);
            this.TaskbarIcon.Visibility = Visibility.Visible;
            this.TaskbarIcon.ShowBalloonTip(payload.title, payload.message, BalloonIcon.Info);
            this.TaskbarIcon.SetBinding(TaskbarIcon.VisibilityProperty, binding);
        };
        this.EventAggregator.GetEvent<RaiseBalloonEvent>().Subscribe(showBalloon);
    }

    /// <summary>
    /// 新しい <see cref="MainWindow"/> のインスタンスを生成します。
    /// </summary>
    /// <param name="regionManager">リージョンマネージャー</param>
    /// <returns>生成された <see cref="MainWindow"/> のインスタンス</returns>
    [LogInterceptor]
    private MainWindow CreateWindow(IRegionManager regionManager = null)
    {
        var window = this.Container.Resolve<MainWindow>((typeof(IRegionManager), regionManager ?? this.RegionManager?.CreateRegionManager()));
        this.Logger.Log($"ウィンドウを生成しました。win#{((MainWindowViewModel)window.DataContext).Sequense}", Category.Info);
        return window;
    }

    /// <summary>
    /// ウィンドウがロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // フックメソッドを登録する
        this._handleSource = this.GetHwndSource();
        this._handleSource.AddHook(this.WndProc);

        // このインスタンスのウィンドウは非表示にする
        // (タスクバーに常駐するだけのウィンドウ)
        this.Hide();

        // 初期ウィンドウを生成する
        var view = this.CreateWindow(this.RegionManager);
        view.IsInitialWindow = true;
        if (view.ViewModel.Settings.IsDifferentVersion)
        {
            void view_ContentRendered(object sender, EventArgs e)
            {
                view.ContentRendered -= view_ContentRendered;
                view.ViewModel.AboutCommand.Execute();

                var info = view.ViewModel.ProductInfo;
                view.ViewModel.DialogService.ToastNotify(string.Format(
                    Properties.Resources.Message_NotifyWelcome,
                    info.Product,
                    $"{info.Version.Major}.{info.Version.Minor}.{info.Version.Build}"));
            }
            view.ContentRendered += view_ContentRendered;
        }

        // 残存する一時フォルダをチェックする
        if (this.SharedDataStore.CachedDirectories.Any())
        {
            void view_ContentRendered(object sender, EventArgs e)
            {
                view.ContentRendered -= view_ContentRendered;
                view.ViewModel.DialogService.Notify(Properties.Resources.Message_NotifyCachedFilesRemain);
                Process.Start("explorer.exe", this.ProductInfo.Temporary);
            }
            view.ContentRendered += view_ContentRendered;
        }

        // コマンドライン引数を渡す
        var args = this.SharedDataStore.CommandLineArgs;
        if (args.Any())
            _ = view.ViewModel.InvokeLoad(args);

        // 初期ウィンドウを表示する
        view.Show();
    }

    /// <summary>
    /// ウィンドウが閉じられたあとに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_Closed(object sender, EventArgs e)
    {
        this._handleSource?.RemoveHook(this.WndProc);
        this.Descendants().OfType<TaskbarIcon>().ForEach(t => t.Dispose());
    }

    /// <summary>
    /// DataContext が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        void viewModel_Disposed(object sender, EventArgs e)
        {
            ((ViewModelBase)sender).Disposed -= viewModel_Disposed;
            this.Dispatcher.InvokeAsync(() => this.Close());
        }

        if (e.OldValue is ViewModelBase oldViewModel)
            oldViewModel.Disposed -= viewModel_Disposed;
        if (e.NewValue is ViewModelBase newViewModel)
            newViewModel.Disposed += viewModel_Disposed;
    }

    /// <summary>
    /// タスクバーアイコン上でコンテキストメニューが開かれたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void TaskbarIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e)
    {
        this.WindowListItem.Items.Clear();
        var windows = MvvmHelper.GetMainWindows();
        for (var i = 0; i < windows.Count(); i++)
            this.WindowListItem.Items.Add(new MenuItem { DataContext = windows.ElementAt(i) });
    }

    /// <summary>
    /// タスクバーアイコン上でダブルクリックされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        // ウィンドウが存在する場合はそれらをフォアグラウンドに移動する
        // ウィンドウが一つも存在しない場合は新しいウィンドウを生成する
        var windows = MvvmHelper.GetMainWindows();
        if (windows.Any())
            windows.ForEach(w => w.SetForegroundWindow());
        else
            this.CreateWindow().Show();
    }

    /// <summary>
    /// ウィンドウ一覧の項目がクリックされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void WindowListItem_Click(object sender, RoutedEventArgs e)
    {
        ((sender as FrameworkElement)?.DataContext as Window)?.SetForegroundWindow();
    }

    /// <summary>
    /// Windows メッセージを受信したときに呼び出されます。
    /// </summary>
    /// <param name="hWnd">ウィンドウハンドル</param>
    /// <param name="msg">Windows メッセージ</param>
    /// <param name="wParam">メッセージの付加情報</param>
    /// <param name="lParam">メッセージの付加情報</param>
    /// <param name="handled">ハンドルされたかどうかを示す値</param>
    /// <returns>メッセージが処理された場合は 0 以外の値が返ります。</returns>
    [LogInterceptorIgnore]
    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        try
        {
            switch ((User32.WindowMessage)msg)
            {
                case User32.WindowMessage.WM_COPYDATA:
                    {
                        this.Logger.Debug($"{nameof(User32.WindowMessage.WM_COPYDATA)} を受信しました。: {nameof(hWnd)}=0x{hWnd:X}, {nameof(wParam)}={wParam}, {nameof(lParam)}={lParam}");

                        var structure = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                        if (string.IsNullOrEmpty(structure.lpData) == false)
                        {
                            var paths = structure.lpData.Split('\t');
                            var window = MvvmHelper.GetMainWindows().FirstOrDefault();
                            if (window == null)
                            {
                                window = this.CreateWindow();
                                window.Show();
                            }
                            _ = (window.DataContext as MainWindowViewModel)?.InvokeLoad(paths);
                            window.SetForegroundWindow();
                        }
                        else
                        {
                            var windows = MvvmHelper.GetMainWindows();
                            if (windows.Any())
                                (windows.FirstOrDefault(w => w.IsActive) ?? windows.First()).SetForegroundWindow();
                            else
                                this.CreateWindow().Show();
                        }
                        break;
                    }
            }
        }
        catch (Exception e)
        {
            this.Logger.Log($"Windows メッセージを処理する際にエラーが発生しました。", Category.Error, e);
        }
        return IntPtr.Zero;
    }
}
