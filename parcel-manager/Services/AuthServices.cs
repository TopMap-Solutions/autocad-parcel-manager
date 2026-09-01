using System.Windows;

namespace ParcelManager.Services
{
    public class AuthService
    {
        public bool Logout()
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return false;
            }

            Application.Current.Shutdown();

            return true;
        }
    }
}