using System.Windows.Controls;

namespace MyPad.Views.Dialogs;

/// <summary>
/// ConfirmDialog.xaml の相互作用ロジック
/// </summary>
public partial class ConfirmDialog : UserControl
{
    [LogInterceptor]
    public ConfirmDialog()
    {
        InitializeComponent();
    }
}
