using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class SyncService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigService _configService;
        private readonly DxfService _dxfService;
        private readonly UploadService _uploadService;

        public SyncService(
            ConfigService configService)
        {
            _configService =
                configService;

            _httpClient =
                new HttpClient();

            _dxfService =
                new DxfService(
                    _configService);

            _uploadService =
                new UploadService();
        }

        // ============================================================
        // BASE URL
        // ============================================================

        private string GetBaseUrl()
        {
            string baseUrl =
                _configService.Config.BaseUrl;

            if (string.IsNullOrWhiteSpace(
                baseUrl))
            {
                throw new InvalidOperationException(
                    "The application server URL is not configured.");
            }

            return baseUrl.TrimEnd('/');
        }

        // ============================================================
        // MANIFEST
        // ============================================================

        public async Task<Manifest> DownloadManifestAsync()
        {
            string baseUrl =
                GetBaseUrl();

            string manifestUrl =
                $"{baseUrl}/api/parcels/sync/manifest/";

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    manifestUrl);

            string responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Manifest request failed.\n\n" +
                    $"URL: {manifestUrl}\n" +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.StatusCode}\n\n" +
                    $"Response:\n{responseBody}");
            }

            Manifest? manifest =
                System.Text.Json.JsonSerializer.Deserialize<Manifest>(
                    responseBody);

            if (manifest == null)
            {
                throw new InvalidOperationException(
                    "The server returned an empty manifest.");
            }

            return manifest;
        }

        // ============================================================
        // SHA-256
        // ============================================================

        public string CalculateSha256(
            string filePath)
        {
            if (!File.Exists(
                filePath))
            {
                throw new FileNotFoundException(
                    "The file was not found.",
                    filePath);
            }

            using FileStream stream =
                File.OpenRead(filePath);

            byte[] hash =
                SHA256.HashData(
                    stream);

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        // ============================================================
        // DOWNLOAD DETECTION
        //
        // The server manifest is the source of truth.
        //
        // Missing file:
        //     DOWNLOAD
        //
        // Existing file with same SHA:
        //     SKIP
        //
        // Existing file with different SHA:
        //     DOWNLOAD
        //
        // This also works when the project folder is completely empty.
        // ============================================================

        public List<ManifestFile> GetFilesToSync(
            Manifest manifest,
            string projectFolder)
        {
            var filesToSync =
                new List<ManifestFile>();

            foreach (ManifestFile file in manifest.Files)
            {
                if (string.IsNullOrWhiteSpace(
                    file.Filename))
                {
                    continue;
                }

                string localPath =
                    Path.Combine(
                        projectFolder,
                        file.Filename);

                // ----------------------------------------------------
                // File does not exist locally.
                // ----------------------------------------------------

                if (!File.Exists(
                    localPath))
                {
                    filesToSync.Add(
                        file);

                    continue;
                }

                // ----------------------------------------------------
                // File exists.
                // Compare SHA-256.
                // ----------------------------------------------------

                string localHash =
                    CalculateSha256(
                        localPath);

                bool hashesMatch =
                    string.Equals(
                        localHash,
                        file.Sha256,
                        StringComparison.OrdinalIgnoreCase);

                if (hashesMatch)
                {
                    continue;
                }

                // ----------------------------------------------------
                // Same filename but different contents.
                // ----------------------------------------------------

                filesToSync.Add(
                    file);
            }

            return filesToSync;
        }

        // ============================================================
        // BARANGAY FILENAME PART
        //
        // Canocotan
        //     CANOCOTAN
        //
        // Magugpo Poblacion
        //     MAGUGPO-POBLACION
        // ============================================================

        private string GetBarangayFilenamePart(
            string barangayName)
        {
            return (
                barangayName ?? string.Empty
            )
            .Trim()
            .ToUpperInvariant()
            .Replace(
                " ",
                "-");
        }

        // ============================================================
        // CANONICAL DRAWING PARSER
        //
        // Valid:
        //
        // 03-CANOCOTAN-2026-v12.dwg
        // 13-MAGUGPO-POBLACION-2026-v15.dwg
        // 22-VISAYAN-VILLAGE-2026-v8.dwg
        //
        // Structure:
        //
        // NN-BARANGAY-YYYY-vN.dwg
        //
        // Example:
        //
        // 03-CANOCOTAN-2026-v12.dwg
        // ^^ barangay number
        //    ^^^^^^^^^ barangay
        //               ^^^^ year
        //                    ^^ version
        // ============================================================

        private bool TryGetCanonicalDrawingVersion(
            string filename,
            string barangayPart,
            out int version)
        {
            version = -1;

            string name =
                Path.GetFileNameWithoutExtension(
                    filename);

            if (string.IsNullOrWhiteSpace(
                name))
            {
                return false;
            }

            // --------------------------------------------------------
            // Find first dash.
            //
            // Canonical filename must begin with exactly:
            //
            // NN-
            // --------------------------------------------------------

            int firstDash =
                name.IndexOf('-');

            if (firstDash != 2)
            {
                return false;
            }

            string numberPart =
                name.Substring(
                    0,
                    2);

            if (!int.TryParse(
                numberPart,
                out _))
            {
                return false;
            }

            // --------------------------------------------------------
            // Expected prefix:
            //
            // 03-CANOCOTAN-
            // --------------------------------------------------------

            string prefix =
                $"{numberPart}-{barangayPart}-";

            if (!name.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string remaining =
                name.Substring(
                    prefix.Length);

            string[] parts =
                remaining.Split(
                    '-',
                    StringSplitOptions.RemoveEmptyEntries);

            // --------------------------------------------------------
            // Expected:
            //
            // 2026
            // v12
            // --------------------------------------------------------

            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(
                parts[0],
                out _))
            {
                return false;
            }

            string versionPart =
                parts[1];

            if (!versionPart.StartsWith(
                "v",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(
                versionPart.Substring(1),
                out version))
            {
                return false;
            }

            return true;
        }

        // ============================================================
        // CHECK CANONICAL BARANGAY DRAWING
        // ============================================================

        private bool IsCanonicalBarangayDrawing(
            string filename,
            string barangayPart)
        {
            return TryGetCanonicalDrawingVersion(
                filename,
                barangayPart,
                out _);
        }

        // ============================================================
        // GET VERSION FROM CANONICAL FILENAME
        // ============================================================

        private int GetDrawingVersion(
            string filename,
            string barangayPart)
        {
            if (TryGetCanonicalDrawingVersion(
                filename,
                barangayPart,
                out int version))
            {
                return version;
            }

            return -1;
        }

        // ============================================================
        // FIND EXISTING BARANGAY DRAWING
        //
        // Finds the latest canonical version already on disk.
        //
        // Example:
        //
        // 03-CANOCOTAN-2026-v10.dwg
        // 03-CANOCOTAN-2026-v11.dwg
        // 03-CANOCOTAN-2026-v12.dwg
        //
        // Returns v12.
        // ============================================================

        private string? FindExistingBarangayDrawing(
            string projectFolder,
            string barangayName)
        {
            if (!Directory.Exists(
                projectFolder))
            {
                return null;
            }

            string barangayPart =
                GetBarangayFilenamePart(
                    barangayName);

            var candidates =
                Directory.GetFiles(
                    projectFolder,
                    "*.dwg",
                    SearchOption.TopDirectoryOnly)
                .Where(
                    path =>
                        !string.Equals(
                            Path.GetFileName(path),
                            "MASTER.dwg",
                            StringComparison.OrdinalIgnoreCase))
                .Where(
                    path =>
                        IsCanonicalBarangayDrawing(
                            Path.GetFileName(path),
                            barangayPart))
                .OrderByDescending(
                    path =>
                        GetDrawingVersion(
                            Path.GetFileName(path),
                            barangayPart))
                .ToList();

            return candidates.FirstOrDefault();
        }

        // ============================================================
        // DELETE OLD BARANGAY DRAWINGS
        //
        // Example:
        //
        // Existing:
        //
        // 03-CANOCOTAN-2026-v10.dwg
        // 03-CANOCOTAN-2026-v11.dwg
        // 03-CANOCOTAN-2026-v12.dwg
        //
        // Download:
        //
        // 03-CANOCOTAN-2026-v13.dwg
        //
        // Result:
        //
        // 03-CANOCOTAN-2026-v13.dwg
        //
        // MASTER.dwg is never touched.
        // Other barangays are never touched.
        // ============================================================

        private void DeleteOldBarangayDrawings(
            string projectFolder,
            string barangayName,
            string newFilename)
        {
            if (!Directory.Exists(
                projectFolder))
            {
                return;
            }

            string barangayPart =
                GetBarangayFilenamePart(
                    barangayName);

            foreach (string filePath in Directory.GetFiles(
                projectFolder,
                "*.dwg",
                SearchOption.TopDirectoryOnly))
            {
                string filename =
                    Path.GetFileName(
                        filePath);

                // ----------------------------------------------------
                // Never touch MASTER.dwg.
                // ----------------------------------------------------

                if (string.Equals(
                    filename,
                    "MASTER.dwg",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // ----------------------------------------------------
                // Keep the file we're about to install.
                // ----------------------------------------------------

                if (string.Equals(
                    filename,
                    newFilename,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // ----------------------------------------------------
                // Only delete canonical files belonging to this
                // barangay.
                // ----------------------------------------------------

                if (!IsCanonicalBarangayDrawing(
                    filename,
                    barangayPart))
                {
                    continue;
                }

                File.Delete(
                    filePath);
            }
        }

        // ============================================================
        // DOWNLOAD DWG
        // ============================================================

        public async Task<string> DownloadImportDwgAsync(
            int importId,
            string tempFolder,
            string expectedSha256)
        {
            Directory.CreateDirectory(
                tempFolder);

            string tempPath =
                Path.Combine(
                    tempFolder,
                    $"sync_{importId}.dwg");

            if (File.Exists(
                tempPath))
            {
                File.Delete(
                    tempPath);
            }

            string baseUrl =
                GetBaseUrl();

            string url =
                $"{baseUrl}/api/parcels/sync/download/{importId}/";

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    url);

            if (!response.IsSuccessStatusCode)
            {
                string responseBody =
                    await response.Content.ReadAsStringAsync();

                throw new HttpRequestException(
                    $"DWG download failed.\n\n" +
                    $"URL: {url}\n" +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.StatusCode}\n\n" +
                    $"Response:\n{responseBody}");
            }

            await using Stream stream =
                await response.Content.ReadAsStreamAsync();

            await using FileStream file =
                File.Create(
                    tempPath);

            await stream.CopyToAsync(
                file);

            await file.FlushAsync();

            file.Close();

            // --------------------------------------------------------
            // VERIFY SHA-256
            // --------------------------------------------------------

            string downloadedHash =
                CalculateSha256(
                    tempPath);

            if (!string.Equals(
                downloadedHash,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(
                        tempPath);
                }
                catch
                {
                }

                throw new InvalidOperationException(
                    $"SHA-256 verification failed " +
                    $"for import {importId}.\n\n" +
                    $"Expected:\n{expectedSha256}\n\n" +
                    $"Downloaded:\n{downloadedHash}");
            }

            return tempPath;
        }

        // ============================================================
        // REPLACE LOCAL DRAWING
        // ============================================================

        public void ReplaceLocalDrawing(
            string tempPath,
            string localPath)
        {
            if (!File.Exists(
                tempPath))
            {
                throw new FileNotFoundException(
                    "The temporary DWG was not found.",
                    tempPath);
            }

            string? directory =
                Path.GetDirectoryName(
                    localPath);

            if (!string.IsNullOrWhiteSpace(
                directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            string backupPath =
                $"{localPath}.sync-backup";

            try
            {
                if (File.Exists(
                    backupPath))
                {
                    File.Delete(
                        backupPath);
                }

                if (File.Exists(
                    localPath))
                {
                    File.Move(
                        localPath,
                        backupPath);
                }

                File.Move(
                    tempPath,
                    localPath);

                if (File.Exists(
                    backupPath))
                {
                    File.Delete(
                        backupPath);
                }
            }
            catch
            {
                if (!File.Exists(localPath) &&
                    File.Exists(backupPath))
                {
                    File.Move(
                        backupPath,
                        localPath);
                }

                throw;
            }
            finally
            {
                if (File.Exists(
                    tempPath))
                {
                    try
                    {
                        File.Delete(
                            tempPath);
                    }
                    catch
                    {
                    }
                }

                if (File.Exists(
                    backupPath))
                {
                    try
                    {
                        File.Delete(
                            backupPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        // ============================================================
        // LOCAL DWG DETECTION
        //
        // MASTER.dwg is excluded.
        // ============================================================

        private List<string> GetLocalDwgs(
            string projectFolder)
        {
            return Directory.GetFiles(
                projectFolder,
                "*.dwg",
                SearchOption.TopDirectoryOnly)
                .Where(
                    path =>
                        !string.Equals(
                            Path.GetFileName(path),
                            "MASTER.dwg",
                            StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // ============================================================
        // UPLOAD DETECTION
        //
        // If a local DWG:
        //
        // 1. Does not exist on server
        //    -> upload
        //
        // 2. Exists but SHA changed
        //    -> upload
        //
        // 3. Exists and SHA is identical
        //    -> skip
        // ============================================================

        private List<string> GetFilesToUpload(
            string projectFolder,
            Manifest manifest)
        {
            var filesToUpload =
                new List<string>();

            List<string> localDwgs =
                GetLocalDwgs(
                    projectFolder);

            foreach (string dwgPath in localDwgs)
            {
                string filename =
                    Path.GetFileName(
                        dwgPath);

                string localSha =
                    CalculateSha256(
                        dwgPath);

                ManifestFile? serverFile =
                    manifest.Files.FirstOrDefault(
                        file =>
                            string.Equals(
                                file.Filename,
                                filename,
                                StringComparison.OrdinalIgnoreCase));

                // ----------------------------------------------------
                // Local file does not exist on server.
                // ----------------------------------------------------

                if (serverFile == null)
                {
                    filesToUpload.Add(
                        dwgPath);

                    continue;
                }

                // ----------------------------------------------------
                // Compare SHA.
                // ----------------------------------------------------

                bool hashesMatch =
                    string.Equals(
                        localSha,
                        serverFile.Sha256,
                        StringComparison.OrdinalIgnoreCase);

                if (hashesMatch)
                {
                    continue;
                }

                // ----------------------------------------------------
                // Local file was modified.
                // ----------------------------------------------------

                filesToUpload.Add(
                    dwgPath);
            }

            return filesToUpload;
        }

        // ============================================================
        // UPLOAD LOCAL CHANGES
        // ============================================================

        private async Task<int> UploadLocalChangesAsync(
            string projectFolder,
            Manifest manifest)
        {
            List<string> filesToUpload =
                GetFilesToUpload(
                    projectFolder,
                    manifest);

            if (filesToUpload.Count == 0)
            {
                return 0;
            }

            int uploadedCount = 0;

            foreach (string dwgPath in filesToUpload)
            {
                string fileName =
                    Path.GetFileNameWithoutExtension(
                        dwgPath);

                string dxfFolder =
                    Path.Combine(
                        projectFolder,
                        "DXF");

                string dxfPath =
                    Path.Combine(
                        dxfFolder,
                        fileName + ".dxf");

                if (!File.Exists(
                    dxfPath))
                {
                    throw new InvalidOperationException(
                        $"DXF file was not found for:\n\n" +
                        $"{Path.GetFileName(dwgPath)}\n\n" +
                        $"Expected:\n{dxfPath}");
                }

                UploadResult result =
                    await _uploadService.UploadAsync(
                        dwgPath,
                        dxfPath);

                uploadedCount +=
                    result.Uploaded;
            }

            return uploadedCount;
        }

        // ============================================================
        // COMPLETE SYNC
        //
        // Flow:
        //
        // 1. Download server manifest
        //
        // 2. Generate DXFs from local DWGs
        //
        // 3. Upload local changes
        //
        // 4. Refresh server manifest
        //
        // 5. Find missing/changed server files
        //
        // 6. Download them
        //
        // 7. Delete old versions
        //
        // 8. Install new version
        //
        // 9. Generate DXFs again
        //
        // Empty folder is valid.
        // Missing local files are valid.
        // ============================================================

        public async Task<int> SyncAsync(
            string projectFolder,
            IEnumerable<Barangay> localBarangays)
        {
            if (!Directory.Exists(
                projectFolder))
            {
                throw new DirectoryNotFoundException(
                    $"Project folder does not exist:\n\n" +
                    projectFolder);
            }

            // ========================================================
            // 1. GET SERVER MANIFEST
            // ========================================================

            Manifest manifest =
                await DownloadManifestAsync();

            // ========================================================
            // 2. GENERATE DXFs
            //
            // If the folder is empty, this should simply result in
            // nothing to convert.
            // ========================================================

            await _dxfService.ConvertAllAsync(
                projectFolder);

            // ========================================================
            // 3. UPLOAD LOCAL CHANGES
            //
            // If there are no local DWGs, this returns 0.
            // ========================================================

            await UploadLocalChangesAsync(
                projectFolder,
                manifest);

            // ========================================================
            // 4. REFRESH MANIFEST
            //
            // Important because an upload may have created a new
            // server version.
            // ========================================================

            manifest =
                await DownloadManifestAsync();

            // ========================================================
            // 5. DETERMINE DOWNLOADS
            //
            // Empty folder:
            //
            //     Every server file is missing.
            //     Therefore every server file is downloaded.
            //
            // Existing folder:
            //
            //     Same SHA -> skip
            //     Missing -> download
            //     Different SHA -> download
            // ========================================================

            List<ManifestFile> filesToSync =
                GetFilesToSync(
                    manifest,
                    projectFolder);

            if (filesToSync.Count == 0)
            {
                await _dxfService.ConvertAllAsync(
                    projectFolder);

                return 0;
            }

            int syncedCount = 0;

            string tempFolder =
                Path.Combine(
                    projectFolder,
                    ".sync-temp");

            try
            {
                Directory.CreateDirectory(
                    tempFolder);

                foreach (ManifestFile file in filesToSync)
                {
                    // ------------------------------------------------
                    // SERVER MANIFEST FILENAME IS THE SOURCE OF TRUTH
                    //
                    // Example:
                    //
                    // 03-CANOCOTAN-2026-v12.dwg
                    // ------------------------------------------------

                    string localPath =
                        Path.Combine(
                            projectFolder,
                            file.Filename);

                    // ------------------------------------------------
                    // DOWNLOAD
                    // ------------------------------------------------

                    string tempPath =
                        await DownloadImportDwgAsync(
                            file.ImportId,
                            tempFolder,
                            file.Sha256);

                    // ------------------------------------------------
                    // DELETE PREVIOUS CANONICAL VERSIONS
                    //
                    // Example:
                    //
                    // v10
                    // v11
                    // v12
                    //
                    // downloading v13 deletes all three.
                    // ------------------------------------------------

                    DeleteOldBarangayDrawings(
                        projectFolder,
                        file.Barangay,
                        file.Filename);

                    // ------------------------------------------------
                    // INSTALL NEW VERSION
                    // ------------------------------------------------

                    ReplaceLocalDrawing(
                        tempPath,
                        localPath);

                    syncedCount++;
                }
            }
            finally
            {
                // ----------------------------------------------------
                // ALWAYS CLEAN TEMP DIRECTORY
                // ----------------------------------------------------

                if (Directory.Exists(
                    tempFolder))
                {
                    try
                    {
                        Directory.Delete(
                            tempFolder,
                            recursive: true);
                    }
                    catch
                    {
                    }
                }
            }

            // ========================================================
            // 6. GENERATE DXFs AGAIN
            // ========================================================

            await _dxfService.ConvertAllAsync(
                projectFolder);

            // ========================================================
            // 7. RETURN NUMBER OF DOWNLOADED FILES
            // ========================================================

            return syncedCount;
        }
    }
}