using Hardcodet.Wpf.TaskbarNotification;
using MyPad.ViewModels;
using MyPad.ViewModels.Events;
using Plow.Wpf;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Unity;
using Vanara.PInvoke;

namespace MyPad.Views
{
    public partial class Workspace : Window
    {
        #region インジェクション

        // Constructor Injection
        public IEventAggregator EventAggregator { get; set; }

        // Dependency Injection
        [Dependency]
        public IContainerExtension ContainerExtension { get; set; }
        [Dependency]
        public IRegionManager RegionManager { get; set; }

        #endregion

        #region プロパティ

        private HwndSource _handleSource;

        #endregion

        [InjectionConstructor]
        [LogInterceptor]
        public Workspace(IEventAggregator eventAggregator)
        {
            this.InitializeComponent();
            this.EventAggregator = eventAggregator;

            void createWindow() => this.CreateWindow().Show();
            this.EventAggregator.GetEvent<CreateWindowEvent>().Subscribe(createWindow);
            void showBalloon((string title, string message) args) => this.TaskbarIcon.ShowBalloonTip(args.title, args.message, BalloonIcon.Info);
            this.EventAggregator.GetEvent<RaiseBalloonEvent>().Subscribe(showBalloon);
        }

        [LogInterceptor]
        private MainWindow CreateWindow()
            => this.ContainerExtension.Resolve<MainWindow>((typeof(IRegionManager), this.RegionManager.CreateRegionManager()));

        [LogInterceptor]
        private IEnumerable<MainWindow> GetWindows()
            => Application.Current?.Windows.OfType<MainWindow>() ?? Enumerable.Empty<MainWindow>();

        [LogInterceptor]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this._handleSource = (HwndSource)PresentationSource.FromVisual(this);
            this._handleSource.AddHook(this.WndProc);
            this.Hide();

            // 初期ウィンドウを生成する
            var view = this.ContainerExtension.Resolve<MainWindow>();
            void view_ContentRendered(object sender, EventArgs e)
            {
                view.ContentRendered -= view_ContentRendered;
                if (view.ViewModel.SettingsService.IsDifferentVersion())
                    view.ViewModel.DialogService.ToastNotify(string.Format(Properties.Resources.Message_NotifyWelcome, view.ViewModel.ProductInfo.Product, view.ViewModel.ProductInfo.Version));
            }
            view.ContentRendered += view_ContentRendered;

            // コマンドライン引数を渡す
            var args = ((App)Application.Current).CommandLineArgs;
            if (args.Any())
                view.ViewModel.LoadCommand.Execute(args);

            // 初期ウィンドウを表示する
            view.Show();
        }

        [LogInterceptor]
        private void Window_Closed(object sender, EventArgs e)
        {
            this._handleSource.RemoveHook(this.WndProc);
            this.Descendants().OfType<TaskbarIcon>().ForEach(t => t.Dispose());
        }

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

        [LogInterceptor]
        private void TaskbarIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            this.WindowListItem.Items.Clear();
            var windows = this.GetWindows();
            for (var i = 0; i < windows.Count(); i++)
                this.WindowListItem.Items.Add(new MenuItem { DataContext = windows.ElementAt(i) });
        }

        [LogInterceptor]
        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // ウィンドウが存在する場合はそれらをフォアグラウンドに移動する
            // ウィンドウが一つも存在しない場合は新しいウィンドウを生成する
            var windows = this.GetWindows();
            if (windows.Any())
                windows.ForEach(w => w.SetForegroundWindow());
            else
                this.CreateWindow().Show();
        }

        [LogInterceptor]
        private void NewWindowCommand_Click(object sender, RoutedEventArgs e)
        {
            this.CreateWindow().Show();
        }

        [LogInterceptor]
        private void WindowItem_Click(object sender, RoutedEventArgs e)
        {
            ((sender as FrameworkElement)?.DataContext as Window)?.SetForegroundWindow();
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32.WindowMessage)msg)
            {
                case User32.WindowMessage.WM_COPYDATA:
                {
                    var structure = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                    if (string.IsNullOrEmpty(structure.lpData) == false)
                    {
                        var paths = structure.lpData.Split('\t');
                        var window = Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault();
                        if (window == null)
                        {
                            window = this.CreateWindow();
                            window.Show();
                        }
                        (window.DataContext as MainWindowViewModel)?.LoadCommand.Execute(paths);
                        window.SetForegroundWindow();
                    }
                    else
                    {
                        var windows = this.GetWindows();
                        if (windows.Any())
                            (windows.FirstOrDefault(w => w.IsActive) ?? windows.First()).SetForegroundWindow();
                        else
                            this.CreateWindow().Show();
                    }
                    break;
                }
            }
            return IntPtr.Zero;
        }
    }
}
