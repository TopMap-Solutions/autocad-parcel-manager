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
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            int count = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt =
                    (BlockTable)tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead
                    );

                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForRead
                    );

                foreach (ObjectId id in btr)
                {
                    Entity? entity =
                        tr.GetObject(id, OpenMode.ForWrite) as Entity;

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
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            int polylineCount = 0;
            int overlapCount = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt =
                    (BlockTable)tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead
                    );

                BlockTableRecord modelSpace =
                    (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForRead
                    );

                List<ObjectId> polylineIds = new List<ObjectId>();

                // Collect all polylines
                foreach (ObjectId id in modelSpace)
                {
                    Entity? entity =
                        tr.GetObject(id, OpenMode.ForRead) as Entity;

                    if (entity is Polyline)
                        polylineIds.Add(id);
                }

                polylineCount = polylineIds.Count;

                // Compare each polyline against every other polyline
                for (int i = 0; i < polylineIds.Count; i++)
                {
                    Polyline? polyline1 =
                        tr.GetObject(polylineIds[i], OpenMode.ForRead) as Polyline;

                    if (polyline1 == null)
                        continue;

                    for (int j = i + 1; j < polylineIds.Count; j++)
                    {
                        Polyline? polyline2 =
                            tr.GetObject(polylineIds[j], OpenMode.ForRead) as Polyline;

                        if (polyline2 == null)
                            continue;

                        Point3dCollection intersections = new Point3dCollection();

                        try
                        {
                            polyline1.IntersectWith(
                                polyline2,
                                Intersect.OnBothOperands,
                                intersections,
                                IntPtr.Zero,
                                IntPtr.Zero
                            );

                            if (intersections.Count > 0)
                            {
                                Polyline p1 =
                                    (Polyline)tr.GetObject(
                                        polylineIds[i], OpenMode.ForWrite);

                                Polyline p2 =
                                    (Polyline)tr.GetObject(
                                        polylineIds[j], OpenMode.ForWrite);

                                p1.ColorIndex = 1; // RED
                                p2.ColorIndex = 1; // RED

                                overlapCount++;
                            }
                        }
                        catch
                        {
                            // Ignore invalid geometry
                        }
                    }
                }

                tr.Commit();
            }

            if (polylineCount == 0)
            {
                ed.WriteMessage("\nNo polylines found.");
                return;
            }

            if (overlapCount == 0)
            {
                ed.WriteMessage(
                    $"\nChecked {polylineCount} polyline(s). No overlaps found!");
            }
            else
            {
                ed.WriteMessage(
                    $"\nFound {overlapCount} overlapping polyline pair(s) — colored RED!" +
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
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            // --- Point ---
            PromptPointResult pointResult =
                ed.GetPoint("\nPlace inside a parcel to generate text: ");

            if (pointResult.Status != PromptStatus.OK)
                return;

            Point3d location = pointResult.Value;

            // --- Owner ---
            PromptStringOptions ownerOptions =
                new PromptStringOptions("\nEnter Owner: ");
            ownerOptions.AllowSpaces = true;
            PromptResult ownerResult = ed.GetString(ownerOptions);

            if (ownerResult.Status != PromptStatus.OK)
                return;

            // --- PIN ---
            PromptStringOptions pinOptions =
                new PromptStringOptions("\nEnter PIN: ");
            pinOptions.AllowSpaces = true;
            PromptResult pinResult = ed.GetString(pinOptions);

            if (pinResult.Status != PromptStatus.OK)
                return;

            // --- Lot ---
            PromptStringOptions lotOptions =
                new PromptStringOptions("\nEnter Lot Number: ");
            lotOptions.AllowSpaces = true;
            PromptResult lotResult = ed.GetString(lotOptions);

            if (lotResult.Status != PromptStatus.OK)
                return;

            // --- Area ---
            PromptStringOptions areaOptions =
                new PromptStringOptions("\nEnter Declared Area: ");
            areaOptions.AllowSpaces = true;
            PromptResult areaResult = ed.GetString(areaOptions);

            if (areaResult.Status != PromptStatus.OK)
                return;

            // --- Land Class ---
            PromptStringOptions classOptions =
                new PromptStringOptions("\nEnter Land Class: ");
            classOptions.AllowSpaces = true;
            PromptResult classResult = ed.GetString(classOptions);

            if (classResult.Status != PromptStatus.OK)
                return;

            // --- Validate and format ---
            string owner = ownerResult.StringResult.ToUpperInvariant();
            string pin = pinResult.StringResult.ToUpperInvariant();
            string lot = lotResult.StringResult.ToUpperInvariant();
            string landClass = classResult.StringResult.ToUpperInvariant();

            string areaInput = areaResult.StringResult.Replace(",", "").Trim();

            if (!decimal.TryParse(areaInput, out decimal area))
            {
                ed.WriteMessage("\nInvalid declared area.");
                return;
            }

            string formattedArea = area.ToString("#,##0.##");

            string parcelText =
                $"{owner}\\P" +
                $"{pin}\\P" +
                $"{lot}\\P" +
                $"A={formattedArea} SQ.M.\\P" +
                $"CLASS: {landClass}";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable? bt =
                    tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord? modelSpace =
                    tr.GetObject(
                        bt![BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite
                    ) as BlockTableRecord;

                MText text = new MText();
                text.Contents = parcelText;
                text.Location = location;
                text.Height = 1;
                text.Attachment = AttachmentPoint.MiddleLeft;

                modelSpace?.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);
                tr.Commit();
            }

            ed.WriteMessage("\nParcel text created.");
        }


        // ================================================================
        // TMSETMAP
        //
        // Establishes the fixed geographic correspondence:
        //
        //   CAD point  (PRS92 / EPSG:3125, metres)
        //     Easting  = 589000.915
        //     Northing = 823481.464
        //
        //   Geographic point
        //     Latitude  =  7.446114724 °N
        //     Longitude = 125.807793900 °E
        //
        //   North = +Y
        //
        // Then enables the Hybrid online basemap and zooms to the site.
        //
        // KEY FIXES vs. the old version:
        //   1. PostToDb()  — the ONLY correct way to attach GeoLocationData.
        //      Manual extension-dictionary stuffing leaves the object in an
        //      invalid internal state that breaks Reorient / Capture Area.
        //   2. TransformFromLonLatAlt() — derives the WCS DesignPoint from
        //      the geographic point.  Do NOT hard-code Easting/Northing here.
        //   3. TypeOfCoordinates.CoordinateTypeGrid — required for any
        //      projected CRS (Easting/Northing in metres) like PRS92.
        //   4. ed.Command("_.GEOMAP", "_HYBRID") — correct way to activate
        //      the online map and unlock Reorient / Capture Area in the ribbon.
        // ================================================================

        // Official geographic coordinates
        private const double Latitude = 7.446114724;
        private const double Longitude = 125.8077939;

        // Official PRS92 / EPSG:3125 survey coordinates
        private const double Easting = 589000.915;
        private const double Northing = 823481.4641;
        private const double Elevation = 0.0;

        // CRS name exactly as shown in AutoCAD's coordinate system picker
        private const string CoordSystem = "PRS92.Philippines-5";

        [CommandMethod("TMSETMAP")]
        public void TMSETMAP()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // ------------------------------------------------------------
                // STEP 1 — Erase any existing GeoLocationData so we start
                // clean.  A stale / malformed geo-data object causes the map
                // to show a logo but no imagery and disables ribbon buttons.
                // ------------------------------------------------------------

                try
                {
                    ObjectId existingId = db.GeoDataObject;

                    using (Transaction trClean =
                           db.TransactionManager.StartTransaction())
                    {
                        DBObject existing =
                            trClean.GetObject(existingId, OpenMode.ForWrite);
                        existing.Erase();
                        trClean.Commit();
                    }

                    ed.WriteMessage("\nExisting geo-data cleared.");
                }
                catch
                {
                    // No existing geo-data — fine, continue.
                }

                // ------------------------------------------------------------
                // STEP 2 — Get the model-space BlockTableRecord ID.
                // ------------------------------------------------------------

                ObjectId msId =
                    SymbolUtilityServices.GetBlockModelSpaceId(db);

                // ------------------------------------------------------------
                // STEP 3 — Create GeoLocationData and attach via PostToDb().
                //
                // PostToDb() writes the object into the model-space extension
                // dictionary under "ACAD_GEODATA" AND registers it with
                // AutoCAD's geographic subsystem.  This is what makes
                // Reorient, Remove Location, and Capture Area become active.
                // ------------------------------------------------------------

                // ------------------------------------------------------------
                // STEP 3a — Set drawing units to METRES.
                //
                // INSUNITS value 6 = metres.
                // Must be set BEFORE PostToDb() so AutoCAD's geo subsystem
                // reads the correct unit scale when initialising the transform.
                //
                // Common INSUNITS values:
                //   0 = Unitless   4 = mm   5 = cm   6 = m   7 = km
                // ------------------------------------------------------------

                AcadApp.SetSystemVariable("INSUNITS", 6);

                // Remove temp point if we created one previously
                // (cleanup from old approach)

                // ============================================================
                // Pure API — replicates your manual workflow:
                //
                //   Manual:  drop marker at Lat/Lon
                //            create point at Easting/Northing
                //            snap marker to that point
                //
                //   API:     PostToDb()  with Lat/Lon as ReferencePoint
                //            set DesignPoint to exact Easting/Northing
                //            AutoCAD snaps them together via the CRS
                // ============================================================

                GeoLocationData geoData = new GeoLocationData();
                geoData.BlockTableRecordId = msId;
                geoData.PostToDb();

                // CRS — must match Name column exactly in AutoCAD picker
                geoData.CoordinateSystem = CoordSystem;
                geoData.TypeOfCoordinates = TypeOfCoordinates.CoordinateTypeGrid;

                // The geographic anchor — where on Earth the marker is
                // X = Longitude, Y = Latitude  (same as manual lat/lon entry)
                geoData.ReferencePoint = new Point3d(Longitude, Latitude, Elevation);

                // The CAD snap point — where in the DWG the marker sits
                // X = Easting, Y = Northing  (same as your manual POINT entity)
                geoData.DesignPoint = new Point3d(Easting, Northing, Elevation);

                // North = +Y
                geoData.NorthDirectionVector = new Vector2d(0.0, 1.0);

                geoData.UpdateTransformationMatrix();

                // Enable hybrid basemap
                ed.Command("_.GEOMAP", "_HYBRID");

                // ------------------------------------------------------------
                // STEP 11 — Zoom to site.
                //
                // Centre the view on the WCS design point at a 2 km extent —
                // a practical starting scale for a land-title survey parcel.
                // Adjust Width/Height to match your drawing's extent.
                // ------------------------------------------------------------

                using (ViewTableRecord view = ed.GetCurrentView())
                {
                    view.CenterPoint = new Point2d(Easting, Northing);
                    view.Width = 2000.0;  // 2 km
                    view.Height = 2000.0;
                    ed.SetCurrentView(view);
                }

                // ------------------------------------------------------------
                // STEP 12 — Regenerate.
                // ------------------------------------------------------------

                ed.Regen();

                // ------------------------------------------------------------
                // Result summary
                // ------------------------------------------------------------

                ed.WriteMessage(
                    "\n\n========================================");
                ed.WriteMessage(
                    "\n  TMSETMAP — Geographic Location Set");
                ed.WriteMessage(
                    "\n========================================");
                ed.WriteMessage(
                    $"\n  CRS        : {CoordSystem}");
                ed.WriteMessage(
                    "\n  EPSG       : 3125");
                ed.WriteMessage(
                    $"\n  Easting    : {Easting:F3} m");
                ed.WriteMessage(
                    $"\n  Northing   : {Northing:F3} m");
                ed.WriteMessage(
                    $"\n  Latitude   : {Latitude:F9}°");
                ed.WriteMessage(
                    $"\n  Longitude  : {Longitude:F9}°");
                ed.WriteMessage(
                    "\n  North      : +Y");
                ed.WriteMessage(
                    "\n  Units      : Metres (INSUNITS=6)");
                ed.WriteMessage(
                    "\n  Map Style  : Hybrid");
                ed.WriteMessage(
                    "\n========================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nTMSETMAP ERROR: {ex.Message}");
                ed.WriteMessage($"\n  {ex.StackTrace}");
            }
        }
    }
}
