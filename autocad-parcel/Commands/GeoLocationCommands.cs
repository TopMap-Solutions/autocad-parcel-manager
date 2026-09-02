using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using AcadApp =
    Autodesk.AutoCAD.ApplicationServices.Application;

using ParcelManger.Models;
using ParcelManger.Services;


namespace ParcelManger.Commands
{
    public class GeoLocationCommands
    {
        private readonly GeoLocationService _geoService =
            new GeoLocationService();

        private static readonly GeoLocationModel MapConfiguration =
            new GeoLocationModel
            {
                Latitude =
                    7.446114724,

                Longitude =
                    125.807793900,

                Easting =
                    589000.915,

                Northing =
                    823481.4641,

                Elevation =
                    0.0,

                CoordinateSystem =
                    "PRS92.Philippines-5",

                ReferencePoint =
                    new Point3d(
                        589051.5593132314,
                        823505.0602381761,
                        0.0),

                NorthDirectionVector =
                    new Vector2d(
                        0.0,
                        1.0),

                MeshPoints =
                {
                    // ====================================================
                    // MESH POINT 0
                    // ====================================================

                    new MeshPointModel
                    {
                        Index = 0,

                        Source =
                            new Point2d(
                                310736.3078929917,
                                585993.8550816455),

                        Destination =
                            new Point2d(
                                123.2937592845364,
                                5.296780081122221)
                    },


                    // ====================================================
                    // MESH POINT 1
                    // ====================================================

                    new MeshPointModel
                    {
                        Index = 1,

                        Source =
                            new Point2d(
                                689263.6921070083,
                                585993.8550816455),

                        Destination =
                            new Point2d(
                                126.70824035989993,
                                5.296676727942055)
                    },


                    // ====================================================
                    // MESH POINT 2
                    // ====================================================

                    new MeshPointModel
                    {
                        Index = 2,

                        Source =
                            new Point2d(
                                689263.6921070083,
                                1409895.418837635),

                        Destination =
                            new Point2d(
                                126.74389792223572,
                                12.74274396118969)
                    },


                    // ====================================================
                    // MESH POINT 3
                    // ====================================================

                    new MeshPointModel
                    {
                        Index = 3,

                        Source =
                            new Point2d(
                                310736.3078929917,
                                1410719.320401391),

                        Destination =
                            new Point2d(
                                123.25852965386093,
                                12.750303802154598)
                    },


                    // ====================================================
                    // MESH POINT 4
                    // ====================================================

                    new MeshPointModel
                    {
                        Index = 4,

                        Source =
                            new Point2d(
                                499103.34916961286,
                                998356.5877415183),

                        Destination =
                            new Point2d(
                                124.9929610408809,
                                9.027911726901952)
                    },


                    // ====================================================
                    // MESH POINT 5
                    // ====================================================

                    new MeshPointModel
                    {
                        Index = 5,

                        Source =
                            new Point2d(
                                500000.0,
                                748742.6578816351),

                        Destination =
                            new Point2d(
                                125.00104541721251,
                                6.770824662868423)
                    },


                    // ====================================================
                    // MESH POINT 6
                    // ====================================================

                    new MeshPointModel
                    {
                        Index = 6,

                        Source =
                            new Point2d(
                                499646.30643085996,
                                1247808.95059774),

                        Destination =
                            new Point2d(
                                124.99795075860223,
                                11.283263010827639)
                    }
                }
            };


        // ================================================================
        // TMSETMAP
        //
        // Creates the AutoCAD GeoLocationData object.
        // Enables Bing Hybrid.
        // Centers the view on the authoritative CAD coordinate.
        // ================================================================

        [CommandMethod("TMSETMAP")]
        public void SetMap()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // ============================================================
                // 1. Drawing units
                // ============================================================

                AcadApp.SetSystemVariable(
                    "INSUNITS",
                    6);


                // ============================================================
                // 2. Check existing geographic data
                // ============================================================

                bool hasGeoLocation = false;

                using (Transaction tr =
                       db.TransactionManager.StartTransaction())
                {
                    GeoLocationData? existing =
                        _geoService.GetGeoLocation(
                            db,
                            tr);

                    hasGeoLocation =
                        existing != null;

                    tr.Commit();
                }


                // ============================================================
                // 3. Only CREATE geolocation if it doesn't already exist
                // ============================================================

                if (!hasGeoLocation)
                {
                    ed.WriteMessage(
                        "\nCreating AutoCAD geographic data...");

                    _geoService.SetGeoLocation(
                        db,
                        MapConfiguration);
                }
                else
                {
                    ed.WriteMessage(
                        "\nGeographic data already exists. Reusing it.");
                }


                // ============================================================
                // 4. Enable Bing Hybrid
                // ============================================================

                ed.Command(
                    "_.GEOMAP",
                    "_HYBRID");


                // ============================================================
                // 5. Zoom to authoritative CAD coordinate
                // ============================================================

                Point3d target =
                    new Point3d(
                        MapConfiguration.Easting,
                        MapConfiguration.Northing,
                        MapConfiguration.Elevation);


                using (ViewTableRecord view =
                       ed.GetCurrentView())
                {
                    view.CenterPoint =
                        new Point2d(
                            target.X,
                            target.Y);

                    view.Width = 2000.0;
                    view.Height = 2000.0;

                    ed.SetCurrentView(view);
                }


                // ============================================================
                // 6. Output
                // ============================================================

                ed.WriteMessage(
                    "\n========== TMSETMAP ==========");

                ed.WriteMessage(
                    "\nAutoCAD geolocation configured.");

                ed.WriteMessage(
                    $"\nLatitude:  {MapConfiguration.Latitude}");

                ed.WriteMessage(
                    $"\nLongitude: {MapConfiguration.Longitude}");

                ed.WriteMessage(
                    $"\nEasting:   {MapConfiguration.Easting}");

                ed.WriteMessage(
                    $"\nNorthing:  {MapConfiguration.Northing}");

                ed.WriteMessage(
                    $"\nCRS:       {MapConfiguration.CoordinateSystem}");

                ed.WriteMessage(
                    $"\nMesh:      {MapConfiguration.MeshPoints.Count}");

                ed.WriteMessage(
                    "\nBing:      Hybrid");

                ed.WriteMessage(
                    "\n==============================");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(
                    "\nTMSETMAP ERROR:");

                ed.WriteMessage(
                    $"\n{ex.GetType().FullName}");

                ed.WriteMessage(
                    $"\n{ex.Message}");
            }
        }


        // ================================================================
        // TMREADGEODATA
        //
        // Inspects the actual ACAD_GEOGRAPHICDATA stored in the DWG.
        //
        // This does NOT create or modify geolocation data.
        // ================================================================

        [CommandMethod("TMREADGEODATA")]
        public void ReadGeoData()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed =
                doc.Editor;

            Database db =
                doc.Database;


            try
            {
                ed.WriteMessage(
                    "\n========== TMREADGEODATA ==========");


                using (Transaction tr =
                       db.TransactionManager.StartTransaction())
                {
                    // ====================================================
                    // Get existing GeoLocationData
                    // ====================================================

                    GeoLocationData? geoData =
                        _geoService.GetGeoLocation(
                            db,
                            tr);


                    // ====================================================
                    // No geolocation found
                    // ====================================================

                    if (geoData == null)
                    {
                        ed.WriteMessage(
                            "\nNo ACAD_GEOGRAPHICDATA found.");

                        ed.WriteMessage(
                            "\nRun TMSETMAP first.");

                        ed.WriteMessage(
                            "\n===================================");

                        return;
                    }


                    // ====================================================
                    // BASIC DATA
                    // ====================================================

                    ed.WriteMessage(
                        "\n\n========== BASIC DATA ==========");


                    ed.WriteMessage(
                        $"\nCoordinateSystem: " +
                        $"{geoData.CoordinateSystem}");


                    ed.WriteMessage(
                        $"\nTypeOfCoordinates: " +
                        $"{geoData.TypeOfCoordinates}");


                    ed.WriteMessage(
                        $"\nDesignPoint: " +
                        $"{geoData.DesignPoint}");


                    ed.WriteMessage(
                        $"\nReferencePoint: " +
                        $"{geoData.ReferencePoint}");


                    ed.WriteMessage(
                        $"\nNorthDirectionVector: " +
                        $"{geoData.NorthDirectionVector}");


                    ed.WriteMessage(
                        $"\nHorizontalUnits: " +
                        $"{geoData.HorizontalUnits}");


                    ed.WriteMessage(
                        $"\nHorizontalUnitsScale: " +
                        $"{geoData.HorizontalUnitsScale}");


                    ed.WriteMessage(
                        $"\nVerticalUnits: " +
                        $"{geoData.VerticalUnits}");


                    ed.WriteMessage(
                        $"\nVerticalUnitsScale: " +
                        $"{geoData.VerticalUnitsScale}");


                    ed.WriteMessage(
                        $"\nScaleEstimationMethod: " +
                        $"{geoData.ScaleEstimationMethod}");


                    ed.WriteMessage(
                        $"\nScaleFactor: " +
                        $"{geoData.ScaleFactor}");


                    ed.WriteMessage(
                        $"\nDoSeaLevelCorrection: " +
                        $"{geoData.DoSeaLevelCorrection}");


                    ed.WriteMessage(
                        $"\nSeaLevelElevation: " +
                        $"{geoData.SeaLevelElevation}");


                    ed.WriteMessage(
                        $"\nNumMeshPoints: " +
                        $"{geoData.NumMeshPoints}");


                    // ====================================================
                    // MESH POINTS
                    // ====================================================

                    ed.WriteMessage(
                        "\n\n========== MESH POINTS ==========");


                    MeshPointMaps mesh =
                        geoData.GetMeshPointMaps();


                    Point2dCollection source =
                        mesh.SourcePonints;


                    Point2dCollection destination =
                        mesh.DestPonints;


                    ed.WriteMessage(
                        $"\nSource count: {source.Count}");


                    ed.WriteMessage(
                        $"\nDestination count: {destination.Count}");


                    int count =
                        Math.Min(
                            source.Count,
                            destination.Count);


                    for (int i = 0;
                         i < count;
                         i++)
                    {
                        ed.WriteMessage(
                            $"\n\nMesh Point [{i}]");


                        ed.WriteMessage(
                            $"\n  SOURCE = {source[i]}");


                        ed.WriteMessage(
                            $"\n  DEST   = {destination[i]}");
                    }


                    // ====================================================
                    // CAD -> LON/LAT
                    // ====================================================

                    ed.WriteMessage(
                        "\n\n========== TRANSFORMATION TEST ==========");


                    Point3d cadPoint =
                        new Point3d(
                            MapConfiguration.Easting,
                            MapConfiguration.Northing,
                            MapConfiguration.Elevation);


                    ed.WriteMessage(
                        "\n\nCAD POINT:");

                    ed.WriteMessage(
                        $"\n  {cadPoint}");


                    try
                    {
                        Point3d geographicPoint =
                            geoData.TransformToLonLatAlt(
                                cadPoint);


                        ed.WriteMessage(
                            "\n\nAUTOCAD -> LON/LAT:");

                        ed.WriteMessage(
                            $"\n  {geographicPoint}");


                        ed.WriteMessage(
                            "\n\nEXPECTED:");

                        ed.WriteMessage(
                            $"\n  Longitude = " +
                            $"{MapConfiguration.Longitude}");

                        ed.WriteMessage(
                            $"\n  Latitude  = " +
                            $"{MapConfiguration.Latitude}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage(
                            "\nTransformToLonLatAlt ERROR:");

                        ed.WriteMessage(
                            $"\n{ex.Message}");
                    }


                    // ====================================================
                    // LON/LAT -> CAD
                    // ====================================================

                    Point3d expectedGeographicPoint =
                        new Point3d(
                            MapConfiguration.Longitude,
                            MapConfiguration.Latitude,
                            MapConfiguration.Elevation);


                    ed.WriteMessage(
                        "\n\nLON/LAT POINT:");

                    ed.WriteMessage(
                        $"\n  {expectedGeographicPoint}");


                    try
                    {
                        Point3d transformedCadPoint =
                            geoData.TransformFromLonLatAlt(
                                expectedGeographicPoint);


                        ed.WriteMessage(
                            "\n\nLON/LAT -> AUTOCAD:");

                        ed.WriteMessage(
                            $"\n  {transformedCadPoint}");


                        ed.WriteMessage(
                            "\n\nEXPECTED CAD:");

                        ed.WriteMessage(
                            $"\n  Easting  = " +
                            $"{MapConfiguration.Easting}");

                        ed.WriteMessage(
                            $"\n  Northing = " +
                            $"{MapConfiguration.Northing}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage(
                            "\nTransformFromLonLatAlt ERROR:");

                        ed.WriteMessage(
                            $"\n{ex.Message}");
                    }


                    // ====================================================
                    // END
                    // ====================================================

                    ed.WriteMessage(
                        "\n\n========== END GEOLOCATION ==========");


                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(
                    "\nTMREADGEODATA ERROR:");

                ed.WriteMessage(
                    $"\n{ex.GetType().FullName}");

                ed.WriteMessage(
                    $"\n{ex.Message}");
            }
        }
    }
}