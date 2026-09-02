using System.Runtime.Versioning;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadApp =
    Autodesk.AutoCAD.ApplicationServices.Application;

using ParcelManager.Views;


namespace ParcelManager.Commands
{
    [SupportedOSPlatform("windows")]
    public class BuddyCommands
    {
        [CommandMethod("TMBUDDY")]
        public void ShowBuddy()
        {
            MainPalette.Show();

            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            Editor ed = doc.Editor;

            ed.WriteMessage(
                "\nWelcome to TopMap Solutions(TM) Surveyor Buddy 2026");
        }
    }
}