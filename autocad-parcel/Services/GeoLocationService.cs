using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_parcel.Models;

namespace autocad_parcel.Services
{
    public class GeoLocationService
    {
        private const string GeographicDataKey =
            "ACAD_GEOGRAPHICDATA";


        public void SetGeoLocation(
            Database db,
            GeoLocationModel model)
        {
            ObjectId modelSpaceId =
                SymbolUtilityServices.GetBlockModelSpaceId(db);

            RemoveExistingGeoLocation(
                db,
                modelSpaceId);

            using (Transaction tr =
                   db.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace =
                    (BlockTableRecord)tr.GetObject(
                        modelSpaceId,
                        OpenMode.ForRead);

                GeoLocationData geoData =
                    new GeoLocationData();

                geoData.BlockTableRecordId =
                    modelSpaceId;

                // IMPORTANT:
                // Post before setting CoordinateSystem.
                geoData.PostToDb();

                geoData.CoordinateSystem =
                    model.CoordinateSystem;

                geoData.TypeOfCoordinates =
                    TypeOfCoordinates.CoordinateTypeLocal;

                geoData.DesignPoint =
                    new Point3d(
                        model.Easting,
                        model.Northing,
                        model.Elevation);

                geoData.ReferencePoint =
                    model.ReferencePoint;

                geoData.NorthDirectionVector =
                    model.NorthDirectionVector;

                geoData.HorizontalUnits =
                    UnitsValue.Meters;

                geoData.HorizontalUnitsScale =
                    1.0;

                geoData.VerticalUnits =
                    UnitsValue.Meters;

                geoData.VerticalUnitsScale =
                    1.0;

                geoData.ScaleEstimationMethod =
                    ScaleEstimationMethod.ScaleEstMethodUnity;

                geoData.ScaleFactor =
                    1.0;

                geoData.DoSeaLevelCorrection =
                    false;

                geoData.SeaLevelElevation =
                    0.0;

                // ----------------------------------------------------
                // Mesh
                // ----------------------------------------------------

                geoData.ResetMeshPointMaps();

                foreach (MeshPointModel mesh in model.MeshPoints)
                {
                    geoData.AddMeshPointMap(
                        mesh.Index,
                        mesh.Source,
                        mesh.Destination);
                }

                // ----------------------------------------------------
                // Let AutoCAD calculate transformation
                // ----------------------------------------------------

                geoData.UpdateTransformationMatrix();

                tr.Commit();
            }
        }


        private void RemoveExistingGeoLocation(
            Database db,
            ObjectId modelSpaceId)
        {
            using (Transaction tr =
                   db.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace =
                    (BlockTableRecord)tr.GetObject(
                        modelSpaceId,
                        OpenMode.ForRead);

                if (!modelSpace.ExtensionDictionary.IsNull)
                {
                    DBDictionary extDict =
                        (DBDictionary)tr.GetObject(
                            modelSpace.ExtensionDictionary,
                            OpenMode.ForRead);

                    if (extDict.Contains(
                        GeographicDataKey))
                    {
                        ObjectId oldGeoId =
                            extDict.GetAt(
                                GeographicDataKey);

                        GeoLocationData oldGeo =
                            (GeoLocationData)tr.GetObject(
                                oldGeoId,
                                OpenMode.ForWrite);

                        oldGeo.Erase();
                    }
                }

                tr.Commit();
            }
        }


        public GeoLocationData? GetGeoLocation(
            Database db,
            Transaction tr)
        {
            ObjectId modelSpaceId =
                SymbolUtilityServices.GetBlockModelSpaceId(db);

            BlockTableRecord modelSpace =
                (BlockTableRecord)tr.GetObject(
                    modelSpaceId,
                    OpenMode.ForRead);

            if (modelSpace.ExtensionDictionary.IsNull)
                return null;

            DBDictionary extDict =
                (DBDictionary)tr.GetObject(
                    modelSpace.ExtensionDictionary,
                    OpenMode.ForRead);

            if (!extDict.Contains(
                GeographicDataKey))
                return null;

            ObjectId geoId =
                extDict.GetAt(
                    GeographicDataKey);

            return
                (GeoLocationData)tr.GetObject(
                    geoId,
                    OpenMode.ForRead);
        }
    }
}