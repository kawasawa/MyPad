using System.Windows.Controls;

namespace MyPad.Views.Dialogs;

/// <summary>
/// ChangePomodoroTimerDialog.xaml の相互作用ロジック
/// </summary>
public partial class ChangePomodoroTimerDialog : UserControl
{
    [LogInterceptor]
    public ChangePomodoroTimerDialog()
    {
        InitializeComponent();
    }
}
