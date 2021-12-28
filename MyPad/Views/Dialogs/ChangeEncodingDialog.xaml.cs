using System.Windows.Controls;

namespace MyPad.Views.Dialogs
{
    /// <summary>
    /// ChangeEncodingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ChangeEncodingDialog : UserControl
    {
        [LogInterceptor]
        public ChangeEncodingDialog()
        {
            InitializeComponent();
        }
    }
}
