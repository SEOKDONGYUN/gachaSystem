namespace GachaSystem.Models
{
    public class GachaResult
    {
        public List<GachaResultItem> Result { get; set; } = new List<GachaResultItem>();
        public List<GachaItemCount> Items { get; set; } = new List<GachaItemCount>();
    }
}
