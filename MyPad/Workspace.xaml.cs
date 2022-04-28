using MyBase;
using MyBase.Logging;
using MyPad.Models;
using MyPad.PubSub;
using MyPad.ViewModels;
using MyPad.Views;
using Prism.Events;
using Prism.Ioc;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Unity;
using Vanara.PInvoke;

namespace MyPad;

/// <summary>
/// アプリケーションのメインプロセスを表します。
/// このクラスのインスタンスが <see cref="Application.MainWindow"/> であり、
/// タスクトレイ内の常駐プロセスの制御、多重起動時の Windows メッセージの受理も行います。
/// </summary>
public partial class Workspace : Window
{
    #region インジェクション

    // Dependency Injection
    [Dependency]
    public IContainerExtension Container { get; set; }
    [Dependency]
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public SettingsModel Settings { get; set; }

    #endregion

    #region プロパティ

    private readonly System.Windows.Forms.NotifyIcon _notifyIcon = new();
    private readonly SemaphoreSlim _semaphoreSingleClick = new(1);
    private readonly SemaphoreSlim _semaphoreDoubleClick = new(0);
    private HwndSource _handleSource;

    #endregion

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="eventAggregator">イベントアグリゲーター</param>
    /// <param name="productInfo">プロダクト情報</param>
    [InjectionConstructor]
    [LogInterceptor]
    public Workspace(IEventAggregator eventAggregator, IProductInfo productInfo)
    {
        this.InitializeComponent();

        this._notifyIcon.Text = productInfo.Title;
        this._notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri(new Views.Markup.IconSourceExtension("Resources/app.ico").Source)).Stream);
        this._notifyIcon.ContextMenuStrip = new();
        this._notifyIcon.ContextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem()); // INFO: ダミーを入れることでコンテキストメニューの初回表示がスムーズになる
        this._notifyIcon.ContextMenuStrip.Opening += this.NotifyIcon_ContextMenuOpening;
        this._notifyIcon.Click += this.NotifyIcon_Click;
        this._notifyIcon.DoubleClick += this.NotifyIcon_DoubleClick;

        async void exitApplication() => await this.ExitApplication();
        eventAggregator.GetEvent<ExitApplicationEvent>().Subscribe(exitApplication);

        void refreshNotifyIcon() => this._notifyIcon.Visible = this.Settings.System.EnableNotifyIcon;
        eventAggregator.GetEvent<RefreshNotifyIconEvent>().Subscribe(refreshNotifyIcon);
    }

    /// <summary>
    /// アプリケーションの終了を試行します。
    /// 内包するすべての <see cref="MainWindowViewModel"/> が終了要求に応じた場合、アプリケーションは終了します。
    /// </summary>
    /// <returns>非同期タスク</returns>
    [LogInterceptor]
    private async Task ExitApplication()
    {
        // すべての ViewModel の破棄に成功した場合はアプリケーションを終了する
        var viewModels = MvvmHelper.GetMainWindowViewModels();
        for (var i = viewModels.Count() - 1; 0 <= i; i--)
        {
            if (await viewModels.ElementAt(i).InvokeClose() == false)
                return;
        }
        await this.Dispatcher.InvokeAsync(() => this.Close());
    }

    /// <summary>
    /// ウィンドウがロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // このインスタンスのウィンドウは非表示にする
        // (タスクバーに常駐するだけのウィンドウ)
        this.Hide();

        // フックメソッドを登録する
        this._handleSource = this.GetHwndSource();
        this._handleSource.AddHook(this.WndProc);

        // 起動時の通知領域アイコンの表示状態を決定する
        this._notifyIcon.Visible = this.Settings.System.EnableNotifyIcon;
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
        this._notifyIcon.Dispose();
    }

    /// <summary>
    /// 通知領域アイコンのコンテキストメニューが開かれるときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void NotifyIcon_ContextMenuOpening(object sender, CancelEventArgs e)
    {
        var contextMenu = (System.Windows.Forms.ContextMenuStrip)sender;
        contextMenu.Items.Clear();

        var windows = MvvmHelper.GetMainWindows();
        if (windows.Any())
        {
            var windowListMenuItem = new System.Windows.Forms.ToolStripMenuItem(Properties.Resources.Command_WindowList);
            for (var i = 0; i < windows.Count(); i++)
            {
                var window = windows.ElementAt(i);
                windowListMenuItem.DropDownItems.Add(
                    new System.Windows.Forms.ToolStripMenuItem(
                        $"[{window.ViewModel.Sequense}] {window.ViewModel.ActiveTextEditor.Value?.FileName}",
                        null,
                        (sender, _) => window.SetForegroundWindow()
                    ));
            }
            contextMenu.Items.Add(windowListMenuItem);
        }

        contextMenu.Items.AddRange(
            new System.Windows.Forms.ToolStripItem[] {
                new System.Windows.Forms.ToolStripMenuItem(
                    Properties.Resources.Command_NewWindow,
                    null,
                    (sender, _) => this.Container.Resolve<MainWindow>().Show()),
                new System.Windows.Forms.ToolStripMenuItem(
                    Properties.Resources.Command_ExitApplication,
                    null,
                    async (sender, _) => await this.ExitApplication())
            });
    }

    /// <summary>
    /// 通知領域アイコンがクリックされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private async void NotifyIcon_Click(object sender, EventArgs e)
    {
        // INFO: Windows Forms の Click イベントはダブルクリック時にも発火する
        // ダブルクリック時は無視するように制御する
        if (this._semaphoreSingleClick.Wait(0) == false) return;
        try { if (await this._semaphoreDoubleClick.WaitAsync(System.Windows.Forms.SystemInformation.DoubleClickTime)) return; }
        finally { this._semaphoreSingleClick.Release(); }

        // 左クリック時もコンテキストメニューを表示する
        typeof(System.Windows.Forms.NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this._notifyIcon, null);
    }

    /// <summary>
    /// 通知領域アイコンがダブルクリックされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void NotifyIcon_DoubleClick(object sender, EventArgs e)
    {
        this._semaphoreDoubleClick.Release();

        // ウィンドウが存在する場合はそれらをフォアグラウンドに移動する
        // ウィンドウが一つも存在しない場合は新しいウィンドウを生成する
        var windows = MvvmHelper.GetMainWindows();
        if (windows.Any())
            windows.ForEach(w => w.SetForegroundWindow());
        else
            this.Container.Resolve<MainWindow>().Show();
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
    [LogInterceptorIgnore] // 呼び出しが頻発するため
    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        try
        {
            switch ((User32.WindowMessage)msg)
            {
                case User32.WindowMessage.WM_COPYDATA:
                    {
                        this.Logger.Debug($"{nameof(User32.WindowMessage.WM_COPYDATA)} を受信しました。: {nameof(hWnd)}=0x{hWnd:X}, {nameof(wParam)}={wParam}, {nameof(lParam)}={lParam}");

                        var structure = Marshal.PtrToStructure<Win32.COPYDATASTRUCT>(lParam);
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
