using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


[assembly: CommandClass(typeof(autocad_parcel.MyCommands))]
[assembly: ExtensionApplication(typeof(autocad_parcel.PluginExtension))]


namespace autocad_parcel
{
    public class MyCommands
    {
        // ================================================================
        // TMCHECKLINES
        // Finds all Line objects in model space and colors them RED.
        // ================================================================

        [CommandMethod("TMCHECKLINES")]
        public void CheckLines()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            int count = 0;

            using (Transaction tr =
                   db.TransactionManager.StartTransaction())
            {
                BlockTable bt =
                    (BlockTable)tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead);

                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForRead);

                foreach (ObjectId id in btr)
                {
                    Entity? entity =
                        tr.GetObject(
                            id,
                            OpenMode.ForWrite) as Entity;

                    if (entity is Line line)
                    {
                        line.ColorIndex = 1;
                        count++;
                    }
                }

                tr.Commit();

                if (count == 0)
                {
                    ed.WriteMessage(
                        "\nNo lines found. Ready for sync in the database!");
                }
                else
                {
                    ed.WriteMessage(
                        $"\nFound {count} line(s) and colored RED! " +
                        "Please inspect the lines — either make them a polygon.");
                }
            }
        }


        // ================================================================
        // TMCHECKOVERLAPS
        // Checks for intersecting polylines and colors overlapping pairs RED.
        // ================================================================

        [CommandMethod("TMCHECKOVERLAPS")]
        public void CheckPolylineOverlaps()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            int polylineCount = 0;
            int overlapCount = 0;

            using (Transaction tr =
                   db.TransactionManager.StartTransaction())
            {
                BlockTable bt =
                    (BlockTable)tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead);

                BlockTableRecord modelSpace =
                    (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForRead);

                List<ObjectId> polylineIds =
                    new List<ObjectId>();


                // --------------------------------------------------------
                // Collect all polylines
                // --------------------------------------------------------

                foreach (ObjectId id in modelSpace)
                {
                    Entity? entity =
                        tr.GetObject(
                            id,
                            OpenMode.ForRead) as Entity;

                    if (entity is Polyline)
                        polylineIds.Add(id);
                }

                polylineCount =
                    polylineIds.Count;


                // --------------------------------------------------------
                // Compare each polyline against every other polyline
                // --------------------------------------------------------

                for (int i = 0;
                     i < polylineIds.Count;
                     i++)
                {
                    Polyline? polyline1 =
                        tr.GetObject(
                            polylineIds[i],
                            OpenMode.ForRead) as Polyline;

                    if (polyline1 == null)
                        continue;


                    for (int j = i + 1;
                         j < polylineIds.Count;
                         j++)
                    {
                        Polyline? polyline2 =
                            tr.GetObject(
                                polylineIds[j],
                                OpenMode.ForRead) as Polyline;

                        if (polyline2 == null)
                            continue;


                        Point3dCollection intersections =
                            new Point3dCollection();

                        try
                        {
                            polyline1.IntersectWith(
                                polyline2,
                                Intersect.OnBothOperands,
                                intersections,
                                IntPtr.Zero,
                                IntPtr.Zero);


                            if (intersections.Count > 0)
                            {
                                Polyline p1 =
                                    (Polyline)tr.GetObject(
                                        polylineIds[i],
                                        OpenMode.ForWrite);

                                Polyline p2 =
                                    (Polyline)tr.GetObject(
                                        polylineIds[j],
                                        OpenMode.ForWrite);


                                p1.ColorIndex = 1;
                                p2.ColorIndex = 1;

                                overlapCount++;
                            }
                        }
                        catch
                        {
                            // Ignore invalid geometry.
                        }
                    }
                }

                tr.Commit();
            }


            if (polylineCount == 0)
            {
                ed.WriteMessage(
                    "\nNo polylines found.");

                return;
            }


            if (overlapCount == 0)
            {
                ed.WriteMessage(
                    $"\nChecked {polylineCount} polyline(s). " +
                    "No overlaps found!");
            }
            else
            {
                ed.WriteMessage(
                    $"\nFound {overlapCount} overlapping polyline pair(s) — " +
                    "colored RED!" +
                    "\nPlease inspect them to avoid sync errors.");
            }
        }


        // ================================================================
        // TMPARCELTEXT
        // Prompts for parcel data and inserts a formatted MText label.
        // ================================================================

        [CommandMethod("TMPARCELTEXT")]
        public void InsertParcelText()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;


            // ------------------------------------------------------------
            // Point
            // ------------------------------------------------------------

            PromptPointResult pointResult =
                ed.GetPoint(
                    "\nPlace inside a parcel to generate text: ");

            if (pointResult.Status != PromptStatus.OK)
                return;

            Point3d location =
                pointResult.Value;


            // ------------------------------------------------------------
            // Owner
            // ------------------------------------------------------------

            PromptStringOptions ownerOptions =
                new PromptStringOptions(
                    "\nEnter Owner: ");

            ownerOptions.AllowSpaces = true;

            PromptResult ownerResult =
                ed.GetString(ownerOptions);

            if (ownerResult.Status != PromptStatus.OK)
                return;


            // ------------------------------------------------------------
            // PIN
            // ------------------------------------------------------------

            PromptStringOptions pinOptions =
                new PromptStringOptions(
                    "\nEnter PIN: ");

            pinOptions.AllowSpaces = true;

            PromptResult pinResult =
                ed.GetString(pinOptions);

            if (pinResult.Status != PromptStatus.OK)
                return;


            // ------------------------------------------------------------
            // Lot
            // ------------------------------------------------------------

            PromptStringOptions lotOptions =
                new PromptStringOptions(
                    "\nEnter Lot Number: ");

            lotOptions.AllowSpaces = true;

            PromptResult lotResult =
                ed.GetString(lotOptions);

            if (lotResult.Status != PromptStatus.OK)
                return;


            // ------------------------------------------------------------
            // Area
            // ------------------------------------------------------------

            PromptStringOptions areaOptions =
                new PromptStringOptions(
                    "\nEnter Declared Area: ");

            areaOptions.AllowSpaces = true;

            PromptResult areaResult =
                ed.GetString(areaOptions);

            if (areaResult.Status != PromptStatus.OK)
                return;


            // ------------------------------------------------------------
            // Land Class
            // ------------------------------------------------------------

            PromptStringOptions classOptions =
                new PromptStringOptions(
                    "\nEnter Land Class: ");

            classOptions.AllowSpaces = true;

            PromptResult classResult =
                ed.GetString(classOptions);

            if (classResult.Status != PromptStatus.OK)
                return;


            // ------------------------------------------------------------
            // Validate and format
            // ------------------------------------------------------------

            string owner =
                ownerResult.StringResult.ToUpperInvariant();

            string pin =
                pinResult.StringResult.ToUpperInvariant();

            string lot =
                lotResult.StringResult.ToUpperInvariant();

            string landClass =
                classResult.StringResult.ToUpperInvariant();


            string areaInput =
                areaResult.StringResult
                    .Replace(",", "")
                    .Trim();


            if (!decimal.TryParse(
                    areaInput,
                    out decimal area))
            {
                ed.WriteMessage(
                    "\nInvalid declared area.");

                return;
            }


            string formattedArea =
                area.ToString("#,##0.##");


            string parcelText =
                $"{owner}\\P" +
                $"{pin}\\P" +
                $"{lot}\\P" +
                $"A={formattedArea} SQ.M.\\P" +
                $"CLASS: {landClass}";


            // ------------------------------------------------------------
            // Create MText
            // ------------------------------------------------------------

            using (Transaction tr =
                   db.TransactionManager.StartTransaction())
            {
                BlockTable? bt =
                    tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead) as BlockTable;


                BlockTableRecord? modelSpace =
                    tr.GetObject(
                        bt![BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;


                MText text =
                    new MText();

                text.Contents =
                    parcelText;

                text.Location =
                    location;

                text.Height =
                    1;

                text.Attachment =
                    AttachmentPoint.MiddleLeft;


                modelSpace?.AppendEntity(text);

                tr.AddNewlyCreatedDBObject(
                    text,
                    true);

                tr.Commit();
            }


            ed.WriteMessage(
                "\nParcel text created.");
        }



        // ================================================================
        // TMSETMAP
        //
        // Fixed geographic anchor:
        //
        // Geographic:
        //     Latitude  = 7.446114724
        //     Longitude = 125.807793900
        //
        // CAD / PRS92 Zone 5:
        //     Easting    = 589000.915
        //     Northing  = 823481.4641
        //
        // CRS:
        //     PRS92.Philippines-5
        //
        // IMPORTANT:
        //
        // The CAD coordinates are authoritative.
        //
        // We DO NOT:
        //
        //     TransformFromLonLatAlt()
        //     UpdateTransformationMatrix()
        //
        // We directly associate the known geographic point with the
        // known CAD point.
        //
        // No GeoLocation UI is displayed.
        // ================================================================


        private const double MapLatitude =
            7.446114724;

        private const double MapLongitude =
            125.807793900;

        private const double MapEasting =
            589000.915;

        private const double MapNorthing =
            823481.4641;

        private const double MapElevation =
            0.0;

        private const string MapCoordinateSystem =
            "PRS92.Philippines-5";


        [CommandMethod("TMSETMAP")]
        public void TMSETMAP()
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
                // ============================================================
                // 1. SET DRAWING UNITS TO METRES
                // ============================================================

                AcadApp.SetSystemVariable(
                    "INSUNITS",
                    6);


                // ============================================================
                // 2. REMOVE EXISTING GEODATA
                // ============================================================

                try
                {
                    ObjectId oldGeoId =
                        db.GeoDataObject;

                    if (!oldGeoId.IsNull &&
                        !oldGeoId.IsErased)
                    {
                        using (Transaction tr =
                               db.TransactionManager
                                 .StartTransaction())
                        {
                            DBObject oldGeo =
                                tr.GetObject(
                                    oldGeoId,
                                    OpenMode.ForWrite);

                            oldGeo.Erase();

                            tr.Commit();
                        }
                    }
                }
                catch
                {
                    // No existing geographic data.
                }


                // ============================================================
                // 3. GET MODEL SPACE
                // ============================================================

                ObjectId modelSpaceId =
                    SymbolUtilityServices
                        .GetBlockModelSpaceId(db);


                // ============================================================
                // 4. CREATE GEODATA
                // ============================================================

                GeoLocationData geoData =
                    new GeoLocationData();


                // ============================================================
                // 5. ATTACH GEODATA TO THE DATABASE FIRST
                //
                // THIS IS IMPORTANT.
                //
                // CoordinateSystem cannot be assigned safely while the
                // GeoLocationData object has no database.
                // ============================================================

                geoData.BlockTableRecordId =
                    modelSpaceId;

                geoData.PostToDb();


                // ============================================================
                // 6. NOW SET THE CRS
                // ============================================================

                geoData.CoordinateSystem =
                    MapCoordinateSystem;


                // ============================================================
                // 7. PROJECTED / GRID COORDINATES
                // ============================================================

                geoData.TypeOfCoordinates =
                    TypeOfCoordinates.CoordinateTypeGrid;


                // ============================================================
                // 8. GEOGRAPHIC POINT
                //
                // AutoCAD geographic coordinates:
                //
                // X = Longitude
                // Y = Latitude
                //
                // This represents the point that would be entered into the
                // manual GEOGRAPHICLOCATION workflow.
                // ============================================================

                Point2d geographicPoint =
                    new Point2d(
                        MapLongitude,
                        MapLatitude);


                geoData.ReferencePoint =
                    new Point3d(
                        MapLongitude,
                        MapLatitude,
                        MapElevation);


                // ============================================================
                // 9. EXACT CAD POINT
                //
                // THIS IS OUR SURVEY CONTROL POINT.
                //
                // We do not calculate this from the latitude/longitude.
                //
                // This is the exact point in the DWG where the geographic
                // location is supposed to be anchored.
                // ============================================================

                Point2d cadPoint =
                    new Point2d(
                        MapEasting,
                        MapNorthing);


                geoData.DesignPoint =
                    new Point3d(
                        MapEasting,
                        MapNorthing,
                        MapElevation);


                // ============================================================
                // 10. NORTH DIRECTION
                //
                // Drawing orientation:
                //
                // +X = East
                // +Y = North
                // ============================================================

                geoData.NorthDirectionVector =
                    new Vector2d(
                        0.0,
                        1.0);


                // ============================================================
                // 11. EXPLICIT CONTROL POINT
                //
                // Geographic:
                //
                //     125.807793900
                //     7.446114724
                //
                // is associated with:
                //
                //     589000.915
                //     823481.4641
                //
                // No transformation function is called by our plugin.
                // ============================================================

                geoData.AddMeshPointMap(
                    0,
                    geographicPoint,
                    cadPoint);


                // ============================================================
                // 12. DO NOT CALL THESE
                //
                // geoData.UpdateTransformationMatrix();
                //
                // geoData.TransformFromLonLatAlt(...);
                //
                // We intentionally leave them out.
                // ============================================================


                // ============================================================
                // 13. ENABLE BING HYBRID
                // ============================================================

                ed.Command(
                    "_.GEOMAP",
                    "_HYBRID");


                // ============================================================
                // 14. ZOOM TO EXACT SURVEY POINT
                // ============================================================

                using (ViewTableRecord view =
                       ed.GetCurrentView())
                {
                    view.CenterPoint =
                        new Point2d(
                            MapEasting,
                            MapNorthing);

                    view.Width =
                        2000.0;

                    view.Height =
                        2000.0;

                    ed.SetCurrentView(
                        view);
                }


                // ============================================================
                // 15. REGENERATE
                // ============================================================

                ed.Regen();


                // ============================================================
                // 16. RESULT
                // ============================================================

                ed.WriteMessage(
                    "\n\n========================================");

                ed.WriteMessage(
                    "\n TMSETMAP COMPLETE");

                ed.WriteMessage(
                    "\n========================================");

                ed.WriteMessage(
                    $"\n CRS        : {MapCoordinateSystem}");

                ed.WriteMessage(
                    "\n EPSG       : 3125");

                ed.WriteMessage(
                    $"\n Easting    : {MapEasting:F4} m");

                ed.WriteMessage(
                    $"\n Northing   : {MapNorthing:F4} m");

                ed.WriteMessage(
                    $"\n Latitude   : {MapLatitude:F9}");

                ed.WriteMessage(
                    $"\n Longitude  : {MapLongitude:F9}");

                ed.WriteMessage(
                    "\n North      : +Y");

                ed.WriteMessage(
                    "\n Units      : Metres");

                ed.WriteMessage(
                    "\n Anchor     : Exact CAD point");

                ed.WriteMessage(
                    "\n Transform  : None requested");

                ed.WriteMessage(
                    "\n Map        : Bing Hybrid");

                ed.WriteMessage(
                    "\n========================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(
                    "\n\nTMSETMAP ERROR:");

                ed.WriteMessage(
                    $"\n{ex.Message}");

                ed.WriteMessage(
                    $"\n{ex.StackTrace}");
            }
        }






[CommandMethod("TMREADGEODATA")]
public void TMREADGEODATA()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                ed.WriteMessage(
                    "\n========== TMREADGEODATA ==========");

                ObjectId modelSpaceId =
                    SymbolUtilityServices.GetBlockModelSpaceId(db);

                using (Transaction tr =
                       db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord modelSpace =
                        (BlockTableRecord)tr.GetObject(
                            modelSpaceId,
                            OpenMode.ForRead);

                    if (modelSpace.ExtensionDictionary.IsNull)
                    {
                        ed.WriteMessage(
                            "\nNo ExtensionDictionary.");

                        return;
                    }

                    DBDictionary extDict =
                        (DBDictionary)tr.GetObject(
                            modelSpace.ExtensionDictionary,
                            OpenMode.ForRead);

                    const string key =
                        "ACAD_GEOGRAPHICDATA";

                    if (!extDict.Contains(key))
                    {
                        ed.WriteMessage(
                            "\nACAD_GEOGRAPHICDATA not found.");

                        return;
                    }

                    ObjectId geoId =
                        extDict.GetAt(key);

                    GeoLocationData geoData =
                        (GeoLocationData)tr.GetObject(
                            geoId,
                            OpenMode.ForRead);


                    // ============================================================
                    // BASIC DATA
                    // ============================================================

                    ed.WriteMessage(
                        $"\n\nGeoLocationData ID: {geoId}");

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
                        $"\nUpDirection: " +
                        $"{geoData.UpDirection}");


                    // ============================================================
                    // MESH OBJECT
                    // ============================================================

                    ed.WriteMessage(
                        "\n\n========== MESH ==========");

                    MeshPointMaps mesh =
                        geoData.GetMeshPointMaps();

                    Type meshType =
                        mesh.GetType();

                    ed.WriteMessage(
                        $"\nMesh type: {meshType.FullName}");


                    // ============================================================
                    // MESH METHODS
                    // ============================================================

                    ed.WriteMessage(
                        "\n\n--- Mesh Methods ---");

                    MethodInfo[] methods =
                        meshType.GetMethods(
                            BindingFlags.Public |
                            BindingFlags.Instance);

                    foreach (MethodInfo method in methods)
                    {
                        ParameterInfo[] parameters =
                            method.GetParameters();

                        string parameterText = "";

                        for (int i = 0;
                             i < parameters.Length;
                             i++)
                        {
                            if (i > 0)
                                parameterText += ", ";

                            parameterText +=
                                parameters[i].ParameterType.Name +
                                " " +
                                parameters[i].Name;
                        }

                        ed.WriteMessage(
                            $"\n{method.Name}" +
                            $"({parameterText})" +
                            $" -> {method.ReturnType.Name}");
                    }


                    // ============================================================
                    // MESH PROPERTIES
                    // ============================================================

                    ed.WriteMessage(
                        "\n\n--- Mesh Properties ---");

                    PropertyInfo[] properties =
                        meshType.GetProperties(
                            BindingFlags.Public |
                            BindingFlags.Instance);

                    foreach (PropertyInfo property in properties)
                    {
                        ed.WriteMessage(
                            $"\n{property.Name}" +
                            $" : {property.PropertyType.Name}");
                    }


                    // ============================================================
                    // GEOLOCATIONDATA METHODS
                    // ============================================================

                    ed.WriteMessage(
                        "\n\n--- GeoLocationData Methods ---");

                    MethodInfo[] geoMethods =
                        typeof(GeoLocationData).GetMethods(
                            BindingFlags.Public |
                            BindingFlags.Instance);

                    foreach (MethodInfo method in geoMethods)
                    {
                        ParameterInfo[] parameters =
                            method.GetParameters();

                        string parameterText = "";

                        for (int i = 0;
                             i < parameters.Length;
                             i++)
                        {
                            if (i > 0)
                                parameterText += ", ";

                            parameterText +=
                                parameters[i].ParameterType.Name +
                                " " +
                                parameters[i].Name;
                        }

                        ed.WriteMessage(
                            $"\n{method.Name}" +
                            $"({parameterText})" +
                            $" -> {method.ReturnType.Name}");
                    }


                    ed.WriteMessage(
                        "\n\n========== END ==========");

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(
                    "\n\nTMREADGEODATA ERROR:");

                ed.WriteMessage(
                    $"\nType: {ex.GetType().FullName}");

                ed.WriteMessage(
                    $"\nMessage: {ex.Message}");

                ed.WriteMessage(
                    $"\nStack: {ex.StackTrace}");
            }
        }






    }
}

