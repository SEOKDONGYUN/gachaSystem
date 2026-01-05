namespace GachaSystem.Models
{
    public class GachaItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Rarity Rarity { get; set; }
        public int BaseWeight { get; set; }
    }
}
