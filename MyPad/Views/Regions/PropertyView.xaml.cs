using System.Windows.Controls;

namespace MyPad.Views.Regions;

/// <summary>
/// PropertyView.xaml の相互作用ロジック
/// </summary>
public partial class PropertyView : UserControl
{
    [LogInterceptor]
    public PropertyView()
    {
        InitializeComponent();
    }
}
