using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class ProjectService
    {
        private const string MasterDrawingName = "MASTER.dwg";

        public string? FindMasterDrawing(string rootFolder)
        {
            if (!Directory.Exists(rootFolder))
            {
                return null;
            }

            try
            {
                return Directory
                    .GetFiles(
                        rootFolder,
                        "*.dwg",
                        SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(IsMasterDrawing);
            }
            catch
            {
                return null;
            }
        }

        public List<Barangay> GetBarangayDrawings(
            string rootFolder)
        {
            try
            {
                return Directory
                    .GetFiles(
                        rootFolder,
                        "*.dwg",
                        SearchOption.TopDirectoryOnly)
                    .Where(file => !IsMasterDrawing(file))
                    .OrderBy(
                        file => Path.GetFileName(file),
                        StringComparer.OrdinalIgnoreCase)
                    .Select(file =>
                        new Barangay(
                            Path.GetFileNameWithoutExtension(file),
                            file))
                    .ToList();
            }
            catch
            {
                return new List<Barangay>();
            }
        }

        private bool IsMasterDrawing(string drawingPath)
        {
            return string.Equals(
                Path.GetFileName(drawingPath),
                MasterDrawingName,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}

