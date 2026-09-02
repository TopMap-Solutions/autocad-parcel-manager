using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ParcelManger.Models;

namespace ParcelManger.Services
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
                        Attachment = AttachmentPoint.MiddleLeft,
                        Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0),
                        TextHeight = 1.5,
                        Width = 0,
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