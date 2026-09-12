namespace ParcelManager.Models
{
    public class UploadResult
    {
        public int Uploaded { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }

        public string Message { get; set; } = "";
    }
}

