namespace GachaSystem.Models
{
    /// <summary>
    /// 픽업 가챠 요청 모델
    /// </summary>
    public class PickupGachaRequest
    {
        public List<int> PickupItemIds { get; set; } = new List<int>();
    }
}
