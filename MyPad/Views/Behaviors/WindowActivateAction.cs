using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace MyPad.Views.Behaviors
{
    public class WindowActivateAction : TriggerAction<Window>
    {
        protected override void Invoke(object parameter)
        {
            this.AssociatedObject.SetForegroundWindow();
        }
    }
}
