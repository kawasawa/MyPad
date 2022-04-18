using MyPad.ViewModels.Regions;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyPad.Views.Regions;

/// <summary>
/// ExplorerView.xaml の相互作用ロジック
/// </summary>
public partial class ExplorerView : UserControl
{
    private ExplorerViewModel ViewModel => (ExplorerViewModel)this.DataContext;

    [LogInterceptor]
    public ExplorerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// コントロールがロードされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.ViewModel.FileTreeNodes.Any() == false)
            this.ViewModel.RecreateExplorer();
    }

    /// <summary>
    /// ファイルツリーノードが右クリックされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void FileTreeNode_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled)
            return;

        if ((e.OriginalSource as DependencyObject)?.Ancestor().FirstOrDefault(d => d is TreeViewItem) is not TreeViewItem item)
            return;

        item.IsSelected = true;
        e.Handled = true;
    }

    /// <summary>
    /// ファイルツリーノードがダブルクリックされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void FileTreeNode_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled)
            return;

        var mainWindow = MvvmHelper.GetActiveMainWindow();
        if (mainWindow == null)
            return;

        var node = (ExplorerViewModel.FileTreeNode)((TreeViewItem)sender).DataContext;
        if (File.Exists(node.FileName))
        {
            _ = mainWindow.ViewModel.InvokeLoad(node.FileName);
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// ファイルツリーノード上でキーが押されたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private void FileTreeNode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                {
                    var node = (ExplorerViewModel.FileTreeNode)((TreeViewItem)sender).DataContext;
                    if (node.IsDummy)
                    {
                        e.Handled = true;
                        return;
                    }
                    if (File.Exists(node.FileName))
                    {
                        _ = MvvmHelper.GetActiveMainWindow()?.ViewModel.InvokeLoad(node.FileName);
                        e.Handled = true;
                        return;
                    }
                    if (Directory.Exists(node.FileName))
                    {
                        node.IsExpanded = !node.IsExpanded;
                        e.Handled = true;
                        return;
                    }
                    break;
                }
        }
    }
}
