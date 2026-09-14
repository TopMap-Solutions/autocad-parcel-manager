using System;
using System.Windows;
using System.Windows.Controls;

namespace ParcelManager.Views
{
    public partial class TestView : UserControl
    {
        private readonly Action _backAction;

        public TestView(Action backAction)
        {
            InitializeComponent();

            _backAction = backAction;
        }


        private void Back_Click(
            object sender,
            RoutedEventArgs e)
        {
            _backAction();
        }
    }
}