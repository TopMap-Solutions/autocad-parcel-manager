using System;
using System.Diagnostics;
using System.IO;

namespace ParcelManager.Services
{
    public class DrawingService
    {

        public bool OpenDrawing(string drawingPath)
        {
            if (string.IsNullOrWhiteSpace(drawingPath))
            {
                return false;
            }

            if (!File.Exists(drawingPath))
            {
                return false;
            }

            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = drawingPath,
                        UseShellExecute = true
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DrawingExists(string drawingPath)
        {
            return !string.IsNullOrWhiteSpace(drawingPath)
                   && File.Exists(drawingPath);
        }
    }
}

