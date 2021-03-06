﻿using System;
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
    /// ChangeSyntaxDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ChangeSyntaxDialog : UserControl
    {
        [LogInterceptor]
        public ChangeSyntaxDialog()
        {
            InitializeComponent();
        }

        [LogInterceptor]
        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Syntax.Focus();
        }
    }
}
