using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyPad.Views.Regions
{
    /// <summary>
    /// MaintenanceContentView.xaml の相互作用ロジック
    /// </summary>
    public partial class MaintenanceContentView : UserControl
    {
        [LogInterceptor]
        public MaintenanceContentView()
        {
            InitializeComponent();
        }

        [LogInterceptor]
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isVisible && isVisible)
            {
                (this.DataContext as ViewModels.Regions.MaintenanceContentViewModel)?.IsVisibleChangedHandler.Execute(e);
            }
        }
    }
}
