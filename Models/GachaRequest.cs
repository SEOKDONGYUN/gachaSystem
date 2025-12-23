namespace GachaSystem.Models
{
    public class GachaRequest
    {
        public int PullCount { get; set; } = 1; // 뽑기 횟수 (1회 or 10회 등)
    }

    public class PickupGachaRequest : GachaRequest
    {
        public List<int> PickupItemIds { get; set; } = new List<int>(); // 픽업할 아이템 ID 목록 (최대 5개)
        public double PickupBoostMultiplier { get; set; } = 2.0; // 픽업 가중치 배율
    }
}
