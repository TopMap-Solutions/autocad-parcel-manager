using System.Windows;

namespace ParcelManager
{
    public partial class App : Application
    {
        protected override void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginWindow = new Views.LoginWindow();

            MainWindow = loginWindow;

            loginWindow.Show();
        }
    }
}