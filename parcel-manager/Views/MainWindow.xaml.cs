using System.Windows;

using ParcelManager.Services;

namespace ParcelManager.Views
{
    public partial class MainWindow : Window
    {
        private readonly AuthService _authService;
        private readonly ConfigService _configService;

        private readonly ProjectService _projectService;
        private readonly DrawingService _drawingService;
        private readonly SyncService _syncService;
        private readonly DxfService _dxfTestService;

        public MainWindow(
            AuthService authService,
            ConfigService configService)
        {
            InitializeComponent();

            _authService = 
                authService;

            _configService =
                configService;

            TopBar.AuthService =
                _authService;

            _projectService =
                new ProjectService();

            _drawingService =
                new DrawingService();

            _syncService =
                new SyncService(
                    _configService,
                    _authService);

            _dxfTestService =
                new DxfService(
                    _configService);

            TopBar.LogoutRequested +=
                Logout;

            ShowProjectView();
        }

        private void ShowProjectView()
        {
            MainContent.Content =
                new ProjectView(
                    _projectService,
                    _drawingService,
                    _syncService);
        }

        private void Logout()
        {
            _authService.Logout();

            var loginWindow =
                new LoginWindow();

            Application.Current.MainWindow =
                loginWindow;

            loginWindow.Show();

            Close();
        }
    }
}