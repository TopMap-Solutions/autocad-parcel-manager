using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace autocad_parcel.Models
{
    public class GeoLocationModel
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Easting { get; set; }

        public double Northing { get; set; }

        public double Elevation { get; set; }

        public string CoordinateSystem { get; set; } = string.Empty;

        public Point3d ReferencePoint { get; set; }

        public Vector2d NorthDirectionVector { get; set; }

        public List<MeshPointModel> MeshPoints { get; set; } = new();
    }


    public class MeshPointModel
    {
        public int Index { get; set; }

        public Point2d Source { get; set; }

        public Point2d Destination { get; set; }
    }
}