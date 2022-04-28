using MyPad.Models;
using System.Windows;
using System.Windows.Controls;
using Unity;

namespace MyPad.Views.Regions;

/// <summary>
/// DiffContentView.xaml の相互作用ロジック
/// </summary>
public partial class DiffContentView : UserControl
{
    [Dependency]
    public SettingsModel Settings { get; set; }

    [LogInterceptor]
    public DiffContentView()
    {
        InitializeComponent();
    }

    [LogInterceptor]
    private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isVisible && isVisible)
        {
            if (this.Settings.Misc?.ShowInlineDiffViewer == true)
                this.DiffViewer.ShowInline();
            else
                this.DiffViewer.ShowSideBySide();
        }
    }

    [LogInterceptor]
    private void ShowInlineDiffViewer_Checked(object sender, RoutedEventArgs e)
    {
        this.DiffViewer.ShowInline();
    }

    [LogInterceptor]
    private void ShowInlineDiffViewer_Unchecked(object sender, RoutedEventArgs e)
    {
        this.DiffViewer.ShowSideBySide();
    }
}
