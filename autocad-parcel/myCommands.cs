using System;
using System.Collections.Generic;

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
            }

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
                    {
                        polylineIds.Add(id);
                    }
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
        // Fixed AutoCAD geolocation configuration.
        //
        // Geographic reference:
        //
        // Latitude  = 7.446114724
        // Longitude = 125.807793900
        //
        // CAD:
        //
        // Easting    = 589000.915
        // Northing  = 823481.4641
        //
        // CRS:
        //
        // PRS92.Philippines-5
        // EPSG:3125
        //
        // IMPORTANT:
        //
        // This reproduces the structure of the manually-created
        // AutoCAD GeoLocationData object.
        //
        // AutoCAD uses:
        //
        // CoordinateTypeLocal
        //
        // DesignPoint:
        //     589000.915
        //     823481.4641
        //
        // ReferencePoint:
        //     589051.5593132314
        //     823505.0602381761
        //
        // Plus the exact 7 mesh points extracted from the
        // manually-geolocated DWG.
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
                // ========================================================
                // 1. Drawing units
                // ========================================================

                AcadApp.SetSystemVariable(
                    "INSUNITS",
                    6);


                // ========================================================
                // 2. Model space
                // ========================================================

                ObjectId modelSpaceId =
                    SymbolUtilityServices.GetBlockModelSpaceId(db);


                // ========================================================
                // 3. Remove existing geographic data
                // ========================================================

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


                        const string geoKey =
                            "ACAD_GEOGRAPHICDATA";


                        if (extDict.Contains(geoKey))
                        {
                            ObjectId oldGeoId =
                                extDict.GetAt(geoKey);


                            GeoLocationData oldGeo =
                                (GeoLocationData)tr.GetObject(
                                    oldGeoId,
                                    OpenMode.ForWrite);


                            oldGeo.Erase();
                        }
                    }


                    tr.Commit();
                }


                // ========================================================
                // 4. Create GeoLocationData
                // ========================================================

                using (Transaction tr =
                       db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord modelSpace =
                        (BlockTableRecord)tr.GetObject(
                            modelSpaceId,
                            OpenMode.ForRead);


                    GeoLocationData geoData =
                        new GeoLocationData();


                    // ----------------------------------------------------
                    // Attach to model space
                    // ----------------------------------------------------

                    geoData.BlockTableRecordId =
                        modelSpaceId;


                    // IMPORTANT:
                    //
                    // Post BEFORE assigning CoordinateSystem.
                    //
                    // Otherwise AutoCAD can throw eNoDatabase.
                    // ----------------------------------------------------

                    geoData.PostToDb();


                    // ====================================================
                    // 5. Coordinate system
                    // ====================================================

                    geoData.CoordinateSystem =
                        MapCoordinateSystem;


                    // ====================================================
                    // 6. Coordinate configuration
                    // ====================================================

                    geoData.TypeOfCoordinates =
                        TypeOfCoordinates.CoordinateTypeLocal;


                    geoData.DesignPoint =
                        new Point3d(
                            MapEasting,
                            MapNorthing,
                            MapElevation);


                    geoData.ReferencePoint =
                        new Point3d(
                            589051.5593132314,
                            823505.0602381761,
                            0.0);


                    geoData.NorthDirectionVector =
                        new Vector2d(
                            0.0,
                            1.0);


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


                    // ====================================================
                    // 7. Recreate AutoCAD's 7 mesh points
                    // ====================================================

                    geoData.ResetMeshPointMaps();


                    // ----------------------------------------------------
                    // Mesh Point 0
                    // ----------------------------------------------------

                    geoData.AddMeshPointMap(
                        0,
                        new Point2d(
                            310736.3078929917,
                            585993.8550816455),
                        new Point2d(
                            123.2937592845364,
                            5.296780081122221));


                    // ----------------------------------------------------
                    // Mesh Point 1
                    // ----------------------------------------------------

                    geoData.AddMeshPointMap(
                        1,
                        new Point2d(
                            689263.6921070083,
                            585993.8550816455),
                        new Point2d(
                            126.70824035989993,
                            5.296676727942055));


                    // ----------------------------------------------------
                    // Mesh Point 2
                    // ----------------------------------------------------

                    geoData.AddMeshPointMap(
                        2,
                        new Point2d(
                            689263.6921070083,
                            1409895.418837635),
                        new Point2d(
                            126.74389792223572,
                            12.74274396118969));


                    // ----------------------------------------------------
                    // Mesh Point 3
                    // ----------------------------------------------------

                    geoData.AddMeshPointMap(
                        3,
                        new Point2d(
                            310736.3078929917,
                            1410719.320401391),
                        new Point2d(
                            123.25852965386093,
                            12.750303802154598));


                    // ----------------------------------------------------
                    // Mesh Point 4
                    // ----------------------------------------------------

                    geoData.AddMeshPointMap(
                        4,
                        new Point2d(
                            499103.34916961286,
                            998356.5877415183),
                        new Point2d(
                            124.9929610408809,
                            9.027911726901952));


                    // ----------------------------------------------------
                    // Mesh Point 5
                    // ----------------------------------------------------

                    geoData.AddMeshPointMap(
                        5,
                        new Point2d(
                            500000.0,
                            748742.6578816351),
                        new Point2d(
                            125.00104541721251,
                            6.770824662868423));


                    // ----------------------------------------------------
                    // Mesh Point 6
                    // ----------------------------------------------------

                    geoData.AddMeshPointMap(
                        6,
                        new Point2d(
                            499646.30643085996,
                            1247808.95059774),
                        new Point2d(
                            124.99795075860223,
                            11.283263010827639));


                    // ====================================================
                    // 8. Let AutoCAD calculate transformation
                    // ====================================================

                    geoData.UpdateTransformationMatrix();


                    tr.Commit();
                }


                // ========================================================
                // 9. Enable Bing Hybrid
                // ========================================================

                ed.Command(
                    "_.GEOMAP",
                    "_HYBRID");


                // ========================================================
                // 10. Zoom to authoritative CAD coordinate
                // ========================================================

                Point3d target =
                    new Point3d(
                        MapEasting,
                        MapNorthing,
                        MapElevation);


                using (ViewTableRecord view =
                       ed.GetCurrentView())
                {
                    view.CenterPoint =
                        new Point2d(
                            target.X,
                            target.Y);


                    view.Width =
                        2000.0;


                    view.Height =
                        2000.0;


                    ed.SetCurrentView(
                        view);
                }


                // ========================================================
                // 11. Output
                // ========================================================

                ed.WriteMessage(
                    "\n========== TMSETMAP ==========");

                ed.WriteMessage(
                    "\nAutoCAD geolocation configured.");

                ed.WriteMessage(
                    $"\nLatitude:  {MapLatitude}");

                ed.WriteMessage(
                    $"\nLongitude: {MapLongitude}");

                ed.WriteMessage(
                    $"\nEasting:   {MapEasting}");

                ed.WriteMessage(
                    $"\nNorthing:  {MapNorthing}");

                ed.WriteMessage(
                    $"\nCRS:       {MapCoordinateSystem}");

                ed.WriteMessage(
                    "\nType:      CoordinateTypeLocal");

                ed.WriteMessage(
                    "\nMesh:      7 points");

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
        // Reads the actual ACAD_GEOGRAPHICDATA object from the DWG.
        // ================================================================

        [CommandMethod("TMREADGEODATA")]
        public void TMREADGEODATA()
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


                ObjectId modelSpaceId =
                    SymbolUtilityServices.GetBlockModelSpaceId(db);


                using (Transaction tr =
                       db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord modelSpace =
                        (BlockTableRecord)tr.GetObject(
                            modelSpaceId,
                            OpenMode.ForRead);


                    // ====================================================
                    // Extension dictionary
                    // ====================================================

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


                    // ====================================================
                    // BASIC DATA
                    // ====================================================

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
                        $"\nScaleEstimationMethod: " +
                        $"{geoData.ScaleEstimationMethod}");


                    ed.WriteMessage(
                        $"\nScaleFactor: " +
                        $"{geoData.ScaleFactor}");


                    ed.WriteMessage(
                        $"\nNumMeshPoints: " +
                        $"{geoData.NumMeshPoints}");


                    // ====================================================
                    // MESH
                    // ====================================================

                    MeshPointMaps mesh =
                        geoData.GetMeshPointMaps();


                    Point2dCollection source =
                        mesh.SourcePonints;


                    Point2dCollection dest =
                        mesh.DestPonints;


                    ed.WriteMessage(
                        "\n\n========== MESH POINTS ==========");


                    ed.WriteMessage(
                        $"\nSource count: {source.Count}");


                    ed.WriteMessage(
                        $"\nDestination count: {dest.Count}");


                    int count =
                        Math.Min(
                            source.Count,
                            dest.Count);


                    for (int i = 0;
                         i < count;
                         i++)
                    {
                        ed.WriteMessage(
                            $"\n\nMesh Point [{i}]");


                        ed.WriteMessage(
                            $"\n  SOURCE = {source[i]}");


                        ed.WriteMessage(
                            $"\n  DEST   = {dest[i]}");
                    }


                    // ====================================================
                    // TRANSFORMATION TEST
                    // ====================================================

                    ed.WriteMessage(
                        "\n\n========== TRANSFORMATION TEST ==========");


                    Point3d testCadPoint =
                        new Point3d(
                            MapEasting,
                            MapNorthing,
                            MapElevation);


                    try
                    {
                        Point3d transformed =
                            geoData.TransformToLonLatAlt(
                                testCadPoint);


                        ed.WriteMessage(
                            "\nCAD POINT:");

                        ed.WriteMessage(
                            $"\n  {testCadPoint}");


                        ed.WriteMessage(
                            "\nAUTOCAD -> LON/LAT:");

                        ed.WriteMessage(
                            $"\n  {transformed}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage(
                            "\nTransformToLonLatAlt ERROR:");

                        ed.WriteMessage(
                            $"\n{ex.Message}");
                    }


                    // ====================================================
                    // REVERSE TEST
                    // ====================================================

                    Point3d geographicPoint =
                        new Point3d(
                            MapLongitude,
                            MapLatitude,
                            MapElevation);


                    try
                    {
                        Point3d transformedBack =
                            geoData.TransformFromLonLatAlt(
                                geographicPoint);


                        ed.WriteMessage(
                            "\n\nLON/LAT POINT:");

                        ed.WriteMessage(
                            $"\n  {geographicPoint}");


                        ed.WriteMessage(
                            "\nAUTOCAD LON/LAT -> CAD:");

                        ed.WriteMessage(
                            $"\n  {transformedBack}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage(
                            "\nTransformFromLonLatAlt ERROR:");

                        ed.WriteMessage(
                            $"\n{ex.Message}");
                    }


                    ed.WriteMessage(
                        "\n\n========== END ==========");


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

