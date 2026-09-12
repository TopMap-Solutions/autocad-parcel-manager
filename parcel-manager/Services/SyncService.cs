using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace ParcelManager.Services
{
    public class SyncService
    {
        private readonly HttpClient _httpClient;

        // I might add a settings config for this
        private const string LatestDwgsUrl =
            "http://127.0.0.1:8000/api/parcels/download/latest-dwgs/";


        public SyncService()
        {
            _httpClient = new HttpClient();
        }


        public async Task<string> DownloadLatestDwgsAsync(
            string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            string zipPath = Path.Combine(
                outputFolder,
                "parcels_latest.zip");

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    LatestDwgsUrl);

            response.EnsureSuccessStatusCode();

            await using Stream stream =
                await response.Content.ReadAsStreamAsync();

            await using FileStream file =
                File.Create(zipPath);

            await stream.CopyToAsync(file);

            return zipPath;
        }


        public void ExtractDwgs(
            string zipPath,
            string outputFolder)
        {
            if (!File.Exists(zipPath))
            {
                throw new FileNotFoundException(
                    "The downloaded ZIP file was not found.",
                    zipPath);
            }

            Directory.CreateDirectory(outputFolder);

            ZipFile.ExtractToDirectory(
                zipPath,
                outputFolder,
                overwriteFiles: true);
        }
    }
}