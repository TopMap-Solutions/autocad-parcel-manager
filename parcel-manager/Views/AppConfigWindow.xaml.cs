using System.Windows;

using ParcelManager.Models;
using ParcelManager.Services;

namespace ParcelManager.Views
{
    public partial class AppConfigWindow : Window
    {
        private readonly ConfigService _configService;

        public AppConfigWindow(
            ConfigService configService)
        {
            InitializeComponent();

            _configService =
                configService;

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            ServerUrlTextBox.Text =
                _configService.Config.BaseUrl;

            CoreConsolePathTextBox.Text =
                _configService.Config.CoreConsolePath;
        }

        private void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            string url =
                ServerUrlTextBox.Text.Trim();

            string coreConsolePath =
                CoreConsolePathTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(
                    "Please enter the server URL.",
                    "Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(coreConsolePath))
            {
                MessageBox.Show(
                    "Please enter the AutoCAD Core Path.",
                    "Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _configService.Config.BaseUrl =
                url;

            _configService.Config.CoreConsolePath =
                coreConsolePath;

            _configService.Save();

            Close();
        }

        private void Reset_Click(
            object sender,
            RoutedEventArgs e)
        {
            var defaults =
                new AppConfig();

            ServerUrlTextBox.Text =
                defaults.BaseUrl;

            CoreConsolePathTextBox.Text =
                defaults.CoreConsolePath;
        }

        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}