using System.Windows;
using System.Windows.Input;

using ParcelManager.Models;
using ParcelManager.Services;

namespace ParcelManager.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();

            _configService =
                new ConfigService();

            _authService =
                new AuthService(
                    _configService);
        }

        private void ShowPassword_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (PasswordBox.Visibility ==
                Visibility.Visible)
            {
                VisiblePasswordBox.Text =
                    PasswordBox.Password;

                PasswordBox.Visibility =
                    Visibility.Collapsed;

                VisiblePasswordBox.Visibility =
                    Visibility.Visible;

                ShowPasswordButton.Content =
                    "◉";
            }
            else
            {
                PasswordBox.Password =
                    VisiblePasswordBox.Text;

                VisiblePasswordBox.Visibility =
                    Visibility.Collapsed;

                PasswordBox.Visibility =
                    Visibility.Visible;

                ShowPasswordButton.Content =
                    "◉";
            }
        }

        private async void Login_Click(
            object sender,
            RoutedEventArgs e)
        {
            ErrorText.Text = "";

            string username =
                UsernameTextBox.Text.Trim();

            string password =
                PasswordBox.Visibility ==
                Visibility.Visible
                    ? PasswordBox.Password
                    : VisiblePasswordBox.Text;

            if (string.IsNullOrWhiteSpace(
                username))
            {
                ErrorText.Text =
                    "Please enter your username.";

                return;
            }

            if (string.IsNullOrWhiteSpace(
                password))
            {
                ErrorText.Text =
                    "Please enter your password.";

                return;
            }

            LoginButton.IsEnabled =
                false;

            LoginButton.Content =
                "Logging in...";

            try
            {
                LoginResult result =
                    await _authService.LoginAsync(
                        username,
                        password);

                switch (result)
                {
                    case LoginResult.Success:

                        var mainWindow =
                            new MainWindow(
                                _authService,
                                _configService);

                        Application.Current.MainWindow =
                            mainWindow;

                        mainWindow.Show();

                        Close();

                        break;

                    case LoginResult.InvalidCredentials:

                        ErrorText.Text =
                            "Invalid username or password.";

                        break;

                    case LoginResult.ServerUnavailable:

                        ErrorText.Text =
                            "Unable to connect to the server.";

                        break;

                    case LoginResult.Timeout:

                        ErrorText.Text =
                            "The request timed out. Please try again.";

                        break;

                    case LoginResult.Failed:

                        ErrorText.Text =
                            "An unexpected error occurred. Please try again.";

                        break;

                    default:

                        ErrorText.Text =
                            "Login failed. Please try again.";

                        break;
                }
            }
            finally
            {
                LoginButton.IsEnabled =
                    true;

                LoginButton.Content =
                    "Login";
            }
        }

        private void AppConfig_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            var configWindow =
                new AppConfigWindow(
                    _configService);

            configWindow.Owner =
                this;

            configWindow.ShowDialog();
        }
    }
}