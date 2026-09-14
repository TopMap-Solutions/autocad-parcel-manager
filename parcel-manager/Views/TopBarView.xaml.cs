using System;
using System.Windows;
using System.Windows.Controls;

namespace ParcelManager.Views
{
    public partial class TopBarView : UserControl
    {
        public event Action? TestViewRequested;
        public event Action? LogoutRequested;

        public TopBarView()
        {
            InitializeComponent();
        }

        private void TestView_Click(
            object sender,
            RoutedEventArgs e)
        {
            TestViewRequested?.Invoke();
        }

        private void Logout_Click(
            object sender,
            RoutedEventArgs e)
        {
            LogoutRequested?.Invoke();
        }
    }
}