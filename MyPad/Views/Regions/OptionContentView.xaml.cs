using MyPad.PubSub;
using Prism.Events;
using System.Windows;
using System.Windows.Controls;
using Unity;

namespace MyPad.Views.Regions;

/// <summary>
/// OptionContentView.xaml の相互作用ロジック
/// </summary>
public partial class OptionContentView : UserControl
{
    [Dependency]
    public IEventAggregator EventAggregator { get; set; }

    [LogInterceptor]
    public OptionContentView()
    {
        InitializeComponent();
    }

    [LogInterceptorIgnore("本質的な処理では無くログが汚れるため")]
    private void EnableNotifyIcon_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (this.IsLoaded)
            this.EventAggregator.GetEvent<RefreshNotifyIconEvent>().Publish();
    }
}
