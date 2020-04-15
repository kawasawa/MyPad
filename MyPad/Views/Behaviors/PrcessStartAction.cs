using Microsoft.Xaml.Behaviors;
using System.Diagnostics;
using System.Windows;

namespace MyPad.Views.Behaviors
{
    public class ProcessStartAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty FileNameProperty = DependencyPropertyExtensions.RegisterDependencyProperty();
        public static readonly DependencyProperty ArgumentsProperty = DependencyPropertyExtensions.RegisterDependencyProperty();
        public static readonly DependencyProperty CreateNoWindowProperty = DependencyPropertyExtensions.RegisterDependencyProperty();
        public static readonly DependencyProperty ThrowExceptionProperty = DependencyPropertyExtensions.RegisterDependencyProperty(new PropertyMetadata(true));

        public string FileName
        {
            get => (string)this.GetValue(FileNameProperty);
            set => this.SetValue(FileNameProperty, value);
        }

        public string Arguments
        {
            get => (string)this.GetValue(ArgumentsProperty);
            set => this.SetValue(ArgumentsProperty, value);
        }

        public bool CreateNoWindow
        {
            get => (bool)this.GetValue(CreateNoWindowProperty);
            set => this.SetValue(CreateNoWindowProperty, value);
        }

        public bool ThrowException
        {
            get => (bool)this.GetValue(ThrowExceptionProperty);
            set => this.SetValue(ThrowExceptionProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            try
            {
                var info = new ProcessStartInfo(this.FileName, this.Arguments);
                info.CreateNoWindow = this.CreateNoWindow;
                Process.Start(info);
            }
            catch when (this.ThrowException == false)
            {
            }
        }
    }
}
