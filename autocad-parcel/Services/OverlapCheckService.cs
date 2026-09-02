using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ParcelManger.Services
{
    public class OverlapCheckService
    {
        public OverlapCheckResult Check(Database db)
        {
            List<ObjectId> polylineIds =
                new List<ObjectId>();

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

                // ----------------------------------------------------
                // Collect polylines
                // ----------------------------------------------------

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

                // ----------------------------------------------------
                // Compare polylines
                // ----------------------------------------------------

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

            return new OverlapCheckResult
            {
                PolylineCount = polylineIds.Count,
                OverlapCount = overlapCount
            };
        }
    }


    public class OverlapCheckResult
    {
        public int PolylineCount { get; set; }

        public int OverlapCount { get; set; }
    }
}