using ParcelManager.Models;
using ParcelManager.Services;

using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ParcelManager.Views
{
    public partial class ProjectView : UserControl
    {
        private readonly ProjectService _projectService;
        private readonly DrawingService _drawingService;
        private readonly SyncService _syncService;

        private string? _projectRootFolder;
        private string? _masterDrawingPath;


        public ProjectView(
            ProjectService projectService,
            DrawingService drawingService,
            SyncService syncService)
        {
            InitializeComponent();

            _projectService =
                projectService;

            _drawingService =
                drawingService;

            _syncService =
                syncService;
        }


        // ============================================================
        // BROWSE PROJECT ROOT
        // ============================================================

        private void BrowseRoot_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog =
                new Microsoft.Win32.OpenFileDialog
                {
                    Title =
                        "Select Project Root Folder",

                    CheckFileExists =
                        false,

                    FileName =
                        "Select Folder"
                };

            if (dialog.ShowDialog() == true)
            {
                string? folder =
                    System.IO.Path.GetDirectoryName(
                        dialog.FileName);

                if (!string.IsNullOrWhiteSpace(
                    folder))
                {
                    LoadProjectFolder(
                        folder);
                }
            }
        }


        // ============================================================
        // LOAD PROJECT
        // ============================================================

        private void LoadProjectFolder(
            string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(
                rootFolder))
            {
                return;
            }

            if (!System.IO.Directory.Exists(
                rootFolder))
            {
                MessageBox.Show(
                    "The selected folder does not exist.",
                    "Invalid Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            _projectRootFolder =
                rootFolder;

            RootFolderTextBox.Text =
                rootFolder;

            _masterDrawingPath =
                _projectService.FindMasterDrawing(
                    rootFolder);

            var barangays =
                _projectService.GetBarangayDrawings(
                    rootFolder);

            BarangayList.ItemsSource =
                barangays;
        }


        // ============================================================
        // VIEW MASTER
        // ============================================================

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
                _projectService.FindMasterDrawing(
                    rootFolder);

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

            OpenDrawing(
                _masterDrawingPath);
        }


        // ============================================================
        // BARANGAY DOUBLE CLICK
        // ============================================================

        private void BarangayList_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (BarangayList.SelectedItem
                is not Barangay barangay)
            {
                return;
            }

            OpenDrawing(
                barangay.DrawingPath);
        }


        // ============================================================
        // OPEN DRAWING
        // ============================================================

        private void OpenDrawing(
            string drawingPath)
        {
            if (!_drawingService.DrawingExists(
                drawingPath))
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

            if (!_drawingService.OpenDrawing(
                drawingPath))
            {
                MessageBox.Show(
                    $"Could not open the drawing.\n\n" +
                    drawingPath,
                    "Open Drawing Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // ============================================================
        // SYNC
        // ============================================================

        private async void Sync_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? rootFolder =
                GetSelectedRootFolder();

            if (rootFolder == null)
            {
                return;
            }

            // --------------------------------------------------------
            // GET MAIN WINDOW
            // --------------------------------------------------------

            Window? mainWindow =
                Window.GetWindow(this);

            // --------------------------------------------------------
            // CREATE SYNC DIALOG
            // --------------------------------------------------------

            var syncDialog =
                new SyncDialog();

            syncDialog.Owner =
                mainWindow;

            // --------------------------------------------------------
            // DISABLE MAIN WINDOW
            // --------------------------------------------------------

            if (mainWindow != null)
            {
                mainWindow.IsEnabled =
                    false;
            }

            // --------------------------------------------------------
            // SHOW SYNC DIALOG
            // --------------------------------------------------------

            syncDialog.Show();

            try
            {
                // ----------------------------------------------------
                // GET LOCAL BARANGAYS
                // ----------------------------------------------------

                var barangays =
                    _projectService.GetBarangayDrawings(
                        rootFolder);

                // ----------------------------------------------------
                // RUN SYNC
                // ----------------------------------------------------

                int syncedCount =
                    await _syncService.SyncAsync(
                        rootFolder,
                        barangays);

                // ----------------------------------------------------
                // CLOSE SYNC DIALOG FIRST
                // ----------------------------------------------------

                if (syncDialog.IsVisible)
                {
                    syncDialog.Close();
                }

                // ----------------------------------------------------
                // ENABLE MAIN WINDOW
                // ----------------------------------------------------

                if (mainWindow != null)
                {
                    mainWindow.IsEnabled =
                        true;
                }

                // ----------------------------------------------------
                // REFRESH PROJECT
                // ----------------------------------------------------

                RefreshProject();

                // ----------------------------------------------------
                // NOTHING CHANGED
                // ----------------------------------------------------

                if (syncedCount == 0)
                {
                    MessageBox.Show(
                        "All parcel drawings are already up to date.",
                        "Sync Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                // ----------------------------------------------------
                // FILES SYNCHRONIZED
                // ----------------------------------------------------

                MessageBox.Show(
                    $"{syncedCount} parcel drawing(s) synchronized successfully.",
                    "Sync Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                // ----------------------------------------------------
                // CLOSE SYNC DIALOG
                // ----------------------------------------------------

                if (syncDialog.IsVisible)
                {
                    syncDialog.Close();
                }

                // ----------------------------------------------------
                // ENABLE MAIN WINDOW
                // ----------------------------------------------------

                if (mainWindow != null)
                {
                    mainWindow.IsEnabled =
                        true;
                }

                MessageBox.Show(
                    $"GIS server request failed.\n\n{ex.Message}",
                    "Sync Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // ----------------------------------------------------
                // CLOSE SYNC DIALOG
                // ----------------------------------------------------

                if (syncDialog.IsVisible)
                {
                    syncDialog.Close();
                }

                // ----------------------------------------------------
                // ENABLE MAIN WINDOW
                // ----------------------------------------------------

                if (mainWindow != null)
                {
                    mainWindow.IsEnabled =
                        true;
                }

                MessageBox.Show(
                    $"Sync failed.\n\n{ex.Message}",
                    "Sync Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // ============================================================
        // REFRESH
        // ============================================================

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


        // ============================================================
        // GET PROJECT ROOT
        // ============================================================

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
    }
}