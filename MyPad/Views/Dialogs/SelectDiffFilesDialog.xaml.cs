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

namespace MyPad.Views.Dialogs
{
    /// <summary>
    /// SelectDiffFilesDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectDiffFilesDialog : UserControl
    {
        public SelectDiffFilesDialog()
        {
            InitializeComponent();
        }

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
}
