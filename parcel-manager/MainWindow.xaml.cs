using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ParcelManager.Models;
using ParcelManager.Services;

namespace ParcelManager
{
    public partial class MainWindow : Window
    {
        private readonly AuthService _authService;
        private readonly ProjectService _projectService;
        private readonly DrawingService _drawingService;


        private string? _projectRootFolder;
        private string? _masterDrawingPath;


        public MainWindow()
        {
            InitializeComponent();

            _authService = new AuthService();
            _projectService = new ProjectService();
            _drawingService = new DrawingService();
        }


        private void BrowseRoot_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Project Root Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadProjectFolder(dialog.FolderName);
            }
        }

        private void LoadProjectFolder(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
            {
                return;
            }

            if (!System.IO.Directory.Exists(rootFolder))
            {
                MessageBox.Show(
                    "The selected folder does not exist.",
                    "Invalid Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            _projectRootFolder = rootFolder;

            RootFolderTextBox.Text = rootFolder;

            _masterDrawingPath =
                _projectService.FindMasterDrawing(rootFolder);

            var barangays =
                _projectService.GetBarangayDrawings(rootFolder);

            BarangayList.ItemsSource = barangays;
        }


        private void ViewMaster_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? rootFolder =
                GetSelectedRootFolder();

            if (rootFolder == null)
            {
                return;
            }

            _masterDrawingPath =
                _projectService.FindMasterDrawing(rootFolder);

            if (string.IsNullOrWhiteSpace(
                _masterDrawingPath))
            {
                MessageBox.Show(
                    "MASTER.dwg was not found in the project root folder.",
                    "Master Drawing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            OpenDrawing(_masterDrawingPath);
        }


        private void BarangayList_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (BarangayList.SelectedItem
                is not Barangay barangay)
            {
                return;
            }

            OpenDrawing(barangay.DrawingPath);
        }


        private void OpenDrawing(string drawingPath)
        {
            if (!_drawingService.DrawingExists(drawingPath))
            {
                MessageBox.Show(
                    "The drawing file no longer exists.\n\n" +
                    drawingPath,
                    "Drawing Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                RefreshProject();

                return;
            }

            if (!_drawingService.OpenDrawing(drawingPath))
            {
                MessageBox.Show(
                    $"Could not open the drawing.\n\n" +
                    drawingPath,
                    "Open Drawing Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void Sync_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? rootFolder =
                GetSelectedRootFolder();

            if (rootFolder == null)
            {
                return;
            }

            RefreshProject();

            int drawingCount =
                BarangayList.Items.Count;

            string masterStatus =
                _masterDrawingPath != null
                    ? "Found"
                    : "Not found";

            MessageBox.Show(
                $"Project refreshed successfully.\n\n" +
                $"Master drawing: {masterStatus}\n" +
                $"Barangay drawings: {drawingCount}",
                "Sync Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        private void RefreshProject()
        {
            if (string.IsNullOrWhiteSpace(
                _projectRootFolder))
            {
                return;
            }

            LoadProjectFolder(
                _projectRootFolder);
        }


        private string? GetSelectedRootFolder()
        {
            if (string.IsNullOrWhiteSpace(
                _projectRootFolder))
            {
                MessageBox.Show(
                    "Please select the project root folder first.",
                    "Project Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return null;
            }

            if (!System.IO.Directory.Exists(
                _projectRootFolder))
            {
                MessageBox.Show(
                    "The selected project folder no longer exists.",
                    "Project Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return null;
            }

            return _projectRootFolder;
        }


        private void Logout_Click(
            object sender,
            RoutedEventArgs e)
        {
            _authService.Logout();
        }
    }
}
