using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class SyncService
    {
        private readonly HttpClient _httpClient;

        private const string ManifestUrl =
            "http://127.0.0.1:8000/api/parcels/sync/manifest/";

        private const string DownloadUrl =
            "http://127.0.0.1:8000/api/parcels/sync/download/";


        public SyncService()
        {
            _httpClient = new HttpClient();
        }


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

                if (localBarangay == null)
                {
                    filesToSync.Add(file);
                    continue;
                }

                string localPath =
                    localBarangay.DrawingPath;

                if (!File.Exists(localPath))
                {
                    filesToSync.Add(file);
                    continue;
                }

                string localHash =
                    CalculateSha256(localPath);

                bool hashesMatch =
                    string.Equals(
                        localHash,
                        file.Sha256,
                        StringComparison.OrdinalIgnoreCase);

                if (!hashesMatch)
                {
                    filesToSync.Add(file);
                }
            }

            return filesToSync;
        }


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

            string downloadedHash =
                CalculateSha256(tempPath);

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
                Path.GetDirectoryName(localPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string backupPath =
                $"{localPath}.sync-backup";

            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                if (File.Exists(localPath))
                {
                    File.Move(
                        localPath,
                        backupPath);
                }

                File.Move(
                    tempPath,
                    localPath);

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
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


        public async Task<int> SyncAsync(
            string projectFolder,
            IEnumerable<Barangay> localBarangays)
        {
            Manifest manifest =
                await DownloadManifestAsync();

            List<ManifestFile> filesToSync =
                GetFilesToSync(
                    manifest,
                    localBarangays);

            if (filesToSync.Count == 0)
            {
                return 0;
            }

            string tempFolder =
                Path.Combine(
                    projectFolder,
                    ".sync-temp");

            int syncedCount = 0;

            try
            {
                foreach (ManifestFile file in filesToSync)
                {
                    string localPath =
                        Path.Combine(
                            projectFolder,
                            file.Filename);

                    string tempPath =
                        await DownloadImportDwgAsync(
                            file.ImportId,
                            tempFolder,
                            file.Sha256);

                    DeleteOldBarangayDrawings(
                        projectFolder,
                        file.Barangay,
                        file.Filename);

                    ReplaceLocalDrawing(
                        tempPath,
                        localPath);

                    syncedCount++;
                }
            }
            finally
            {
                if (Directory.Exists(tempFolder))
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

            return syncedCount;
        }
    }
}