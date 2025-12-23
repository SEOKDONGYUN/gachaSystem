using GachaSystem.Models;

namespace GachaSystem.Services
{
    public interface IGachaService
    {
        GachaResult PullNormalGacha(int pullCount);
        GachaResult PullPickupGacha(int pullCount, List<int> pickupItemIds, double boostMultiplier = 2.0);
        List<GachaItem> GetAllItems();
        GachaItem? GetItemById(int id);
    }
}
