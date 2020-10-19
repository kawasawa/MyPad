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
    /// OptionContentView.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionContentView : UserControl
    {
        public OptionContentView()
        {
            InitializeComponent();
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.IsNewItem ? string.Empty : e.Row.GetIndex().ToString();
        }
    }
}
