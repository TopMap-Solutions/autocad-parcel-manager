using System;
using System.Collections.Generic;
using System.Text;

namespace ParcelManager.Models
{
    public class AppConfig
    {
        public string BaseUrl { get; set; } =
            "http://127.0.0.1:8000/";

        public string CoreConsolePath { get; set; } =
            @"C:\Program Files\Autodesk\AutoCAD 2027\accoreconsole.exe";
    }
}