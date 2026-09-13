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
        private readonly DxfService _dxfService;
        private readonly UploadService _uploadService;


        private const string ManifestUrl =
            "http://127.0.0.1:8000/api/parcels/sync/manifest/";


        private const string DownloadUrl =
            "http://127.0.0.1:8000/api/parcels/sync/download/";


        public SyncService()
        {
            _httpClient =
                new HttpClient();

            _dxfService =
                new DxfService();

            _uploadService =
                new UploadService();
        }


        // ============================================================
        // MANIFEST
        // ============================================================

        public async Task<Manifest> DownloadManifestAsync()
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    ManifestUrl);


            string responseBody =
                await response.Content.ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Manifest request failed.\n\n" +
                    $"URL: {ManifestUrl}\n" +
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
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "The file was not found.",
                    filePath);
            }


            using FileStream stream =
                File.OpenRead(filePath);


            byte[] hash =
                SHA256.HashData(stream);


            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }


        // ============================================================
        // FIND LOCAL BARANGAY
        // ============================================================

        private Barangay? FindLocalBarangay(
            string barangayName,
            IEnumerable<Barangay> localBarangays)
        {
            foreach (Barangay barangay in localBarangays)
            {
                if (string.Equals(
                    barangay.Name,
                    barangayName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return barangay;
                }
            }


            return null;
        }


        // ============================================================
        // DOWNLOAD DETECTION
        //
        // Server is downloaded only when the local file does not
        // already have the same SHA.
        // ============================================================

        public List<ManifestFile> GetFilesToSync(
            Manifest manifest,
            IEnumerable<Barangay> localBarangays)
        {
            var filesToSync =
                new List<ManifestFile>();


            foreach (ManifestFile file in manifest.Files)
            {
                Barangay? localBarangay =
                    FindLocalBarangay(
                        file.Barangay,
                        localBarangays);


                // ----------------------------------------------------
                // BARANGAY DOES NOT EXIST LOCALLY
                // ----------------------------------------------------

                if (localBarangay == null)
                {
                    filesToSync.Add(file);

                    continue;
                }


                string localPath =
                    localBarangay.DrawingPath;


                // ----------------------------------------------------
                // LOCAL DRAWING DOES NOT EXIST
                // ----------------------------------------------------

                if (!File.Exists(localPath))
                {
                    filesToSync.Add(file);

                    continue;
                }


                // ----------------------------------------------------
                // CALCULATE LOCAL SHA
                // ----------------------------------------------------

                string localHash =
                    CalculateSha256(
                        localPath);


                // ----------------------------------------------------
                // SAME SHA
                //
                // Nothing to download.
                // ----------------------------------------------------

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
                // DIFFERENT SHA
                //
                // At this point the caller should already have tried
                // uploading local changes first.
                //
                // If the manifest is already refreshed, a difference
                // means the server version is still different.
                // ----------------------------------------------------

                filesToSync.Add(file);
            }


            return filesToSync;
        }


        // ============================================================
        // DELETE OLD BARANGAY DRAWINGS
        // ============================================================

        private void DeleteOldBarangayDrawings(
            string projectFolder,
            string barangayName,
            string newFilename)
        {
            string prefix =
                $"parcels_{barangayName.Trim().ToLower().Replace(" ", "_")}_";


            foreach (string filePath in Directory.GetFiles(
                projectFolder,
                $"{prefix}*.dwg"))
            {
                if (string.Equals(
                    Path.GetFileName(filePath),
                    newFilename,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                File.Delete(filePath);
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


            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }


            string url =
                $"{DownloadUrl}{importId}/";


            using HttpResponseMessage response =
                await _httpClient.GetAsync(url);


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
                File.Create(tempPath);


            await stream.CopyToAsync(file);


            await file.FlushAsync();


            file.Close();


            // --------------------------------------------------------
            // VERIFY DOWNLOADED SHA
            // --------------------------------------------------------

            string downloadedHash =
                CalculateSha256(
                    tempPath);


            if (!string.Equals(
                downloadedHash,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tempPath);


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
            if (!File.Exists(tempPath))
            {
                throw new FileNotFoundException(
                    "The temporary DWG was not found.",
                    tempPath);
            }


            string? directory =
                Path.GetDirectoryName(
                    localPath);


            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(
                    directory);
            }


            string backupPath =
                $"{localPath}.sync-backup";


            try
            {
                // ----------------------------------------------------
                // REMOVE OLD BACKUP
                // ----------------------------------------------------

                if (File.Exists(backupPath))
                {
                    File.Delete(
                        backupPath);
                }


                // ----------------------------------------------------
                // MOVE CURRENT DRAWING TO BACKUP
                // ----------------------------------------------------

                if (File.Exists(localPath))
                {
                    File.Move(
                        localPath,
                        backupPath);
                }


                // ----------------------------------------------------
                // MOVE DOWNLOADED DRAWING INTO PLACE
                // ----------------------------------------------------

                File.Move(
                    tempPath,
                    localPath);


                // ----------------------------------------------------
                // SUCCESS
                // ----------------------------------------------------

                if (File.Exists(backupPath))
                {
                    File.Delete(
                        backupPath);
                }
            }
            catch
            {
                // ----------------------------------------------------
                // RESTORE BACKUP
                // ----------------------------------------------------

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
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }


                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }


        // ============================================================
        // LOCAL DWG DETECTION
        // ============================================================

        private List<string> GetLocalDwgs(
            string projectFolder)
        {
            return Directory.GetFiles(
                projectFolder,
                "*.dwg",
                SearchOption.TopDirectoryOnly)
                .Where(path =>
                    !string.Equals(
                        Path.GetFileName(path),
                        "MASTER.dwg",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }


        // ============================================================
        // UPLOAD DETECTION
        //
        // IMPORTANT:
        //
        // Filename alone does NOT determine whether a file should
        // be uploaded.
        //
        // SHA-256 determines whether the local content is already
        // present on the server.
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


                // ----------------------------------------------------
                // CALCULATE LOCAL SHA
                // ----------------------------------------------------

                string localSha =
                    CalculateSha256(
                        dwgPath);


                // ----------------------------------------------------
                // FIND SERVER FILE WITH SAME FILENAME
                // ----------------------------------------------------

                ManifestFile? serverFile =
                    manifest.Files.FirstOrDefault(
                        file =>
                            string.Equals(
                                file.Filename,
                                filename,
                                StringComparison.OrdinalIgnoreCase));


                // ----------------------------------------------------
                // BRAND-NEW LOCAL DWG
                //
                // Filename does not exist on server.
                // ----------------------------------------------------

                if (serverFile == null)
                {
                    filesToUpload.Add(
                        dwgPath);

                    continue;
                }


                // ----------------------------------------------------
                // SAME SHA
                //
                // Server already has this exact content.
                // Do NOT upload again.
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
                // SAME FILENAME + DIFFERENT SHA
                //
                // Local drawing was modified.
                //
                // Upload it so the backend can create the next
                // version.
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


                // ----------------------------------------------------
                // DXF MUST EXIST
                // ----------------------------------------------------

                if (!File.Exists(dxfPath))
                {
                    throw new InvalidOperationException(
                        $"DXF file was not found for:\n\n" +
                        $"{Path.GetFileName(dwgPath)}\n\n" +
                        $"Expected:\n{dxfPath}");
                }


                // ----------------------------------------------------
                // UPLOAD DWG + DXF
                // ----------------------------------------------------

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
            // 2. GENERATE DXFs BEFORE UPLOAD
            //
            // This ensures every locally modified DWG has a matching
            // DXF before UploadLocalChangesAsync() runs.
            // ========================================================

            await _dxfService.ConvertAllAsync(
                projectFolder);


            // ========================================================
            // 3. UPLOAD LOCAL CHANGES FIRST
            //
            // IMPORTANT:
            //
            // If local f9.dwg is v6 content and server currently has
            // v5, the SHA will differ.
            //
            // We upload v6 BEFORE downloading anything.
            // ========================================================

            await UploadLocalChangesAsync(
                projectFolder,
                manifest);


            // ========================================================
            // 4. DOWNLOAD FRESH MANIFEST
            //
            // Backend should now contain the newly uploaded version.
            //
            // Example:
            //
            // Before:
            //     f9.dwg -> v5 -> SHA AAA
            //
            // Local:
            //     f9.dwg -> SHA BBB
            //
            // After upload:
            //     f9.dwg -> v6 -> SHA BBB
            //
            // We MUST get the new manifest before download detection.
            // ========================================================

            manifest =
                await DownloadManifestAsync();


            // ========================================================
            // 5. DETERMINE SERVER FILES TO DOWNLOAD
            // ========================================================

            List<ManifestFile> filesToSync =
                GetFilesToSync(
                    manifest,
                    localBarangays);


            // ========================================================
            // 6. DOWNLOAD SERVER CHANGES
            // ========================================================

            int syncedCount = 0;


            string tempFolder =
                Path.Combine(
                    projectFolder,
                    ".sync-temp");


            try
            {
                foreach (ManifestFile file in filesToSync)
                {
                    string localPath =
                        Path.Combine(
                            projectFolder,
                            file.Filename);


                    // ------------------------------------------------
                    // DOWNLOAD TO TEMPORARY LOCATION
                    // ------------------------------------------------

                    string tempPath =
                        await DownloadImportDwgAsync(
                            file.ImportId,
                            tempFolder,
                            file.Sha256);


                    // ------------------------------------------------
                    // REMOVE OLD VERSIONS OF SAME BARANGAY
                    // ------------------------------------------------

                    DeleteOldBarangayDrawings(
                        projectFolder,
                        file.Barangay,
                        file.Filename);


                    // ------------------------------------------------
                    // REPLACE LOCAL DRAWING
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
                // CLEAN TEMP FOLDER
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
            // 7. GENERATE DXFs AGAIN
            //
            // Needed if any server drawings were downloaded.
            // ConvertAllAsync should skip DXFs that are already current.
            // ========================================================

            await _dxfService.ConvertAllAsync(
                projectFolder);


            // ========================================================
            // 8. RETURN DOWNLOAD COUNT
            // ========================================================

            return syncedCount;
        }
    }
}
