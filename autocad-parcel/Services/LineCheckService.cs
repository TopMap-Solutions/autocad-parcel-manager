using Autodesk.AutoCAD.DatabaseServices;

namespace autocad_parcel.Services
{
    public class LineCheckService
    {
        public int CheckLines(Database db)
        {
            int count = 0;

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

                foreach (ObjectId id in modelSpace)
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

            return count;
        }
    }
}