using MyPad.Models;
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
using Unity;

namespace MyPad.Views.Regions
{
    /// <summary>
    /// DiffContentView.xaml の相互作用ロジック
    /// </summary>
    public partial class DiffContentView : UserControl
    {
        #region インジェクション

        [Dependency]
        public Settings Settings { get; set; }

        #endregion

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
                if (this.Settings.OtherTools?.ShowInlineDiffViewer == true)
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
}
