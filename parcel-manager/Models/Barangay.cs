
using System.IO;

namespace ParcelManager.Models
{
    public class Barangay
    {
        public string Name { get; }

        public string DrawingPath { get; }

        public string DrawingName =>
            Path.GetFileName(DrawingPath);

        public string Status { get; }


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

