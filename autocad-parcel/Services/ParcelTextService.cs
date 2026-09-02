using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using autocad_parcel.Models;

namespace autocad_parcel.Services
{
    public class ParcelTextService
    {
        public void CreateText(
            Database db,
            Point3d location,
            ParcelTextModel parcel)
        {
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
                        OpenMode.ForWrite);

                MText text =
                    new MText
                    {
                        Contents = parcel.GetFormattedText(),
                        Location = location,
                        Height = 1,
                        Attachment = AttachmentPoint.MiddleLeft
                    };

                modelSpace.AppendEntity(text);

                tr.AddNewlyCreatedDBObject(
                    text,
                    true);

                tr.Commit();
            }
        }
    }
}