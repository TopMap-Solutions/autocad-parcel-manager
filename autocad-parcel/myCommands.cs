

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Internal.Windows;
using System.Numerics;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(autocad_parcel.MyCommands))]
[assembly: ExtensionApplication(typeof(autocad_parcel.PluginExtension))]

namespace autocad_parcel
{
    public class MyCommands
    {
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
                        OpenMode.ForRead);

                foreach (ObjectId id in btr)
                {
                    Entity? entity = 
                        tr.GetObject(
                            id, 
                            OpenMode.ForWrite
                        ) as Entity;

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
                        $"\nFound {count} lines(s) and colored RED! Please inspect the lines either make them polygon.");
                }

            }
        }

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

                // Collect polylines
                foreach (ObjectId id in modelSpace)
                {
                    Entity? entity =
                        tr.GetObject(
                            id,
                            OpenMode.ForRead
                        ) as Entity;

                    if (entity is Polyline)
                    {
                        polylineIds.Add(id);
                    }
                }

                polylineCount = polylineIds.Count;

                // Compare each polyline against every other polyline
                for (int i = 0; i < polylineIds.Count; i++)
                {
                    Polyline? polyline1 =
                        tr.GetObject(
                            polylineIds[i],
                            OpenMode.ForRead
                        ) as Polyline;

                    if (polyline1 == null)
                        continue;

                    for (int j = i + 1; j < polylineIds.Count; j++)
                    {
                        Polyline? polyline2 =
                            tr.GetObject(
                                polylineIds[j],
                                OpenMode.ForRead
                            ) as Polyline;

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
                                IntPtr.Zero
                            );


                            if (intersections.Count > 0)
                            {
                                Polyline p1 =
                                    (Polyline)tr.GetObject(
                                        polylineIds[i],
                                        OpenMode.ForWrite
                                    );

                                Polyline p2 =
                                    (Polyline)tr.GetObject(
                                        polylineIds[j],
                                        OpenMode.ForWrite
                                    );

                                // RED
                                p1.ColorIndex = 1;
                                p2.ColorIndex = 1;

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
                ed.WriteMessage(
                    "\nNo polylines found."
                );

                return;
            }

            if (overlapCount == 0)
            {
                ed.WriteMessage(
                    $"\nChecked {polylineCount} polylines. No overlaps found!"
                );
            }
            else
            {
                ed.WriteMessage(
                    $"\nFound {overlapCount} overlapping polyline/s colored RED!" +
                    "\nPlease inspect the overlapping polyline/s to avoid sync errors."
                );

                
            }
        }

        [CommandMethod("TMPARCELTEXT")]
        public void InsertParcelText()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Point
            PromptPointResult pointResult =
                ed.GetPoint("\nPlace inside a parcel to generate text: ");

            if (pointResult.Status != PromptStatus.OK)
                return;
           

            Point3d location = pointResult.Value;

            // Owner
            PromptStringOptions ownerOptions =
                new PromptStringOptions("\nEnter Owner: ");

            ownerOptions.AllowSpaces = true;

            PromptResult ownerResult = ed.GetString(ownerOptions);

            if (ownerResult.Status != PromptStatus.OK)
                return;

            // Pin
            PromptStringOptions pinOptions =
                new PromptStringOptions("\nEnter PIN: ");

            pinOptions.AllowSpaces = true;

            PromptResult pinResult = ed.GetString(pinOptions);

            if (pinResult.Status != PromptStatus.OK)
                return;

            // Lot
            PromptStringOptions lotOptions =
                new PromptStringOptions("\nEnter Lot Number: ");

            lotOptions.AllowSpaces = true;

            PromptResult lotResult = ed.GetString(lotOptions);

            if (lotResult.Status != PromptStatus.OK)
                return;


            // Area
            PromptStringOptions areaOptions =
                new PromptStringOptions("\nEnter Declared Area: ");

            areaOptions.AllowSpaces = true;

            PromptResult areaResult = ed.GetString(areaOptions);

            if (areaResult.Status != PromptStatus.OK)
                return;

            // Class
            PromptStringOptions classOptions =
                new PromptStringOptions("\nEnter Land Class: ");

            classOptions.AllowSpaces = true;

            PromptResult classResult = ed.GetString(classOptions);

            if (classResult.Status != PromptStatus.OK)
                return;

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
                    tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead
                    ) as BlockTable;

                BlockTableRecord? modelSpace =
                    tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
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
            ed.WriteMessage("\nParcel Text Created");



        }
    }
}
