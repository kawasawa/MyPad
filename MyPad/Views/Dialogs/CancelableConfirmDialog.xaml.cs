using System.Windows.Controls;

namespace MyPad.Views.Dialogs
{
    /// <summary>
    /// CancelableConfirmDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class CancelableConfirmDialog : UserControl
    {
        [LogInterceptor]
        public CancelableConfirmDialog()
        {
            InitializeComponent();
        }
    }
}
