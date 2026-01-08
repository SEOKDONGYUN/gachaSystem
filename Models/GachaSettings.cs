namespace GachaSystem.Models
{
    /// <summary>
    /// 가챠 시스템 설정
    /// Data/gacha-settings.json 파일에서 로드
    /// </summary>
    public class GachaSettings
    {
        /// <summary>
        /// 픽업 가챠에서 선택된 아이템의 가중치 부스트 값
        /// 기본값: 100
        /// </summary>
        public int BoostMultiplier { get; set; } = 100;
    }
}
