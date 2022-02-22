using System.Windows.Controls;

namespace MyPad.Views.Regions;

/// <summary>
/// ToolBarView.xaml の相互作用ロジック
/// </summary>
public partial class ToolBarView : UserControl
{
    [LogInterceptor]
    public ToolBarView()
    {
        InitializeComponent();
    }
}
