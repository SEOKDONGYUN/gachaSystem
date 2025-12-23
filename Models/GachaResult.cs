namespace GachaSystem.Models
{
    public class GachaResult
    {
        public List<GachaItem> Items { get; set; } = new List<GachaItem>();
        public int TotalPulls { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
