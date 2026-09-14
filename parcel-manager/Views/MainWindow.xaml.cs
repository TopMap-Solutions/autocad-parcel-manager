using System.Windows;

using ParcelManager.Services;

namespace ParcelManager.Views
{
    public partial class MainWindow : Window
    {
        private readonly AuthService _authService;
        private readonly ProjectService _projectService;
        private readonly DrawingService _drawingService;
        private readonly SyncService _syncService;
        private readonly DxfService _dxfTestService;

        public MainWindow(AuthService authService)
        {
            InitializeComponent();

            _authService = authService;

            _projectService = new ProjectService();
            _drawingService = new DrawingService();
            _syncService = new SyncService();
            _dxfTestService = new DxfService();

            TopBar.TestViewRequested += ShowTestView;
            TopBar.LogoutRequested += Logout;

            ShowProjectView();
        }

        // ============================================================
        // VIEW NAVIGATION
        // ============================================================

        private void ShowProjectView()
        {
            MainContent.Content = new ProjectView(
                _projectService,
                _drawingService,
                _syncService);
        }

        private void ShowTestView()
        {
            MainContent.Content = new TestView(
                ShowProjectView);
        }

        // ============================================================
        // AUTH
        // ============================================================

        private void Logout()
        {
            _authService.Logout();

            var loginWindow = new LoginWindow();

            Application.Current.MainWindow = loginWindow;

            loginWindow.Show();

            Close();
        }
    }
}