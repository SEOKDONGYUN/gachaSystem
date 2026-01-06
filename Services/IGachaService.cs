using GachaSystem.Models;

namespace GachaSystem.Services
{
    public interface IGachaService
    {
        GachaResult PullGacha();
        GachaResult PullPickupGacha(List<int> pickupItemIds);
        void ResetStatistics();
        GachaStatistics GetStatistics();
    }
}
