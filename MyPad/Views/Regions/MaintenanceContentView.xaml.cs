using System.Windows.Controls;

namespace MyPad.Views.Regions;

/// <summary>
/// MaintenanceContentView.xaml の相互作用ロジック
/// </summary>
public partial class MaintenanceContentView : UserControl
{
    [LogInterceptor]
    public MaintenanceContentView()
    {
        InitializeComponent();
    }
}
