using System;
using System.Windows;
using System.Windows.Controls;

using ParcelManager.Services;

namespace ParcelManager.Views
{
    public partial class TopBarView : UserControl
    {
        public event Action? TestViewRequested;
        public event Action? LogoutRequested;

        private AuthService? _authService;

        public AuthService? AuthService
        {
            get => _authService;

            set
            {
                _authService = value;
                UpdateUsername();
            }
        }

        public TopBarView()
        {
            InitializeComponent();
        }

        private void UpdateUsername()
        {
            if (_authService == null)
            {
                UsernameText.Text = "";
                return;
            }

            UsernameText.Text =
                _authService.GetUsername() ?? "";
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