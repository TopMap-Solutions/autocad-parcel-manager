using System.Windows;

using ParcelManager.Services;

namespace ParcelManager.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();

            _authService = new AuthService();
        }

        private void ShowPassword_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                VisiblePasswordBox.Text = PasswordBox.Password;

                PasswordBox.Visibility = Visibility.Collapsed;
                VisiblePasswordBox.Visibility = Visibility.Visible;

                ShowPasswordButton.Content = "◉";
            }
            else
            {
                PasswordBox.Password = VisiblePasswordBox.Text;

                VisiblePasswordBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;

                ShowPasswordButton.Content = "◉";
            }
        }

        private async void Login_Click(
            object sender,
            RoutedEventArgs e)
        {
            ErrorText.Text = "";

            string username = UsernameTextBox.Text.Trim();

            string password =
                PasswordBox.Visibility == Visibility.Visible
                    ? PasswordBox.Password
                    : VisiblePasswordBox.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                ErrorText.Text =
                    "Please enter your username.";

                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text =
                    "Please enter your password.";

                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Logging in...";

            try
            {
                bool success =
                    await _authService.LoginAsync(
                        username,
                        password);

                if (!success)
                {
                    ErrorText.Text =
                        "Invalid username or password.";

                    return;
                }

                var mainWindow = new MainWindow(_authService);

                Application.Current.MainWindow =
                    mainWindow;

                mainWindow.Show();

                Close();
            }
            catch
            {
                ErrorText.Text =
                    "Unable to connect to the server.";
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Login";
            }
        }
    }
}