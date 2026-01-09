namespace GachaSystem.Models
{
    public class GachaItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Rarity Rarity { get; set; }
        public int BaseWeight { get; set; }  // JSON 로딩용
    }

    public class GachaResultItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Rarity Rarity { get; set; }
        public bool PickUp { get; set; }
    }
}
