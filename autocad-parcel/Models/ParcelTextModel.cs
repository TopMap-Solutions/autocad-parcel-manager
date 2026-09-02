namespace autocad_parcel.Models
{
    public class ParcelTextModel
    {
        public string Owner { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public decimal DeclaredArea { get; set; }
        public string LandClass { get; set; } = string.Empty;

        public string GetFormattedText()
        {
            string formattedArea =
                DeclaredArea.ToString("#,##0.##");

            return
                $"{Owner}\\P" +
                $"{Pin}\\P" +
                $"{Lot}\\P" +
                $"A={formattedArea} SQ.M.\\P" +
                $"CLASS: {LandClass}";
        }
    }
}