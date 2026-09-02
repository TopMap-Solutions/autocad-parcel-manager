using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using AcadApp =
    Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(ParcelManger.Commands.ParcelCommands))]

namespace ParcelManager.Views
{
    [SupportedOSPlatform("windows")]
    public class MainPalette
    {
        private static PaletteSet? palette;

        // --------------------------------
        // COLORS
        // --------------------------------

        private static readonly Color Background =
            Color.FromArgb(33, 40, 48);

        private static readonly Color PanelColor =
            Color.FromArgb(46, 52, 64);

        private static readonly Color ButtonColor =
            Color.FromArgb(59, 68, 83);

        private static readonly Color ButtonHover =
            Color.FromArgb(74, 86, 104);

        private static readonly Color ButtonBorder =
            Color.FromArgb(22, 27, 34);

        private static readonly Color Accent =
            Color.FromArgb(0, 133, 189);

        private static readonly Color Text =
            Color.FromArgb(214, 214, 214);

        private static readonly Color SecondaryText =
            Color.FromArgb(140, 140, 140);


        public static void Show()
        {
            if (palette == null)
            {
                palette = new PaletteSet("TM Land Parcel Buddy")
                {
                    Size = new Size(340, 500)
                };

                // --------------------------------
                // MAIN PANEL
                // --------------------------------

                Panel mainPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    BackColor = Background
                };

                // --------------------------------
                // FILL PANEL
                // --------------------------------

                Panel fillPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };

                // --------------------------------
                // HEADER
                // --------------------------------

                Label title = new Label
                {
                    Text = "TM LAND PARCEL BUDDY",
                    Dock = DockStyle.Top,
                    Height = 38,
                    ForeColor = Text,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(12, 0, 0, 0),

                    Font = new Font(
                        "Segoe UI",
                        12,
                        FontStyle.Bold
                    ),

                    TextAlign =
                        ContentAlignment.MiddleLeft
                };

                Label subtitle = new Label
                {
                    Text = "Powered by TopMap Solutions 2026",
                    Dock = DockStyle.Top,
                    Height = 24,
                    ForeColor = SecondaryText,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(12, 0, 0, 0),

                    Font = new Font(
                        "Segoe UI",
                        9
                    ),

                    TextAlign =
                        ContentAlignment.MiddleLeft
                };

                // --------------------------------
                // DIVIDER
                // --------------------------------

                Panel headerDivider = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 1,
                    BackColor = ButtonBorder,
                    Margin = new Padding(0)
                };

                // --------------------------------
                // GENERAL TOOLS
                // --------------------------------

                Panel generalPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 112,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(10)
                };

                Button setMapButton =
                    CreateButton("Set Map");

                Button insertParcelTextButton =
                    CreateButton("Insert Parcel Text");

                generalPanel.Controls.Add(
                    insertParcelTextButton);

                generalPanel.Controls.Add(
                    setMapButton);

                // --------------------------------
                // INSPECTION
                // --------------------------------

                Panel inspectionPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 148,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(10)
                };

                Label inspectionLabel = new Label
                {
                    Text = "INSPECTION",
                    Dock = DockStyle.Top,
                    Height = 30,
                    ForeColor = Accent,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(2, 0, 0, 4),

                    Font = new Font(
                        "Segoe UI",
                        9,
                        FontStyle.Bold
                    ),

                    TextAlign =
                        ContentAlignment.BottomLeft
                };

                Button inspectOverlapButton =
                    CreateButton("Inspect Overlap");

                Button inspectLinesButton =
                    CreateButton("Inspect Lines");

                inspectionPanel.Controls.Add(
                    inspectLinesButton);

                inspectionPanel.Controls.Add(
                    inspectOverlapButton);

                inspectionPanel.Controls.Add(
                    inspectionLabel);

                // --------------------------------
                // EVENTS
                // --------------------------------

                setMapButton.Click += (sender, e) =>
                {
                    RunCommand("TMSETMAP");
                };

                insertParcelTextButton.Click += (sender, e) =>
                {
                    RunCommand("TMPARCELTEXT");
                };

                inspectOverlapButton.Click += (sender, e) =>
                {
                    RunCommand("TMCHECKOVERLAPS");
                };

                inspectLinesButton.Click += (sender, e) =>
                {
                    RunCommand("TMCHECKLINES");
                };

                // --------------------------------
                // MAIN LAYOUT
                // --------------------------------

                mainPanel.Controls.Add(fillPanel);

                mainPanel.Controls.Add(
                    inspectionPanel);

                mainPanel.Controls.Add(
                    generalPanel);

                mainPanel.Controls.Add(
                    headerDivider);

                mainPanel.Controls.Add(
                    subtitle);

                mainPanel.Controls.Add(
                    title);

                palette.Add(
                    "Tools",
                    mainPanel);
            }

            palette.Visible = true;
        }

        // --------------------------------
        // BUTTON
        // --------------------------------

        private static Button CreateButton(string text)
        {
            Button button = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = ButtonColor,
                ForeColor = Text,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(10, 0, 0, 0),

                Font = new Font(
                    "Segoe UI",
                    10
                ),

                TextAlign =
                    ContentAlignment.MiddleLeft,

                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 1;

            button.FlatAppearance.BorderColor =
                ButtonBorder;

            button.FlatAppearance.MouseOverBackColor =
                ButtonHover;

            button.FlatAppearance.MouseDownBackColor =
                PanelColor;

            return button;
        }

        // --------------------------------
        // RUN COMMAND
        // --------------------------------
        private static void RunCommand(string command)
        {
            Document? doc =
                AcadApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            doc.SendStringToExecute(
                command + " ",
                true,
                false,
                false);
        }
    }
}