using Dragablz;
using MahApps.Metro.Controls;
using MyBase.Logging;
using MyPad.Models;
using MyPad.ViewModels;
using MyPad.Views.Controls;
using MyPad.Views.Regions;
using Prism.Commands;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Unity;
using Vanara.PInvoke;

namespace MyPad.Views;

/// <summary>
/// アプリケーションのメインウィンドウを表します。
/// </summary>
public partial class MainWindow : MetroWindow
{
    #region インジェクション

    // Constructor Injection
    public IContainerExtension Container { get; set; }
    public IRegionManager RegionManager { get; set; }

    // Dependency Injection
    [Dependency]
    public ILoggerFacade Logger { get; set; }
    [Dependency]
    public Settings Settings { get; set; }

    #endregion

    #region プロパティ

    public static readonly ICommand ActivateTextEditorCommand
        = new RoutedCommand(nameof(ActivateTextEditorCommand), typeof(MainWindow));
    public static readonly ICommand ActivateTerminalCommand
        = new RoutedCommand(nameof(ActivateTerminalCommand), typeof(MainWindow));
    public static readonly ICommand ActivateScriptRunnerCommand
        = new RoutedCommand(nameof(ActivateScriptRunnerCommand), typeof(MainWindow));
    public static readonly ICommand ActivateExplorerCommand
        = new RoutedCommand(nameof(ActivateExplorerCommand), typeof(MainWindow));
    public static readonly ICommand ActivateGrepCommand
        = new RoutedCommand(nameof(ActivateGrepCommand), typeof(MainWindow));
    public static readonly ICommand ActivatePropertyCommand
        = new RoutedCommand(nameof(ActivatePropertyCommand), typeof(MainWindow));
    public ICommand SwitchFullScreenModeCommand
        => new DelegateCommand(() =>
        {
            if (this._fullScreenMode)
            {
                // ウィンドウモードに切り替える
                this._fullScreenMode = false;
                this.ShowTitleBar = true;
                this.IgnoreTaskbarOnMaximize = false;
                this.WindowState = WindowState.Normal;
            }
            else
            {
                // 全画面モードに切り替える
                this._fullScreenMode = true;
                this.ShowTitleBar = false;
                this.IgnoreTaskbarOnMaximize = true;
                this.WindowState = WindowState.Maximized;

                this.TryInjectRegion(typeof(MenuBarView));
                this.ViewModel.DialogService.ToastNotify(Properties.Resources.Message_NotifyFullScreenMode);
            }
        });

    private static readonly DependencyProperty IsVisibleBottomContentProperty
        = DependencyPropertyExtensions.Register(
            new PropertyMetadata((obj, e) =>
            {
                var self = (MainWindow)obj;
                if (e.NewValue is bool value && value)
                {
                    self.BottomContentRow.Height = new(150);
                    self.FocusBottomContent();
                }
                else
                {
                    self.BottomContentRow.Height = new(0);
                    self.FocusTextEditor();
                }
            }));

    private HwndSource _handleSource;
    private (uint fByPosition, User32.MENUITEMINFO lpmii) _miiShowMenuBar;
    private (uint fByPosition, User32.MENUITEMINFO lpmii) _miiShowToolBar;
    private (uint fByPosition, User32.MENUITEMINFO lpmii) _miiShowSideBar;
    private (uint fByPosition, User32.MENUITEMINFO lpmii) _miiShowStatusBar;
    private (uint fByPosition, User32.MENUITEMINFO lpmii) _miiSeparator;
    private (double sideBarWidth, double contentAreaWidth) _columnWidthCache = (2, 5);
    private bool _fullScreenMode;

    /// <summary>
    /// このウィンドウの ViewModel
    /// </summary>
    public MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

    /// <summary>
    /// フローティングウィンドウプロバイダー
    /// </summary>
    public IInterTabClient InterTabClient { get; }

    /// <summary>
    /// トースト通知プロバイダー
    /// </summary>
    public Notifier Notifier { get; }

    /// <summary>
    /// 検索パネル用の多言語処理プロバイダー
    /// </summary>
    public ICSharpCode.AvalonEdit.Search.Localization Localization { get; }

    /// <summary>
    /// 初期ウィンドウであるかどうかを示す値
    /// </summary>
    public bool IsInitialWindow { get; set; }

    /// <summary>
    /// フローティングによって誕生したウィンドウであるかどうかを示す値
    /// </summary>
    public bool IsFloatingWindow { get; set; }

    /// <summary>
    /// サイドコンテンツが開かれているかどうかを示す値
    /// </summary>
    public bool IsOpenedSideContent => double.IsNaN(this.SideContent.Width);

    /// <summary>
    /// ボトムコンテンツが表示されているかどうかを示す値
    /// </summary>
    public bool IsVisibleBottomContent
    {
        get => (bool)this.GetValue(IsVisibleBottomContentProperty);
        set => this.SetValue(IsVisibleBottomContentProperty, value);
    }

    /// <summary>
    /// アクティブな <see cref="TextEditor"/> のインスタンス
    /// </summary>
    public TextEditor ActiveTextEditor
    {
        get
        {
            try
            {
                // INFO: タブ内のコントロールを取得できない問題への対応
                // ItemsSource に ViewModel をバインドした場合、その参照が Item プロパティに設定されるため、
                // 子要素のビジュアルオブジェクトを直接取得する方法が無い。(仕様)
                // VisualTree をたどり ContentPreseneter を取得し、内包する要素の名前からコントロールを特定する。
                // ただし、描画前のオブジェクトはツリーに登録されていないため取得できない。
                var presenter = this.MainContent.Descendants().OfType<ContentPresenter>()
                    .Where(x => x.DataContext == this.MainContent.SelectedItem)
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

    #endregion

    #region メソッド

    /// <summary>
    /// このクラスの新しいインスタンスを生成します。
    /// </summary>
    /// <param name="container">DI コンテナ</param>
    /// <param name="regionManager">リージョンマネージャー</param>
    [InjectionConstructor]
    [LogInterceptor]
    public MainWindow(IContainerExtension container, IRegionManager regionManager)
    {
        this.Container = container;
        this.RegionManager = regionManager.CreateRegionManager();

        this.InitializeComponent();

        // INFO: HamburgerMenu 内のコンテンツは遅延初期化されるため p:RegionManager.RegionName では紐づけできない
        Prism.Regions.RegionManager.SetRegionName(this.ExplorerRegion, nameof(this.ExplorerRegion));
        Prism.Regions.RegionManager.SetRegionManager(this.ExplorerRegion, this.RegionManager);
        Prism.Regions.RegionManager.SetRegionName(this.GrepRegion, nameof(this.GrepRegion));
        Prism.Regions.RegionManager.SetRegionManager(this.GrepRegion, this.RegionManager);
        Prism.Regions.RegionManager.SetRegionName(this.PropertyRegion, nameof(this.PropertyRegion));
        Prism.Regions.RegionManager.SetRegionManager(this.PropertyRegion, this.RegionManager);

        this.Localization = new LocalizationWrapper();
        this.InterTabClient = this.Container.Resolve<InterTabClientWrapper>();
        this.Notifier = new(config =>
        {
            config.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(AppSettingsReader.ToastDuration), MaximumNotificationCount.FromCount(AppSettingsReader.ToastCountLimit));
            config.PositionProvider = new WindowPositionProvider(this, Corner.BottomRight, 5, 0);
            config.Dispatcher = Application.Current.Dispatcher;
            config.DisplayOptions.Width = 280;
            config.DisplayOptions.TopMost = false;
        });

        this.CommandBindings.AddRange(new[] {
            new CommandBinding(
                // ApplicationCommands.Close の実装
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
            new CommandBinding(
                ActivateTextEditorCommand,
                (sender, e) => this.FocusTextEditor(),
                (sender, e) => e.CanExecute = this.ViewModel.IsEditMode.Value
            ),
            new CommandBinding(
                ActivateTerminalCommand,
                (sender, e) =>
                {
                    if (this.IsVisibleBottomContent && this.BottomContent.SelectedIndex == 0)
                    {
                        this.IsVisibleBottomContent = false;
                    }
                    else
                    {
                        this.BottomContent.SelectedIndex = 0;
                        this.IsVisibleBottomContent = true;
                        this.FocusBottomContent();
                    }
                },
                (sender, e) => e.CanExecute = this.ViewModel.IsEditMode.Value
            ),
            new CommandBinding(
                ActivateScriptRunnerCommand,
                (sender, e) =>
                {
                    if (this.IsVisibleBottomContent && this.BottomContent.SelectedIndex == 1)
                    {
                        this.IsVisibleBottomContent = false;
                    }
                    else
                    {
                        this.BottomContent.SelectedIndex = 1;
                        this.IsVisibleBottomContent = true;
                        this.FocusBottomContent();
                    }
                },
                (sender, e) => e.CanExecute = this.ViewModel.IsEditMode.Value
            ),
            new CommandBinding(
                ActivateExplorerCommand,
                (sender, e) => this.PerformClickSideContent(this.ExplorerItem),
                (sender, e) => e.CanExecute = this.ViewModel.IsEditMode.Value
            ),
            new CommandBinding(
                ActivateGrepCommand,
                (sender, e) => this.PerformClickSideContent(this.GrepItem),
                (sender, e) => e.CanExecute = this.ViewModel.IsEditMode.Value
            ),
            new CommandBinding(
                ActivatePropertyCommand,
                (sender, e) => this.PerformClickSideContent(this.PropertyItem),
                (sender, e) => e.CanExecute = this.ViewModel.IsEditMode.Value
            ),
        });

        var sequence = 0u;
        User32.MENUITEMINFO createMenuItem(bool isSeparator = false)
            => new()
            {
                cbSize = (uint)Marshal.SizeOf(typeof(User32.MENUITEMINFO)),
                fMask = isSeparator ? User32.MenuItemInfoMask.MIIM_FTYPE : User32.MenuItemInfoMask.MIIM_STATE | User32.MenuItemInfoMask.MIIM_ID | User32.MenuItemInfoMask.MIIM_STRING,
                fType = isSeparator ? User32.MenuItemType.MFT_SEPARATOR : User32.MenuItemType.MFT_MENUBARBREAK,
                fState = User32.MenuItemState.MFS_ENABLED,
                wID = ++sequence,
                hSubMenu = IntPtr.Zero,
                hbmpChecked = IntPtr.Zero,
                hbmpUnchecked = IntPtr.Zero,
                dwItemData = IntPtr.Zero,
                dwTypeData = new(string.Empty), // 必ず文字列で初期化するように
                cch = 0,
                hbmpItem = IntPtr.Zero
            };
        this._miiShowMenuBar = (6, createMenuItem());
        this._miiShowToolBar = (7, createMenuItem());
        this._miiShowSideBar = (8, createMenuItem());
        this._miiShowStatusBar = (9, createMenuItem());
        this._miiSeparator = (10, createMenuItem(true));
    }

    /// <summary>
    /// テキストエディタをキャレット位置までスクロールさせます。
    /// </summary>
    [LogInterceptor]
    public void ScrollToCaret()
    {
        this.ActiveTextEditor?.ScrollToCaret();
    }

    /// <summary>
    /// テキストエディタにフォーカスを設定します。
    /// </summary>
    [LogInterceptor]
    private void FocusTextEditor()
    {
        this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus());
    }

    /// <summary>
    /// ボトムコンテンツにフォーカスを設定します。
    /// </summary>
    [LogInterceptor]
    private void FocusBottomContent()
    {
        if ((((this.BottomContent.SelectedItem as TabItem)?.Content as ContentControl)?.Content as UserControl)?.FindName("ScriptInputField") is not FrameworkElement element)
            return;

        if (element.IsLoaded)
        {
            this.Dispatcher.InvokeAsync(() => element.Focus());
            return;
        }

        void elementLoaded(object sender, EventArgs e)
        {
            element.Focus();
            element.Loaded -= elementLoaded;
        }
        element.Loaded += elementLoaded;
    }

    /// <summary>
    /// サイドコンテンツにフォーカスを設定します。
    /// </summary>
    [LogInterceptor]
    private void FocusSideContent()
    {
        if ((this.SideContent.Content as HamburgerMenuItem)?.Tag is not FrameworkElement element)
            return;

        void elementLoaded(object sender, EventArgs e)
        {
            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            element.Loaded -= elementLoaded;
        }
        element.Loaded += elementLoaded;
    }

    /// <summary>
    /// サイドコンテンツを開き、項目をアクティブにします。
    /// </summary>
    /// <param name="targetItem">アクティブにする項目</param>
    [LogInterceptor]
    private void OpenSideContent(object targetItem)
    {
        // 指定されたコンテンツを選択する
        this.SideContent.Content = targetItem;

        if (this.IsOpenedSideContent)
            return;

        // ハンバーガーメニューを開く
        this.SideContent.Width = double.NaN;

        // グリッドの列構成を調整する
        this.SideContentColumn.Width = new(this._columnWidthCache.sideBarWidth, GridUnitType.Star);
        this.MainContentColumn.Width = new(this._columnWidthCache.contentAreaWidth, GridUnitType.Star);
    }

    /// <summary>
    /// サイドコンテンツを閉じます。
    /// </summary>
    [LogInterceptor]
    private void CloseSideContent()
    {
        // コンテンツの選択状態を解除し、ハンバーガーメニューを閉じる
        this.SideContent.Content = null;
        this.SideContent.Width = this.SideContent.HamburgerWidth;

        // グリッドの列構成を調整する
        this._columnWidthCache = (this.SideContentColumn.Width.Value, this.MainContentColumn.Width.Value);
        this.SideContentColumn.Width = GridLength.Auto;
        this.MainContentColumn.Width = new(1, GridUnitType.Star);
    }

    /// <summary>
    /// サイドコンテンツの項目を選択した際の再現します。
    /// </summary>
    /// <param name="targetItem">対象の項目</param>
    [LogInterceptor]
    private void PerformClickSideContent(HamburgerMenuItem targetItem)
    {
        this.Settings.System.ShowSideBar = true;

        if (this.IsOpenedSideContent == false || this.SideContent.Content != targetItem)
        {
            // 閉じた状態の場合 または
            // 開いた状態 かつ 非アクティブな項目が選択された場合
            // 選択された項目をアクティブにする
            this.OpenSideContent(targetItem);
            this.FocusSideContent();

            if (targetItem.Tag is ContentControl region && string.IsNullOrEmpty(region.Name) == false)
                this.TryInjectRegion(region.Name);
        }
        else
        {
            // 開いた状態 かつ アクティブな項目が選択された 場合
            // 選択状態を解除し、ハンバーガーメニューを閉じる
            this.CloseSideContent();
            this.FocusTextEditor();
        }
    }

    /// <summary>
    /// ウィンドウ内のリージョンに View のインスタンスを設定します。
    /// </summary>
    /// <param name="regionName">リージョン名</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private bool TryInjectRegion(string regionName)
    {
        if (this.RegionManager.Regions.First(r => r.Name == regionName).Views.Any())
            return false;

        var regionType = PrismConverter.RegionNameToRegionType(regionName);
        var view = this.Container.Resolve(regionType);
        return this.InjectRegion(regionName, view);
    }

    /// <summary>
    /// ウィンドウ内のリージョンに View のインスタンスを設定します。
    /// </summary>
    /// <param name="regionType">リージョンの型</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private bool TryInjectRegion(Type regionType)
    {
        var regionName = PrismConverter.ConvertToRegionName(regionType);
        if (this.RegionManager.Regions.First(r => r.Name == regionName).Views.Any())
            return false;

        var view = this.Container.Resolve(regionType);
        return this.InjectRegion(regionName, view);
    }

    /// <summary>
    /// ウィンドウ内のリージョンに View のインスタンスを設定します。
    /// </summary>
    /// <param name="regionName">リージョン名</param>
    /// <param name="view">View のインスタンス</param>
    /// <returns>正常に処理されたかどうかを示す値</returns>
    [LogInterceptor]
    private bool InjectRegion(string regionName, object view)
    {
        try
        {
            this.RegionManager.AddToRegion(regionName, view);
            return true;
        }
        catch (Exception e)
        {
            this.Logger.Log($"IRegionManager.AddToRegion() が失敗しました。別メソッドで再試行します。: RegionName={regionName}", Category.Warn, e);
        }
        try
        {
            this.RegionManager.RegisterViewWithRegion(regionName, () => view);
            return true;
        }
        catch (Exception e)
        {
            this.Logger.Log($"IRegionManager.RegisterViewWithRegion() が失敗しました。: RegionName={regionName}", Category.Error, e);
        }
        return false;
    }

    #endregion

    #region イベントハンドラ

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

        // システムメニューを構築する
        var hMenu = User32.GetSystemMenu(this._handleSource.Handle, false);
        User32.InsertMenuItem(hMenu, this._miiShowMenuBar.fByPosition, true, ref this._miiShowMenuBar.lpmii);
        User32.InsertMenuItem(hMenu, this._miiShowToolBar.fByPosition, true, ref this._miiShowToolBar.lpmii);
        User32.InsertMenuItem(hMenu, this._miiShowSideBar.fByPosition, true, ref this._miiShowSideBar.lpmii);
        User32.InsertMenuItem(hMenu, this._miiShowStatusBar.fByPosition, true, ref this._miiShowStatusBar.lpmii);
        User32.InsertMenuItem(hMenu, this._miiSeparator.fByPosition, true, ref this._miiSeparator.lpmii);

        // 表示位置を復元する
        if (this.IsInitialWindow)
        {
            if (this.Settings.System.SaveWindowPlacement && this.Settings.System.WindowPlacement.HasValue)
            {
                var lpwndpl = this.Settings.System.WindowPlacement.Value;
                if (lpwndpl.showCmd == ShowWindowCommand.SW_SHOWMINIMIZED)
                    lpwndpl.showCmd = ShowWindowCommand.SW_SHOWNORMAL;
                User32.SetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
            }
        }

        // 既定のタブを生成する
        if (this.IsInitialWindow || this.IsFloatingWindow == false)
        {
            if (this.MainContent.Items.Count == 0)
            {
                TabablzControl.AddItemCommand.Execute(null, this.MainContent);
            }
        }

        this.Logger.Log($"ウィンドウを生成しました。win#{this.ViewModel.Sequense}", Category.Info);
    }

    /// <summary>
    /// ウィンドウのコンテンツの描画が完了したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_ContentRendered(object sender, EventArgs e)
    {
        // イベントを購読する
        // INFO: 以下のイベントはウィンドウのロード前にも発生しており、トレースログが汚れるのでこのタイミングで紐づける
        this.SideContent.IsVisibleChanged += this.SideContent_IsVisibleChanged;
        this.BottomContent.IsVisibleChanged += this.BottomContent_IsVisibleChanged;
    }

    /// <summary>
    /// ウィンドウが閉じられたあとに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_Closed(object sender, EventArgs e)
    {
        this.Logger.Log($"ウィンドウを破棄しました。win#{this.ViewModel.Sequense}", Category.Info);

        // イベントの購読を解除する
        this.SideContent.IsVisibleChanged -= this.SideContent_IsVisibleChanged;
        this.BottomContent.IsVisibleChanged -= this.BottomContent_IsVisibleChanged;

        // 表示位置を退避する
        if (this.Settings.System.SaveWindowPlacement && this._handleSource.IsDisposed == false)
        {
            var lpwndpl = new User32.WINDOWPLACEMENT();
            User32.GetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
            this.Settings.System.WindowPlacement = lpwndpl;
        }

        // リージョンを破棄する
        this.RegionManager.Regions.ForEach(r => r.RemoveAll());

        // フックメソッドを解除する
        this._handleSource.RemoveHook(this.WndProc);

        // 他のウィンドウが存在せず、タスクトレイに存在しない場合はアプリケーションを終了する
        if (MvvmHelper.GetMainWindows().Any() == false &&
            (this.Settings.System.EnableNotificationIcon == false ||
             this.Settings.System.EnableResident == false))
        {
            MvvmHelper.GetWorkspace()?.Close();
        }
    }

    /// <summary>
    /// DataContext が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // INFO: Closing イベント内で非同期処理後にイベントをキャンセルできない問題への対応 (View)
        // 非同期処理を挟む都合上、ViewModel は Closing イベントをキャンセルせざるを得ない。
        // したがって Close 処理は View 側で行う。
        // ViewModel は Close 要件を満たすと自身の Dispose を実行する。
        // View はこれをトリガーに Close メソッドを実行する。
        void viewModel_Disposed(object sender, EventArgs e)
        {
            ((ViewModelBase)sender).Disposed -= viewModel_Disposed;

            // HACK: ウィンドウのクローズ後にアプリ内トーストが表示されると例外になる現象への対応
            // ウィンドウがクローズされる前に、トースト通知プロバイダーを解放する。
            this.Notifier.Dispose();

            // ウィンドウを閉じる
            // ViewModel が破棄済みのためウィンドウは確実に終了する
            this.Dispatcher.InvokeAsync(() => this.Close());
        }

        if (e.OldValue is ViewModelBase oldViewModel)
            oldViewModel.Disposed -= viewModel_Disposed;
        if (e.NewValue is ViewModelBase newViewModel)
            newViewModel.Disposed += viewModel_Disposed;
    }

    /// <summary>
    /// サイドコンテンツの表示設定が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void SideContent_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (this.IsLoaded == false)
            return;
        if (e.NewValue is not bool visible || visible)
            return;
        if (this.IsOpenedSideContent == false)
            return;

        this.CloseSideContent();
    }

    /// <summary>
    /// サイドコンテンツの項目が選択されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void SideContent_ItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs e)
    {
        if (System.Windows.Input.MouseButtonState.Pressed == Mouse.LeftButton && e.IsItemOptions == false)
            this.PerformClickSideContent((HamburgerMenuItem)e.InvokedItem);
        e.Handled = true;
    }


    /// <summary>
    /// メインコンテンツの選択中の項目が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void MainContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Handled || e.Source != e.OriginalSource)
            return;

        this.FocusTextEditor();
        e.Handled = true;
    }

    /// <summary>
    /// メインコンテンツの項目を右クリックしたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void MainContentItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled)
            return;

        var item = (ContentControl)sender;
        item.Ancestor().OfType<DraggableTabControl>().First().SelectedItem = item.Content;
        e.Handled = true;
    }

    /// <summary>
    /// テキストエディタがロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
        var textEditor = (TextEditor)sender;
        textEditor.Focus();
        textEditor.Redraw();
    }

    /// <summary>
    /// テキストエディタがアンロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void TextEditor_Unloaded(object sender, RoutedEventArgs e)
    {
        var textEditor = (TextEditor)sender;
        textEditor.Dispose();
    }

    /// <summary>
    /// ボトムコンテンツの表示設定が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void BottomContent_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (this.IsLoaded == false || this.BottomContent.IsVisible == false)
            return;

        foreach (var tabItem in this.BottomContent.Items.Cast<TabItem>())
        {
            var regionName = Prism.Regions.RegionManager.GetRegionName((ContentControl)tabItem.Content);
            if (this.TryInjectRegion(regionName) == false)
                break;
        }
    }

    /// <summary>
    /// 列スプリッターの移動が完了したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void ColumnSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (this.SideContentColumn.Width.Value != this.SideContentColumn.MinWidth)
            return;

        this.SideContentColumn.Width = new(this._columnWidthCache.sideBarWidth, GridUnitType.Star);
        this.MainContentColumn.Width = new(this._columnWidthCache.contentAreaWidth, GridUnitType.Star);
        this.CloseSideContent();
    }

    /// <summary>
    /// 行スプリッターの移動が完了したときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void RowSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (this.BottomContentRow.Height.Value != 0)
            return;

        this.IsVisibleBottomContent = false;
    }

    /// <summary>
    /// フライアウトの開閉状態が変更されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void Flyout_IsOpenChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not Flyout flyout)
            return;

        if (flyout.IsOpen)
        {
            var regionName = Prism.Regions.RegionManager.GetRegionName((ContentControl)flyout.Content);
            this.TryInjectRegion(regionName);
        }
        else
        {
            this.FocusTextEditor();
        }
        e.Handled = true;
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
                // システムメニューがアクティブになったとき
                case User32.WindowMessage.WM_INITMENUPOPUP:
                    {
                        var hMenu = User32.GetSystemMenu(this._handleSource.Handle, false);
                        this._miiShowMenuBar.lpmii.dwTypeData.Assign(Properties.Resources.Command_ShowMenuBar);
                        this._miiShowToolBar.lpmii.dwTypeData.Assign(Properties.Resources.Command_ShowToolBar);
                        this._miiShowSideBar.lpmii.dwTypeData.Assign(Properties.Resources.Command_ShowSideBar);
                        this._miiShowStatusBar.lpmii.dwTypeData.Assign(Properties.Resources.Command_ShowStatusBar);
                        this._miiShowMenuBar.lpmii.fState = this.Settings.System.ShowMenuBar ? User32.MenuItemState.MFS_CHECKED : User32.MenuItemState.MFS_ENABLED;
                        this._miiShowToolBar.lpmii.fState = this.Settings.System.ShowToolBar ? User32.MenuItemState.MFS_CHECKED : User32.MenuItemState.MFS_ENABLED;
                        this._miiShowSideBar.lpmii.fState = this.Settings.System.ShowSideBar ? User32.MenuItemState.MFS_CHECKED : User32.MenuItemState.MFS_ENABLED;
                        this._miiShowStatusBar.lpmii.fState = this.Settings.System.ShowStatusBar ? User32.MenuItemState.MFS_CHECKED : User32.MenuItemState.MFS_ENABLED;
                        User32.SetMenuItemInfo(hMenu, this._miiShowMenuBar.fByPosition, true, in this._miiShowMenuBar.lpmii);
                        User32.SetMenuItemInfo(hMenu, this._miiShowToolBar.fByPosition, true, in this._miiShowToolBar.lpmii);
                        User32.SetMenuItemInfo(hMenu, this._miiShowSideBar.fByPosition, true, in this._miiShowSideBar.lpmii);
                        User32.SetMenuItemInfo(hMenu, this._miiShowStatusBar.fByPosition, true, in this._miiShowStatusBar.lpmii);
                        break;
                    }

                // システムメニューの項目が選択されたとき
                case User32.WindowMessage.WM_SYSCOMMAND:
                    {
                        var settings = this.Settings.System;
                        var wID = wParam.ToInt32();
                        if (this._miiShowMenuBar.lpmii.wID == wID)
                            settings.ShowMenuBar = !settings.ShowMenuBar;
                        if (this._miiShowToolBar.lpmii.wID == wID)
                            settings.ShowToolBar = !settings.ShowToolBar;
                        if (this._miiShowSideBar.lpmii.wID == wID)
                            settings.ShowSideBar = !settings.ShowSideBar;
                        if (this._miiShowStatusBar.lpmii.wID == wID)
                            settings.ShowStatusBar = !settings.ShowStatusBar;
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

    #endregion

    #region 内部クラス

    /// <summary>
    /// フローティング処理における新旧のウィンドウの制御を行います。
    /// </summary>
    public class InterTabClientWrapper : IInterTabClient
    {
        [Dependency]
        public IContainerExtension Container { get; set; }

        [LogInterceptor]
        INewTabHost<Window> IInterTabClient.GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        {
            // INFO: IsHeaderPanelVisible = false の状態でフローティングを行うと例外が発生する場合がある問題への対応
            // アプリ側の仕様でタブが一つのみの場合にタブバーを非表示にする機能がある。
            // この状態ではタブの受け入れができずフローティング時に例外となってしまう。
            // これを避けるため、ドラッグ移動中(マウスの左ボタンが押下されている間)はタブを常に表示させる。
            //
            // Dragablz/Dragablz/TabablzControl.cs | 6311e72 on 16 Aug 2017 | Line 1330:
            //   _dragablzItemsControl.InstigateDrag(interTabTransfer.Item, newContainer =>
            var view = this.Container.Resolve<MainWindow>();
            view.IsFloatingWindow = true;
            if (view.Settings.System.ShowSingleTab == false)
            {
                // フローティングが終了するタイミングはドラッグ終了時とウィンドウクローズ時がある
                // (既存のタブコントロールにドッキングされるとウィンドウがクローズする)
                void floatingFinished(object sender, EventArgs e)
                {
                    view.Settings.System.ShowSingleTab = false;
                    ((Window)sender).PreviewMouseLeftButtonUp -= floatingFinished;
                    ((Window)sender).Closed -= floatingFinished;
                }
                view.Settings.System.ShowSingleTab = true;
                view.PreviewMouseLeftButtonUp += floatingFinished;
                view.Closed += floatingFinished;
            }
            return new NewTabHost<Window>(view, view.MainContent);
        }

        [LogInterceptor]
        TabEmptiedResponse IInterTabClient.TabEmptiedHandler(TabablzControl tabControl, Window window)
            => TabEmptiedResponse.CloseWindowOrLayoutBranch;
    }

    /// <summary>
    /// <see cref="ICSharpCode.AvalonEdit.Search.SearchPanel"/> のラベルテキストを多言語対応します。
    /// </summary>
    public class LocalizationWrapper : ICSharpCode.AvalonEdit.Search.Localization
    {
        public override string MatchCaseText => Properties.Resources.Label_CaseSensitive;
        public override string MatchWholeWordsText => string.Empty;
        public override string UseRegexText => Properties.Resources.Label_UseRegex;
        public override string FindNextText => Properties.Resources.Command_FindNext;
        public override string FindPreviousText => Properties.Resources.Command_FindPrev;
        public override string ErrorText => $"{Properties.Resources.Message_NotifyErrorText}: ";
        public override string NoMatchesFoundText => Properties.Resources.Message_NotifyNoMatchesText;

#pragma warning disable CA1822
        public string SwitchFindModeText => Properties.Resources.Command_SwitchFindMode;
        public string FindText => Properties.Resources.Command_Find;
        public string ReplaceText => Properties.Resources.Command_Replace;
        public string ReplaceNextText => Properties.Resources.Command_ReplaceNext;
        public string ReplaceAllText => Properties.Resources.Command_ReplaceAll;
        public string CloseText => Properties.Resources.Command_Close;
#pragma warning restore CA1822
    }

    #endregion
}
