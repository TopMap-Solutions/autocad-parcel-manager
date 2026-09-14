using System;
using System.Net.Http;
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

        public MainWindow()
        {
            InitializeComponent();

            _authService = new AuthService();
            _projectService = new ProjectService();
            _drawingService = new DrawingService();
            _syncService = new SyncService();
            _dxfTestService = new DxfService();

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
        // TEST VIEW
        // ============================================================

        private void TestView_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowTestView();
        }


        // ============================================================
        // LOGOUT
        // ============================================================

        private void Logout_Click(
            object sender,
            RoutedEventArgs e)
        {
            _authService.Logout();
        }
    }
}