using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class DxfService
    {
        private readonly ConfigService _configService;

        public DxfService(
            ConfigService configService)
        {
            _configService = configService;
        }

        public async Task<DxfConversionResult> ConvertAllAsync(
            string rootFolder)
        {
            var result =
                new DxfConversionResult();

            if (!Directory.Exists(rootFolder))
            {
                return result;
            }

            string coreConsolePath =
                _configService.Config.CoreConsolePath;

            if (!File.Exists(coreConsolePath))
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

                string metadataPath =
                    GetMetadataPath(
                        dxfPath);

                try
                {
                    DeleteOldVersionDxfs(
                        dxfFolder,
                        fileName);

                    string dwgSha =
                        CalculateSha256(
                            dwgPath);

                    bool dxfIsCurrent =
                        IsDxfCurrent(
                            dxfPath,
                            metadataPath,
                            dwgSha);

                    if (dxfIsCurrent)
                    {
                        result.Skipped++;

                        continue;
                    }

                    DeleteIfExists(
                        dxfPath);

                    DeleteIfExists(
                        metadataPath);

                    bool success =
                        await ConvertSingleAsync(
                            dwgPath,
                            dxfPath,
                            coreConsolePath);

                    if (!success)
                    {
                        result.Failed++;

                        continue;
                    }

                    WriteDxfMetadata(
                        metadataPath,
                        dwgSha);

                    result.Success++;
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

        private string GetMetadataPath(
            string dxfPath)
        {
            return dxfPath + ".sha256";
        }

        private bool IsDxfCurrent(
            string dxfPath,
            string metadataPath,
            string dwgSha)
        {
            if (!File.Exists(dxfPath))
            {
                return false;
            }

            if (!File.Exists(metadataPath))
            {
                return false;
            }

            string storedSha =
                File.ReadAllText(
                    metadataPath)
                .Trim();

            return string.Equals(
                storedSha,
                dwgSha,
                StringComparison.OrdinalIgnoreCase);
        }

        private void WriteDxfMetadata(
            string metadataPath,
            string dwgSha)
        {
            File.WriteAllText(
                metadataPath,
                dwgSha);
        }

        private void DeleteIfExists(
            string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch
            {
            }
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

                DeleteIfExists(
                    dxfPath);

                DeleteIfExists(
                    GetMetadataPath(
                        dxfPath));
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

                DeleteIfExists(
                    dxfPath);

                DeleteIfExists(
                    GetMetadataPath(
                        dxfPath));
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
            string dxfPath,
            string coreConsolePath)
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
                            coreConsolePath,

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
                DeleteIfExists(
                    scriptPath);
            }
        }

        private string CalculateSha256(
            string filePath)
        {
            using FileStream stream =
                File.OpenRead(filePath);

            byte[] hash =
                SHA256.HashData(
                    stream);

            return Convert.ToHexString(
                hash)
                .ToLowerInvariant();
        }
    }
}