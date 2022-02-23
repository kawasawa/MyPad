using System.Windows.Controls;

namespace MyPad.Views.Dialogs;

/// <summary>
/// ChangeSyntaxDialog.xaml の相互作用ロジック
/// </summary>
public partial class ChangeSyntaxDialog : UserControl
{
    [LogInterceptor]
    public ChangeSyntaxDialog()
    {
        InitializeComponent();
    }
}
