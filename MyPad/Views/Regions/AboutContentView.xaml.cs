using Plow.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Unity;

namespace MyPad.Views.Regions
{
    /// <summary>
    /// AboutContentView.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutContentView : UserControl
    {
        [Dependency]
        public ILoggerFacade Logger { get; set; }

        [LogInterceptor]
        public AboutContentView()
        {
            InitializeComponent();
        }

        [LogInterceptor]
        private void Hyperlink_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                this.Logger.Log($"ハイパーリンクを開きます。: Hyperlink={e.Parameter}", Category.Info);
                Process.Start(new ProcessStartInfo("cmd", $"/c start {e.Parameter}") { CreateNoWindow = true });
            }
            catch (Exception ex)
            {
                this.Logger.Log($"ハイパーリンクを開けませんでした。: Hyperlink={e.Parameter}", Category.Warn, ex);
            }
        }
    }
}
