using System.Windows.Controls;

namespace MyPad.Views.Regions;

/// <summary>
/// ScriptRunnerView.xaml の相互作用ロジック
/// </summary>
public partial class ScriptRunnerView : UserControl
{
    [LogInterceptor]
    public ScriptRunnerView()
    {
        InitializeComponent();
    }
}
