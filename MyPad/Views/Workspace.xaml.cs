using Hardcodet.Wpf.TaskbarNotification;
using MyBase.Logging;
using MyPad.ViewModels;
using Prism.Ioc;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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

    // Dependency Injection
    [Dependency]
    public IContainerExtension Container { get; set; }
    [Dependency]
    public ILoggerFacade Logger { get; set; }

    #endregion

    #region プロパティ

    private HwndSource _handleSource;

    #endregion

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    [InjectionConstructor]
    [LogInterceptor]
    public Workspace()
    {
        this.InitializeComponent();
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
            this.Container.Resolve<MainWindow>().Show();
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
                                window = this.Container.Resolve<MainWindow>();
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
                                this.Container.Resolve<MainWindow>().Show();
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
