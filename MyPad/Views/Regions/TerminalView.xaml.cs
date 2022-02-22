using System.Windows.Controls;

namespace MyPad.Views.Regions;

/// <summary>
/// TerminalView.xaml の相互作用ロジック
/// </summary>
public partial class TerminalView : UserControl
{
    [LogInterceptor]
    public TerminalView()
    {
        InitializeComponent();
    }
}
