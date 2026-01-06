using GachaSystem.Models;

namespace GachaSystem.Services
{
    public interface IGachaService
    {
        GachaResult PullGacha();
        GachaResult PullPickupGacha(List<int> pickupItemIds);
        List<string> GetAvailablePools();
        void ResetStatistics();
        GachaStatistics GetStatistics();
    }
}
