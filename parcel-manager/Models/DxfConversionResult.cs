namespace ParcelManager.Models
{
    public class DxfConversionResult
    {
        public int Success { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }

        public string Message { get; set; } = "";
    }
}