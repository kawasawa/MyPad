using System.Windows.Controls;

namespace MyPad.Views.Dialogs;

/// <summary>
/// NotifyDialog.xaml の相互作用ロジック
/// </summary>
public partial class NotifyDialog : UserControl
{
    [LogInterceptor]
    public NotifyDialog()
    {
        InitializeComponent();
    }
}
