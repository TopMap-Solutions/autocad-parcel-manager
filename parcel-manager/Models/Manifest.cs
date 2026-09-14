using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace ParcelManager.Models
{
    public class Manifest
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("files")]
        public List<ManifestFile> Files { get; set; } = new();
    }

    public class ManifestFile
    {
        [JsonPropertyName("barangay")]
        public string Barangay { get; set; } = string.Empty;

        [JsonPropertyName("import_id")]
        public int ImportId { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;
    }
}