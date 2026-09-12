using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class DxfService
    {
        private const string CoreConsolePath =
            @"C:\Program Files\Autodesk\AutoCAD 2027\accoreconsole.exe";


        public async Task<DxfConversionResult> ConvertAllAsync(
            string rootFolder)
        {
            var result =
                new DxfConversionResult();


            if (!Directory.Exists(rootFolder))
            {
                return result;
            }


            if (!File.Exists(CoreConsolePath))
            {
                return result;
            }


            string dxfFolder =
                Path.Combine(
                    rootFolder,
                    "DXF");


            Directory.CreateDirectory(
                dxfFolder);


            string[] dwgFiles =
                Directory.GetFiles(
                    rootFolder,
                    "*.dwg",
                    SearchOption.AllDirectories)
                .Where(path =>
                    !path.StartsWith(
                        dxfFolder,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            DeleteOrphanDxfs(
                dxfFolder,
                dwgFiles);


            foreach (string dwgPath in dwgFiles)
            {
                string? fileName =
                    Path.GetFileNameWithoutExtension(
                        dwgPath);


                if (string.IsNullOrWhiteSpace(
                        fileName))
                {
                    continue;
                }


                string dxfPath =
                    Path.Combine(
                        dxfFolder,
                        fileName + ".dxf");


                try
                {
                    DeleteOldVersionDxfs(
                        dxfFolder,
                        fileName);

                    if (File.Exists(dxfPath))
                    {
                        result.Skipped++;

                        continue;
                    }

                    bool success =
                        await ConvertSingleAsync(
                            dwgPath,
                            dxfPath);


                    if (success)
                    {
                        result.Success++;
                    }
                    else
                    {
                        result.Failed++;
                    }
                }
                catch
                {
                    result.Failed++;
                }
            }

            result.Message =
                $"DXF conversion complete.\n\n" +
                $"Converted: {result.Success}\n" +
                $"Skipped:   {result.Skipped}\n" +
                $"Failed:    {result.Failed}";


            return result;
        }


        private void DeleteOrphanDxfs(
            string dxfFolder,
            IEnumerable<string> dwgFiles)
        {
            var currentDwgNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);


            foreach (string dwgPath in dwgFiles)
            {
                string? fileName =
                    Path.GetFileNameWithoutExtension(
                        dwgPath);


                if (string.IsNullOrWhiteSpace(
                        fileName))
                {
                    continue;
                }


                currentDwgNames.Add(
                    fileName);
            }


            string[] existingDxfs =
                Directory.GetFiles(
                    dxfFolder,
                    "*.dxf",
                    SearchOption.TopDirectoryOnly);


            foreach (string dxfPath in existingDxfs)
            {
                string? dxfFileName =
                    Path.GetFileNameWithoutExtension(
                        dxfPath);

                if (string.IsNullOrWhiteSpace(
                        dxfFileName))
                {
                    continue;
                }

                if (currentDwgNames.Contains(
                        dxfFileName))
                {
                    continue;
                }


                try
                {
                    File.Delete(
                        dxfPath);
                }
                catch
                {
                    /*
                     * Ignore deletion errors.
                     */
                }
            }
        }


        private void DeleteOldVersionDxfs(
            string dxfFolder,
            string currentFileName)
        {

            string currentBarangay =
                GetBarangayName(
                    currentFileName);


            string[] existingDxfs =
                Directory.GetFiles(
                    dxfFolder,
                    "*.dxf",
                    SearchOption.TopDirectoryOnly);


            foreach (string dxfPath in existingDxfs)
            {
                string? existingFileName =
                    Path.GetFileNameWithoutExtension(
                        dxfPath);

                if (string.IsNullOrWhiteSpace(
                        existingFileName))
                {
                    continue;
                }


                string existingBarangay =
                    GetBarangayName(
                        existingFileName);

                if (!string.Equals(
                        existingBarangay,
                        currentBarangay,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                if (string.Equals(
                        existingFileName,
                        currentFileName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    File.Delete(
                        dxfPath);
                }
                catch
                {
                    /*
                     * Ignore deletion errors.
                     *
                     * Conversion will fail naturally
                     * if the old file prevents creation.
                     */
                }
            }
        }


        private string GetBarangayName(
            string fileName)
        {


            int versionIndex =
                fileName.LastIndexOf(
                    "_v",
                    StringComparison.OrdinalIgnoreCase);


            if (versionIndex >= 0)
            {
                string version =
                    fileName.Substring(
                        versionIndex + 2);


                if (int.TryParse(
                        version,
                        out _))
                {
                    return fileName.Substring(
                        0,
                        versionIndex);
                }
            }

            return fileName;
        }


        private async Task<bool> ConvertSingleAsync(
            string dwgPath,
            string dxfPath)
        {
            string? folder =
                Path.GetDirectoryName(
                    dxfPath);


            if (string.IsNullOrWhiteSpace(
                    folder))
            {
                return false;
            }

            string scriptPath =
                Path.Combine(
                    folder,
                    "__dxf_test.scr");


            try
            {
                File.WriteAllText(
                    scriptPath,
                    $"_.OPEN\n" +
                    $"\"{dwgPath}\"\n" +
                    $"_.DXFOUT\n" +
                    $"\"{dxfPath}\"\n" +
                    $"16\n" +
                    $"_.QUIT\n");


                using var process =
                    new Process();


                process.StartInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            CoreConsolePath,

                        Arguments =
                            $"\"{dwgPath}\" " +
                            $"/s \"{scriptPath}\"",

                        WorkingDirectory =
                            folder,

                        UseShellExecute =
                            false,

                        CreateNoWindow =
                            true
                    };


                process.Start();


                await process.WaitForExitAsync();

                return File.Exists(
                    dxfPath);
            }
            finally
            {
                if (File.Exists(
                        scriptPath))
                {
                    try
                    {
                        File.Delete(
                            scriptPath);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}