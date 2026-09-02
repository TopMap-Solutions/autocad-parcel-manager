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
    public class ParcelCommands
    {
        private readonly LineCheckService _lineService =
            new LineCheckService();

        private readonly OverlapCheckService _overlapService =
            new OverlapCheckService();

        private readonly ParcelTextService _parcelTextService =
            new ParcelTextService();


        [CommandMethod("TMCHECKLINES")]
        public void CheckLines()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;

            int count =
                _lineService.CheckLines(
                    doc.Database);

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


        [CommandMethod("TMCHECKOVERLAPS")]
        public void CheckPolylineOverlaps()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;

            OverlapCheckResult result =
                _overlapService.Check(
                    doc.Database);

            if (result.PolylineCount == 0)
            {
                ed.WriteMessage(
                    "\nNo polylines found.");

                return;
            }

            if (result.OverlapCount == 0)
            {
                ed.WriteMessage(
                    $"\nChecked {result.PolylineCount} polyline(s). " +
                    "No overlaps found!");
            }
            else
            {
                ed.WriteMessage(
                    $"\nFound {result.OverlapCount} overlapping " +
                    "polyline pair(s) — colored RED!" +
                    "\nPlease inspect them to avoid sync errors.");
            }
        }


        [CommandMethod("TMPARCELTEXT")]
        public void InsertParcelText()
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;

            // --------------------------------------------------------
            // Point
            // --------------------------------------------------------

            PromptPointResult pointResult =
                ed.GetPoint(
                    "\nPlace inside a parcel to generate text: ");

            if (pointResult.Status != PromptStatus.OK)
                return;

            // --------------------------------------------------------
            // Owner
            // --------------------------------------------------------

            string? owner =
                GetString(
                    ed,
                    "\nEnter Owner: ");

            if (owner == null)
                return;

            // --------------------------------------------------------
            // PIN
            // --------------------------------------------------------

            string? pin =
                GetString(
                    ed,
                    "\nEnter PIN: ");

            if (pin == null)
                return;

            // --------------------------------------------------------
            // Lot
            // --------------------------------------------------------

            string? lot =
                GetString(
                    ed,
                    "\nEnter Lot Number: ");

            if (lot == null)
                return;

            // --------------------------------------------------------
            // Area
            // --------------------------------------------------------

            string? areaInput =
                GetString(
                    ed,
                    "\nEnter Declared Area: ");

            if (areaInput == null)
                return;

            areaInput =
                areaInput
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

            // --------------------------------------------------------
            // Land class
            // --------------------------------------------------------

            string? landClass =
                GetString(
                    ed,
                    "\nEnter Land Class: ");

            if (landClass == null)
                return;

            ParcelTextModel parcel =
                new ParcelTextModel
                {
                    Owner = owner.ToUpperInvariant(),
                    Pin = pin.ToUpperInvariant(),
                    Lot = lot.ToUpperInvariant(),
                    DeclaredArea = area,
                    LandClass = landClass.ToUpperInvariant()
                };

            _parcelTextService.CreateText(
                doc.Database,
                pointResult.Value,
                parcel);

            ed.WriteMessage(
                "\nParcel text created.");
        }


        private string? GetString(
            Editor ed,
            string message)
        {
            PromptStringOptions options =
                new PromptStringOptions(message)
                {
                    AllowSpaces = true
                };

            PromptResult result =
                ed.GetString(options);

            if (result.Status != PromptStatus.OK)
                return null;

            return result.StringResult;
        }
    }
}