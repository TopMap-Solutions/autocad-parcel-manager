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

        private const string UploadUrl =
            "http://127.0.0.1:8000/api/parcels/upload-parcel-import/";


        public UploadService()
        {
            _httpClient =
                new HttpClient();
        }


        // ============================================================
        // UPLOAD DWG + DXF
        // ============================================================

        public async Task<UploadResult> UploadAsync(
            string dwgPath,
            string dxfPath)
        {
            var result =
                new UploadResult();


            // --------------------------------------------------------
            // VALIDATE DWG
            // --------------------------------------------------------

            if (!File.Exists(dwgPath))
            {
                result.Failed++;

                result.Message =
                    $"DWG file was not found.\n\n" +
                    $"{dwgPath}";

                return result;
            }


            // --------------------------------------------------------
            // VALIDATE DXF
            // --------------------------------------------------------

            if (!File.Exists(dxfPath))
            {
                result.Failed++;

                result.Message =
                    $"DXF file was not found.\n\n" +
                    $"{dxfPath}";

                return result;
            }


            try
            {
                // ----------------------------------------------------
                // CREATE MULTIPART FORM
                // ----------------------------------------------------

                using var form =
                    new MultipartFormDataContent();


                // ----------------------------------------------------
                // OPEN DWG
                // ----------------------------------------------------

                await using FileStream dwgStream =
                    File.OpenRead(dwgPath);


                // ----------------------------------------------------
                // OPEN DXF
                // ----------------------------------------------------

                await using FileStream dxfStream =
                    File.OpenRead(dxfPath);


                // ----------------------------------------------------
                // DWG CONTENT
                // ----------------------------------------------------

                using var dwgContent =
                    new StreamContent(dwgStream);

                dwgContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        "application/octet-stream");


                // ----------------------------------------------------
                // DXF CONTENT
                // ----------------------------------------------------

                using var dxfContent =
                    new StreamContent(dxfStream);

                dxfContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        "application/octet-stream");


                // ----------------------------------------------------
                // ADD DWG
                // ----------------------------------------------------

                form.Add(
                    dwgContent,
                    "dwg",
                    Path.GetFileName(dwgPath));


                // ----------------------------------------------------
                // ADD DXF
                // ----------------------------------------------------

                form.Add(
                    dxfContent,
                    "dxf",
                    Path.GetFileName(dxfPath));


                // ----------------------------------------------------
                // SEND REQUEST
                // ----------------------------------------------------

                using HttpResponseMessage response =
                    await _httpClient.PostAsync(
                        UploadUrl,
                        form);


                // ----------------------------------------------------
                // READ RESPONSE
                // ----------------------------------------------------

                string responseBody =
                    await response.Content.ReadAsStringAsync();


                // ----------------------------------------------------
                // HANDLE HTTP ERROR
                // ----------------------------------------------------

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Upload failed.\n\n" +
                        $"URL: {UploadUrl}\n" +
                        $"Status: {(int)response.StatusCode} " +
                        $"{response.StatusCode}\n\n" +
                        $"Response:\n{responseBody}");
                }


                // ----------------------------------------------------
                // SUCCESS
                // ----------------------------------------------------

                result.Uploaded++;


                // ----------------------------------------------------
                // READ BACKEND MESSAGE
                // ----------------------------------------------------

                try
                {
                    using JsonDocument document =
                        JsonDocument.Parse(
                            responseBody);


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
