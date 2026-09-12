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
}