namespace GachaSystem.Models
{
    public class GachaResult
    {
        public List<GachaResultItem> Result { get; set; } = new List<GachaResultItem>();

        // 누적 통계
        public GachaStatistics Statistics { get; set; } = new GachaStatistics();
    }

    public class GachaStatistics
    {
        public GachaTypeStats? Normal { get; set; }
        public GachaTypeStats? Pickup { get; set; }
    }

    public class GachaTypeStats
    {
        public int TotalPulls { get; set; }  // 누적 가챠 횟수
        public List<GachaItemCount> Items { get; set; } = new List<GachaItemCount>();  // 누적 아이템 개수 및 확률
    }
}
