using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

using ParcelManager.Models;

namespace ParcelManager.Services
{
    public class UploadService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigService _configService;
        private readonly AuthService _authService;

        public UploadService(
            HttpClient httpClient,
            ConfigService configService,
            AuthService authService
            )
        {
            _httpClient = httpClient;
            _configService = configService;
            _authService = authService;
        }

        // ============================================================
        // UPLOAD DWG + DXF
        // ============================================================

        public async Task<UploadResult> UploadAsync(
            string dwgPath,
            string dxfPath)
        {
            var result = new UploadResult();

            // --------------------------------------------------------
            // VALIDATE FILES
            // --------------------------------------------------------

            if (!File.Exists(dwgPath))
            {
                result.Failed++;
                result.Message =
                    $"DWG file was not found.\n\n{dwgPath}";

                return result;
            }

            if (!File.Exists(dxfPath))
            {
                result.Failed++;
                result.Message =
                    $"DXF file was not found.\n\n{dxfPath}";

                return result;
            }

            // --------------------------------------------------------
            // BUILD URL FROM GLOBAL CONFIG
            // --------------------------------------------------------

            string baseUrl =
                _configService.Config.BaseUrl.TrimEnd('/');

            string uploadUrl =
                $"{baseUrl}/api/parcels/upload-parcel-import/";

            try
            {
                // ----------------------------------------------------
                // MULTIPART FORM
                // ----------------------------------------------------

                using var form =
                    new MultipartFormDataContent();

                await using FileStream dwgStream =
                    File.OpenRead(dwgPath);

                await using FileStream dxfStream =
                    File.OpenRead(dxfPath);

                using var dwgContent =
                    new StreamContent(dwgStream);

                dwgContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        "application/octet-stream");

                using var dxfContent =
                    new StreamContent(dxfStream);

                dxfContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        "application/octet-stream");

                form.Add(
                    dwgContent,
                    "dwg",
                    Path.GetFileName(dwgPath));

                form.Add(
                    dxfContent,
                    "dxf",
                    Path.GetFileName(dxfPath));

                // ----------------------------------------------------
                // SEND REQUEST
                // ----------------------------------------------------

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        uploadUrl)
                    {
                        Content = form
                    };

                _authService.AddAuthorizationHeader(
                    request);

                using HttpResponseMessage response =
                    await _httpClient.SendAsync(
                        request);

                string responseBody =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Upload failed.\n\n" +
                        $"URL: {uploadUrl}\n" +
                        $"Status: {(int)response.StatusCode} " +
                        $"{response.StatusCode}\n\n" +
                        $"Response:\n{responseBody}");
                }

                // ----------------------------------------------------
                // SUCCESS
                // ----------------------------------------------------

                result.Uploaded++;

                try
                {
                    using JsonDocument document =
                        JsonDocument.Parse(responseBody);

                    if (document.RootElement.TryGetProperty(
                        "message",
                        out JsonElement message))
                    {
                        result.Message =
                            message.GetString()
                            ?? "Upload successful.";
                    }
                    else
                    {
                        result.Message =
                            "Upload successful.";
                    }
                }
                catch
                {
                    result.Message =
                        "Upload successful.";
                }
            }
            catch
            {
                result.Failed++;
                throw;
            }

            return result;
        }
    }
}