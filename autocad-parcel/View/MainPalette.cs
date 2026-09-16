using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ParcelManager.Views
{
    [SupportedOSPlatform("windows")]
    public class MainPalette
    {
        private static PaletteSet? palette;

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


        // =============================================================
        // LOAD TOPMAP LOGO
        // =============================================================

        private static Image? LoadLogo()
        {
            Assembly assembly =
                Assembly.GetExecutingAssembly();

            string[] resources =
                assembly.GetManifestResourceNames();

            string? resourceName =
                resources.FirstOrDefault(name =>
                    name.EndsWith(
                        "Assets.topmap-logo.png",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (resourceName == null)
                return null;

            using Stream? stream =
                assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                return null;

            try
            {
                using Bitmap temp =
                    new Bitmap(stream);

                return new Bitmap(temp);
            }
            catch
            {
                return null;
            }
        }


        // =============================================================
        // LOAD TOPMAP ICON
        // =============================================================

        private static Icon? LoadIcon()
        {
            Assembly assembly =
                Assembly.GetExecutingAssembly();

            string[] resources =
                assembly.GetManifestResourceNames();

            string? resourceName =
                resources.FirstOrDefault(name =>
                    name.EndsWith(
                        "Assets.topmap-logo.ico",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (resourceName == null)
                return null;

            using Stream? stream =
                assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                return null;

            try
            {
                return new Icon(stream);
            }
            catch
            {
                return null;
            }
        }


        // =============================================================
        // SHOW PALETTE
        // =============================================================

        public static void Show()
        {
            if (palette == null)
            {
                palette = new PaletteSet("TM Land Parcel Buddy")
                {
                    Size = new Size(420, 720),
                    MinimumSize = new Size(320, 500),
                    Icon = LoadIcon()
                };


                // =========================================================
                // MAIN PANEL
                // =========================================================

                Panel mainPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };


                // =========================================================
                // HEADER
                // =========================================================

                Panel headerPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 76,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(14, 10, 14, 8)
                };


                // =========================================================
                // TOPMAP LOGO
                // =========================================================

                PictureBox logo = new PictureBox
                {
                    Image = LoadLogo(),
                    Size = new Size(35, 35),
                    Location = new Point(14, 10),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Background,
                    Margin = new Padding(0)
                };


                // =========================================================
                // TITLE
                // =========================================================

                Label title = new Label
                {
                    Text = "TM LAND PARCEL BUDDY",
                    Location = new Point(68, 7),
                    Size = new Size(300, 30),
                    ForeColor = Text,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    Font = new Font(
                        "Segoe UI",
                        12,
                        FontStyle.Bold
                    ),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Anchor =
                        AnchorStyles.Top |
                        AnchorStyles.Left |
                        AnchorStyles.Right
                };


                // =========================================================
                // SUBTITLE
                // =========================================================

                Label subtitle = new Label
                {
                    Text = "Powered by TopMap Solutions 2026",
                    Location = new Point(68, 36),
                    Size = new Size(300, 22),
                    ForeColor = SecondaryText,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    Font = new Font(
                        "Segoe UI",
                        9
                    ),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Anchor =
                        AnchorStyles.Top |
                        AnchorStyles.Left |
                        AnchorStyles.Right
                };


                headerPanel.Controls.Add(logo);
                headerPanel.Controls.Add(title);
                headerPanel.Controls.Add(subtitle);


                // =========================================================
                // HEADER DIVIDER
                // =========================================================

                Panel headerDivider = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 1,
                    BackColor = ButtonBorder,
                    Margin = new Padding(0)
                };


                // =========================================================
                // GENERAL TOOLS
                // =========================================================

                FlowLayoutPanel generalPanel =
                    CreateSectionPanel();


                Button setMapButton =
                    CreateButton("Set Map");

                Button insertParcelTextButton =
                    CreateButton("Insert Parcel Text");


                generalPanel.Controls.Add(setMapButton);
                generalPanel.Controls.Add(insertParcelTextButton);


                // =========================================================
                // INSPECTION
                // =========================================================

                FlowLayoutPanel inspectionPanel =
                    CreateSectionPanel();


                Label inspectionLabel = new Label
                {
                    Text = "INSPECTION",
                    Height = 28,
                    AutoSize = false,
                    ForeColor = Accent,
                    BackColor = Background,
                    Margin = new Padding(0, 2, 0, 5),
                    Padding = new Padding(2, 0, 0, 0),
                    Font = new Font(
                        "Segoe UI",
                        9,
                        FontStyle.Bold
                    ),
                    TextAlign = ContentAlignment.MiddleLeft
                };


                Button inspectOverlapButton =
                    CreateButton("Inspect Overlap");

                Button inspectLinesButton =
                    CreateButton("Inspect Lines");


                inspectionPanel.Controls.Add(inspectionLabel);
                inspectionPanel.Controls.Add(inspectOverlapButton);
                inspectionPanel.Controls.Add(inspectLinesButton);


                // =========================================================
                // COMMAND EVENTS
                // =========================================================

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


                // =========================================================
                // MAIN LAYOUT
                // =========================================================
                Panel fillPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Background,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };

                mainPanel.Controls.Add(fillPanel);

                mainPanel.Controls.Add(inspectionPanel);
                mainPanel.Controls.Add(generalPanel);
                mainPanel.Controls.Add(headerDivider);
                mainPanel.Controls.Add(headerPanel);


                // =========================================================
                // RESPONSIVE WIDTH
                // =========================================================

                mainPanel.Resize += (sender, e) =>
                {
                    int width =
                        Math.Max(
                            240,
                            mainPanel.ClientSize.Width - 28
                        );

                    foreach (Control control in generalPanel.Controls)
                    {
                        if (control is Button button)
                            button.Width = width;
                    }

                    foreach (Control control in inspectionPanel.Controls)
                    {
                        if (control is Button button)
                            button.Width = width;

                        if (control is Label label)
                            label.Width = width;
                    }

                    title.Width =
                        Math.Max(
                            180,
                            mainPanel.ClientSize.Width - 90
                        );

                    subtitle.Width =
                        Math.Max(
                            180,
                            mainPanel.ClientSize.Width - 90
                        );
                };


                // =========================================================
                // PALETTE TAB
                // =========================================================

                palette.Add(
                    "Tools",
                    mainPanel
                );
            }

            palette.Visible = true;
        }


        // =============================================================
        // CREATE SECTION
        // =============================================================

        private static FlowLayoutPanel CreateSectionPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Background,
                Margin = new Padding(0),
                Padding = new Padding(14, 12, 14, 4)
            };
        }


        // =============================================================
        // CREATE BUTTON
        // =============================================================

        private static Button CreateButton(string text)
        {
            Button button = new Button
            {
                Text = text,
                Height = 42,
                Width = 390,
                BackColor = ButtonColor,
                ForeColor = Text,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 7),
                Padding = new Padding(12, 0, 10, 0),
                Font = new Font(
                    "Segoe UI",
                    10
                ),
                TextAlign = ContentAlignment.MiddleLeft,
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


        // =============================================================
        // RUN AUTOCAD COMMAND
        // =============================================================

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
                false
            );
        }
    }
}
