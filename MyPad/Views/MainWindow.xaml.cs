using Dragablz;
using MahApps.Metro.Controls;
using MyPad.Models;
using MyPad.ViewModels;
using MyPad.Views.Controls;
using MyPad.Views.Regions;
using Plow.Wpf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Unity;
using Vanara.InteropServices;
using Vanara.PInvoke;

namespace MyPad.Views
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        #region インジェクション

        // Constructor Injection
        public IContainerExtension ContainerExtension { get; set; }

        // Dependency Injection
        [Dependency]
        public IRegionManager RegionManager { get; set; }
        [Dependency]
        public SettingsService SettingsService { get; set; }

        #endregion

        #region プロパティ

        private HwndSource _handleSource;
        private User32.MENUITEMINFO _lpmiiShowMenuBar;
        private User32.MENUITEMINFO _lpmiiShowToolBar;
        private User32.MENUITEMINFO _lpmiiShowStatusBar;
        private bool _fullScreenMode;

        public ICSharpCode.AvalonEdit.Search.Localization Localization { get; }
        public IInterTabClient InterTabClient { get; }
        public Notifier Notifier { get; }
        public bool IsNewTabHost { get; set; }
        public MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;
        public TextEditor ActiveTextEditor
        {
            get
            {
                try
                {
                    // MEMO: 選択されたタブ内のコントロールを取得
                    // ItemsSource に ViewModel をバインドした場合、その参照が Item プロパティに設定されるため、
                    // 子要素のビジュアルオブジェクトを直接取得する方法が無い。(仕様)
                    // VisualTree をたどり ContentPreseneter を取得し、内包する要素の名前からコントロールを特定する。
                    // ただし、描画前のオブジェクトはツリーに登録されていないため取得できない。
                    var presenter = this.DraggableTabControl.Descendants().OfType<ContentPresenter>()
                        .Where(x => x.DataContext == this.DraggableTabControl.SelectedItem)
                        .Where(x => x.TemplatedParent == null)
                        .FirstOrDefault();
                    return presenter?.ContentTemplate?.FindName("TextEditor", presenter) as TextEditor;
                }
                catch
                {
                    return null;
                }
            }
        }

        public ICommand SwitchFullScreenModeCommand => new DelegateCommand(() => {
            if (this._fullScreenMode)
            {
                this._fullScreenMode = false;
                this.ShowTitleBar = true;
                this.IgnoreTaskbarOnMaximize = false;
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this._fullScreenMode = true;
                this.ShowTitleBar = false;
                this.IgnoreTaskbarOnMaximize = true;
                this.WindowState = WindowState.Maximized;
                this.ViewModel.DialogService.ToastNotify(Properties.Resources.Message_NotifyFullScreenMode);
            }
        });

        #endregion

        #region メソッド

        [InjectionConstructor]
        [LogInterceptor]
        public MainWindow(IContainerExtension containerExtension)
        {
            this.InitializeComponent();
            this.ContainerExtension = containerExtension;
            this.Localization = new LocalizationWrapper();
            this.InterTabClient = this.ContainerExtension.Resolve<InterTabClientWrapper>();
            this.Notifier = new Notifier(config =>
                {
                    const int TOAST_LIFE_TIME = 5000;
                    const int TOAST_MAX_COUNT = 5;
                    const double TOAST_WIDTH = 300;

                    config.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromMilliseconds(TOAST_LIFE_TIME), MaximumNotificationCount.FromCount(TOAST_MAX_COUNT));
                    config.PositionProvider = new WindowPositionProvider(this, Corner.BottomRight, 5, 0);
                    config.Dispatcher = Application.Current.Dispatcher;
                    config.DisplayOptions.Width = TOAST_WIDTH;
                    config.DisplayOptions.TopMost = false;
                });

            // MEMO: ApplicationCommands.Close の実装
            this.CommandBindings.AddRange(new[] {
                new CommandBinding(
                    ApplicationCommands.Close,
                    (sender, e) =>
                    {
                        if (sender is Window window)
                        {
                            window.Close();
                            e.Handled = true;
                        }
                    },
                    (sender, e) =>
                    {
                        e.CanExecute = true;
                        e.Handled = true;
                    }
                ),
            });
        }

        [LogInterceptor]
        public void ScrollToCaret()
        {
            this.ActiveTextEditor?.ScrollToCaret();
        }

        [LogInterceptor]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // フックメソッドを登録する
            this._handleSource = (HwndSource)PresentationSource.FromVisual(this);
            this._handleSource.AddHook(this.WndProc);

            // システムメニューを構築する
            var sequence = 0u;
            User32.MENUITEMINFO createMenuItem(bool isSeparater = false)
                => new User32.MENUITEMINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(User32.MENUITEMINFO)),
                    fMask = isSeparater ? User32.MenuItemInfoMask.MIIM_FTYPE : User32.MenuItemInfoMask.MIIM_STATE | User32.MenuItemInfoMask.MIIM_ID | User32.MenuItemInfoMask.MIIM_STRING,
                    fType = isSeparater ? User32.MenuItemType.MFT_SEPARATOR : User32.MenuItemType.MFT_MENUBARBREAK,
                    fState = User32.MenuItemState.MFS_ENABLED,
                    wID = ++sequence,
                    hSubMenu = IntPtr.Zero,
                    hbmpChecked = IntPtr.Zero,
                    hbmpUnchecked = IntPtr.Zero,
                    dwItemData = IntPtr.Zero,
                    dwTypeData = new StrPtrAuto(string.Empty), // 必ず文字列で初期化するように
                    cch = 0,
                    hbmpItem = IntPtr.Zero
                };
            var hMenu = User32.GetSystemMenu(this._handleSource.Handle, false);
            this._lpmiiShowMenuBar = createMenuItem();
            this._lpmiiShowToolBar = createMenuItem();
            this._lpmiiShowStatusBar = createMenuItem();
            var lpmiiSeparater = createMenuItem(true);
            User32.InsertMenuItem(hMenu, (uint)SystemMenuIndex.ShowMenuBar, true, ref this._lpmiiShowMenuBar);
            User32.InsertMenuItem(hMenu, (uint)SystemMenuIndex.ShowToolBar, true, ref this._lpmiiShowToolBar);
            User32.InsertMenuItem(hMenu, (uint)SystemMenuIndex.ShowStatusBar, true, ref this._lpmiiShowStatusBar);
            User32.InsertMenuItem(hMenu, (uint)SystemMenuIndex.__Separater__, true, ref lpmiiSeparater);

            // 初期ウィンドウ向けの処理を行う
            if (this.IsNewTabHost == false)
            {
                // 表示位置を復元する
                if (this.SettingsService.System.SaveWindowPlacement && this.SettingsService.System.WindowPlacement.HasValue)
                {
                    var lpwndpl = this.SettingsService.System.WindowPlacement.Value;
                    if (lpwndpl.showCmd == ShowWindowCommand.SW_SHOWMINIMIZED)
                        lpwndpl.showCmd = ShowWindowCommand.SW_SHOWNORMAL;
                    User32.SetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
                }

                // 既定のタブを生成する
                if (this.DraggableTabControl.Items.Count == 0)
                {
                    TabablzControl.AddItemCommand.Execute(null, this.DraggableTabControl);
                }
            }

            // リージョンにビューを設定する
            void addToRegion<T>(string suffix = null)
                => this.RegionManager.AddToRegion(
                    $"{PrismNamingConverter.ConvertToRegionName<T>()}{suffix}", this.ContainerExtension.Resolve<T>());
            addToRegion<MenuBarView>("1");
            addToRegion<MenuBarView>("2");
            addToRegion<ToolBarView>();
            addToRegion<StatusBarView>();
            addToRegion<DiffContentView>();
            addToRegion<PropertyContentView>();
            addToRegion<PrintPreviewContentView>();
            addToRegion<OptionContentView>();
            addToRegion<AboutContentView>();
        }

        [LogInterceptor]
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // MEMO: ダイアログの表示に備えてフォアグラウンドへ移動
            this.SetForegroundWindow();
        }

        [LogInterceptor]
        private void Window_Closed(object sender, EventArgs e)
        {
            // 表示位置を退避する
            if (this.SettingsService.System.SaveWindowPlacement && this._handleSource.IsDisposed == false)
            {
                var lpwndpl = new User32.WINDOWPLACEMENT();
                User32.GetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
                this.SettingsService.System.WindowPlacement = lpwndpl;
            }

            // フックメソッドを解除する
            this._handleSource.RemoveHook(this.WndProc);

            // リソースを開放する
            this.Notifier.Dispose();

            // 他のウィンドウが存在せず、タスクトレイに存在しない場合はアプリケーションを終了する
            if (Application.Current.Windows.OfType<MainWindow>().Any() == false &&
                (this.SettingsService.System.EnableNotificationIcon == false ||
                 this.SettingsService.System.EnableResident == false))
            {
                Application.Current.Windows.OfType<Workspace>().ForEach(w => w.Close());
            }
        }

        [LogInterceptor]
        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            void viewModel_Disposed(object sender, EventArgs e)
            {
                // MEMO: Closing イベント内で非同期処理後にイベントをキャンセルできなくなる問題 (View)
                // ViewModel の Dispose をトリガーに、View の Close メソッドを実行する。
                ((ViewModelBase)sender).Disposed -= viewModel_Disposed;
                this.Dispatcher.InvokeAsync(() => this.Close());
            }

            if (e.OldValue is ViewModelBase oldViewModel)
                oldViewModel.Disposed -= viewModel_Disposed;
            if (e.NewValue is ViewModelBase newViewModel)
                newViewModel.Disposed += viewModel_Disposed;
        }

        [LogInterceptor]
        private void Flyout_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as Flyout)?.IsOpen != false)
                return;

            this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus());
            e.Handled = true;
        }


        [LogInterceptor]
        private void DraggableTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Handled || e.Source != e.OriginalSource)
                return;

            this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus());
            e.Handled = true;
        }

        [LogInterceptor]
        private void DragablzItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            var item = (ContentControl)sender;
            item.Ancestor().OfType<DraggableTabControl>().First().SelectedItem = item.Content;
            e.Handled = true;
        }

        [LogInterceptor]
        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var textEditor = (TextEditor)sender;
            textEditor.Focus();
            textEditor.Redraw();
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32.WindowMessage)msg)
            {
                case User32.WindowMessage.WM_INITMENUPOPUP:
                {
                    var settings = this.SettingsService.System;
                    var hMenu = User32.GetSystemMenu(this._handleSource.Handle, false);
                    this._lpmiiShowMenuBar.dwTypeData.Assign(Properties.Resources.Command_ShowMenuBar);
                    this._lpmiiShowToolBar.dwTypeData.Assign(Properties.Resources.Command_ShowToolBar);
                    this._lpmiiShowStatusBar.dwTypeData.Assign(Properties.Resources.Command_ShowStatusBar);
                    this._lpmiiShowMenuBar.fState = settings.ShowMenuBar ? User32.MenuItemState.MFS_CHECKED : User32.MenuItemState.MFS_ENABLED;
                    this._lpmiiShowToolBar.fState = settings.ShowToolBar ? User32.MenuItemState.MFS_CHECKED : User32.MenuItemState.MFS_ENABLED;
                    this._lpmiiShowStatusBar.fState = settings.ShowStatusBar ? User32.MenuItemState.MFS_CHECKED : User32.MenuItemState.MFS_ENABLED;
                    User32.SetMenuItemInfo(hMenu, (uint)SystemMenuIndex.ShowMenuBar, true, in this._lpmiiShowMenuBar);
                    User32.SetMenuItemInfo(hMenu, (uint)SystemMenuIndex.ShowToolBar, true, in this._lpmiiShowToolBar);
                    User32.SetMenuItemInfo(hMenu, (uint)SystemMenuIndex.ShowStatusBar, true, in this._lpmiiShowStatusBar);
                    break;
                }

                case User32.WindowMessage.WM_SYSCOMMAND:
                {
                    var settings = this.SettingsService.System;
                    var wID = wParam.ToInt32();
                    if (this._lpmiiShowMenuBar.wID == wID)
                        settings.ShowMenuBar = !settings.ShowMenuBar;
                    if (this._lpmiiShowToolBar.wID == wID)
                        settings.ShowToolBar = !settings.ShowToolBar;
                    if (this._lpmiiShowStatusBar.wID == wID)
                        settings.ShowStatusBar = !settings.ShowStatusBar;
                    break;
                }
            }
            return IntPtr.Zero;
        }

        #endregion

        #region 内部クラス

        private enum SystemMenuIndex
        {
            ShowMenuBar = 6,
            ShowToolBar = 7,
            ShowStatusBar = 8,
            __Separater__ = 9,
        }

        public class InterTabClientWrapper : IInterTabClient
        {
            [Dependency]
            public IContainerExtension ContainerExtension { get; set; }
            [Dependency]
            public IRegionManager RegionManager { get; set; }

            INewTabHost<Window> IInterTabClient.GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
            {
                var view = this.ContainerExtension.Resolve<MainWindow>((typeof(IRegionManager), this.RegionManager.CreateRegionManager()));
                view.IsNewTabHost = true;

                // MEMO: IsHeaderPanelVisible = false の状態でフローティングを行うと例外が発生する現象への対策
                // ドラッグ移動中(マウスの左ボタンが押下されている間)はタブを表示する。
                // また既存のタブにドッキングされウィンドウが消滅する場合に備え Closed イベントも監視する。
                //
                // Dragablz/Dragablz/TabablzControl.cs | 6311e72 on 16 Aug 2017 | Line 1330:
                //   _dragablzItemsControl.InstigateDrag(interTabTransfer.Item, newContainer =>
                if (view.SettingsService.System.ShowSingleTab == false)
                {
                    void windowMoveEnd(object sender, EventArgs e)
                    {
                        view.SettingsService.System.ShowSingleTab = false;
                        ((Window)sender).PreviewMouseLeftButtonUp -= windowMoveEnd;
                        ((Window)sender).Closed -= windowMoveEnd;
                    }
                    view.SettingsService.System.ShowSingleTab = true;
                    view.PreviewMouseLeftButtonUp += windowMoveEnd;
                    view.Closed += windowMoveEnd;
                }

                return new NewTabHost<Window>(view, view.DraggableTabControl);
            }

            TabEmptiedResponse IInterTabClient.TabEmptiedHandler(TabablzControl tabControl, Window window)
                => TabEmptiedResponse.CloseWindowOrLayoutBranch;
        }

        public class LocalizationWrapper : ICSharpCode.AvalonEdit.Search.Localization
        {
            public override string MatchCaseText => Properties.Resources.Command_CaseSensitive;
            public override string MatchWholeWordsText => string.Empty;
            public override string UseRegexText => Properties.Resources.Command_UseRegex;
            public override string FindNextText => Properties.Resources.Command_FindNext;
            public override string FindPreviousText => Properties.Resources.Command_FindPrev;
            public override string ErrorText => $"{Properties.Resources.Message_NotifyErrorText}: ";
            public override string NoMatchesFoundText => Properties.Resources.Message_NotifyNoMatchesText;

            public string SwitchFindModeText => Properties.Resources.Command_SwitchFindMode;
            public string FindText => Properties.Resources.Command_Find;
            public string ReplaceText => Properties.Resources.Command_Replace;
            public string ReplaceNextText => Properties.Resources.Command_ReplaceNext;
            public string ReplaceAllText => Properties.Resources.Command_ReplaceAll;
            public string CloseText => Properties.Resources.Command_Close;
        }

        #endregion
    }
}
