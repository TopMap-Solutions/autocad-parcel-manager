using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace _12_file_manager
{
    public partial class MainWindow : Window
    {
        // =========================================
        // PROJECT STATE
        // =========================================

        private string _projectRootFolder;

        private string _masterDrawingPath;


        // =========================================
        // CONSTRUCTOR
        // =========================================

        public MainWindow()
        {
            InitializeComponent();
        }


        // =========================================
        // BROWSE ROOT FOLDER
        // =========================================

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


        // =========================================
        // LOAD PROJECT
        // =========================================

        private void LoadProjectFolder(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
            {
                return;
            }


            if (!Directory.Exists(rootFolder))
            {
                MessageBox.Show(
                    "The selected folder does not exist.",
                    "Invalid Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }


            // Store project root
            _projectRootFolder = rootFolder;

            RootFolderTextBox.Text = rootFolder;


            // Find MASTER.dwg
            LoadMasterDrawing(rootFolder);


            // Find all other DWG files
            LoadBarangayDrawings(rootFolder);
        }


        // =========================================
        // LOAD MASTER DRAWING
        // =========================================

        private void LoadMasterDrawing(string rootFolder)
        {
            _masterDrawingPath =
                FindMasterDrawing(rootFolder);
        }


        // =========================================
        // FIND MASTER.DWG
        // =========================================

        private string FindMasterDrawing(
            string rootFolder)
        {
            try
            {
                return Directory
                    .GetFiles(
                        rootFolder,
                        "*.dwg",
                        SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(file =>
                        string.Equals(
                            Path.GetFileName(file),
                            "MASTER.dwg",
                            StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }


        // =========================================
        // LOAD BARANGAY DRAWINGS
        // =========================================

        private void LoadBarangayDrawings(
            string rootFolder)
        {
            var drawings = new List<Barangay>();


            try
            {
                /*
                 * Only read DWG files directly inside
                 * the selected project root.
                 *
                 * Subfolders are NOT scanned.
                 *
                 * MASTER.dwg is excluded.
                 */

                var dwgFiles = Directory
                    .GetFiles(
                        rootFolder,
                        "*.dwg",
                        SearchOption.TopDirectoryOnly)
                    .Where(file =>
                        !IsMasterDrawing(file))
                    .OrderBy(
                        file => Path.GetFileName(file),
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();


                foreach (string dwgFile in dwgFiles)
                {
                    string drawingName =
                        Path.GetFileNameWithoutExtension(
                            dwgFile);


                    drawings.Add(
                        new Barangay(
                            drawingName,
                            dwgFile));
                }


                // Update UI
                BarangayList.ItemsSource = drawings;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Access to the selected project folder was denied.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                BarangayList.ItemsSource =
                    new List<Barangay>();
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    $"Could not read the project folder.\n\n{ex.Message}",
                    "File Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                BarangayList.ItemsSource =
                    new List<Barangay>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                BarangayList.ItemsSource =
                    new List<Barangay>();
            }
        }


        // =========================================
        // CHECK IF MASTER DRAWING
        // =========================================

        private bool IsMasterDrawing(
            string drawingPath)
        {
            return string.Equals(
                Path.GetFileName(drawingPath),
                "MASTER.dwg",
                StringComparison.OrdinalIgnoreCase);
        }


        // =========================================
        // VIEW MASTER DRAWING
        // =========================================

        private void ViewMaster_Click(
            object sender,
            RoutedEventArgs e)
        {
            string rootFolder =
                GetSelectedRootFolder();


            if (rootFolder == null)
            {
                return;
            }


            /*
             * Search again so the button always
             * uses the current MASTER.dwg.
             */

            _masterDrawingPath =
                FindMasterDrawing(rootFolder);


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


        // =========================================
        // DOUBLE CLICK BARANGAY
        // =========================================

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


        // =========================================
        // OPEN DRAWING
        // =========================================

        private void OpenDrawing(
            string drawingPath)
        {
            if (string.IsNullOrWhiteSpace(
                drawingPath))
            {
                return;
            }


            if (!File.Exists(drawingPath))
            {
                MessageBox.Show(
                    "The drawing file no longer exists.\n\n" +
                    drawingPath,
                    "Drawing Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);


                // Refresh the project
                if (!string.IsNullOrWhiteSpace(
                    _projectRootFolder))
                {
                    LoadProjectFolder(
                        _projectRootFolder);
                }


                return;
            }


            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = drawingPath,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not open the drawing.\n\n{ex.Message}",
                    "Open Drawing Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================
        // SYNC / REFRESH PROJECT
        // =========================================

        private void Sync_Click(
            object sender,
            RoutedEventArgs e)
        {
            string rootFolder =
                GetSelectedRootFolder();


            if (rootFolder == null)
            {
                return;
            }


            // Re-scan project folder
            LoadProjectFolder(rootFolder);


            int drawingCount =
                GetBarangayDrawingCount(
                    rootFolder);


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


        // =========================================
        // GET BARANGAY DRAWING COUNT
        // =========================================

        private int GetBarangayDrawingCount(
            string rootFolder)
        {
            try
            {
                return Directory
                    .GetFiles(
                        rootFolder,
                        "*.dwg",
                        SearchOption.TopDirectoryOnly)
                    .Count(file =>
                        !IsMasterDrawing(file));
            }
            catch
            {
                return 0;
            }
        }


        // =========================================
        // GET SELECTED ROOT FOLDER
        // =========================================

        private string GetSelectedRootFolder()
        {
            string rootFolder =
                _projectRootFolder;


            if (string.IsNullOrWhiteSpace(
                rootFolder))
            {
                MessageBox.Show(
                    "Please select the project root folder first.",
                    "Project Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return null;
            }


            if (!Directory.Exists(rootFolder))
            {
                MessageBox.Show(
                    "The selected project folder no longer exists.",
                    "Project Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return null;
            }


            return rootFolder;
        }


        // =========================================
        // LOGOUT
        // =========================================

        private void Logout_Click(
            object sender,
            RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);


            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }


    // =============================================
    // BARANGAY MODEL
    // =============================================

    public class Barangay
    {
        // =========================================
        // PROPERTIES
        // =========================================

        public string Name { get; set; }

        public string DrawingPath { get; set; }

        public string DrawingName =>
            Path.GetFileName(DrawingPath);

        public string Status { get; set; }


        // =========================================
        // CONSTRUCTOR
        // =========================================

        public Barangay(
            string name,
            string drawingPath)
        {
            Name = name;

            DrawingPath = drawingPath;

            Status = "Ready";
        }
    }
}

