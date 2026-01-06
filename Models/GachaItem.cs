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

    public class GachaItemCount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Rarity Rarity { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }  // 전체 뽑기 중 이 아이템이 차지하는 비율 (%)
    }
}
