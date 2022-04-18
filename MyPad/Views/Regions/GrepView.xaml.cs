using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyPad.Views.Regions;

/// <summary>
/// GrepView.xaml の相互作用ロジック
/// </summary>
public partial class GrepView : UserControl
{
    [LogInterceptor]
    public GrepView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Grep 結果のレコードがダブルクリックされたときに行う処理を定義します。
    /// </summary>
    /// <param name="sender">イベントの発生源</param>
    /// <param name="e">イベントの情報</param>
    [LogInterceptor]
    private async void GrepResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled)
            return;

        var mainWindow = MvvmHelper.GetActiveMainWindow();
        if (mainWindow == null)
            return;

        dynamic content = ((ListBoxItem)sender).Content;
        var path = (string)content.path;
        var line = (int)content.line;
        var encoding = (Encoding)content.encoding;
        await mainWindow.ViewModel.InvokeLoad(path, encoding);

        // ViewModel が変更されてから View へ反映されるまでにラグがあるため待機する
        while (mainWindow.ActiveTextEditor?.DataContext != mainWindow.ViewModel.ActiveTextEditor.Value)
            await Task.Delay(100);
        mainWindow.ActiveTextEditor.Line = line;
        mainWindow.ScrollToCaret();

        e.Handled = true;
    }
}
