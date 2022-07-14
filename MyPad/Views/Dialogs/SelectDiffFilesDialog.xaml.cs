using System.Windows;
using System.Windows.Controls;

namespace MyPad.Views.Dialogs;

/// <summary>
/// SelectDiffFilesDialog.xaml の相互作用ロジック
/// </summary>
public partial class SelectDiffFilesDialog : UserControl
{
    [LogInterceptor]
    public SelectDiffFilesDialog()
    {
        InitializeComponent();
    }

    [LogInterceptorIgnore("本質的な処理では無くログが汚れるため")]
    private void Dialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.DiffSourcePath.SelectedValue == null)
            this.DiffSourcePath.Focus();
        if (this.DiffDestinationPath.SelectedValue == null)
            this.DiffDestinationPath.Focus();
        else
            this.DiffSourcePath.Focus();
    }
}
