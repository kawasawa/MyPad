using System.Windows.Controls;

namespace MyPad.Views.Dialogs
{
    /// <summary>
    /// WarnDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class WarnDialog : UserControl
    {
        [LogInterceptor]
        public WarnDialog()
        {
            InitializeComponent();
        }
    }
}
