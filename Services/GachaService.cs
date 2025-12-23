using GachaSystem.Models;
using System.Text.Json;

namespace GachaSystem.Services
{
    public class GachaService : IGachaService
    {
        private readonly List<GachaItem> _gachaPool;
        private readonly Random _random;

        public GachaService()
        {
            _random = new Random();
            _gachaPool = InitializeGachaPool();
        }

        private List<GachaItem> InitializeGachaPool()
        {
            var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "gacha-items.json");

            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"가챠 아이템 데이터 파일을 찾을 수 없습니다: {jsonFilePath}");
            }

            var jsonString = File.ReadAllText(jsonFilePath);
            var items = JsonSerializer.Deserialize<List<GachaItem>>(jsonString);

            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException("가챠 아이템 데이터를 로드할 수 없습니다.");
            }

            return items;
        }

        public List<GachaItem> GetAllItems()
        {
            return _gachaPool;
        }

        public GachaItem? GetItemById(int id)
        {
            return _gachaPool.FirstOrDefault(x => x.Id == id);
        }

        public GachaResult PullNormalGacha(int pullCount)
        {
            var result = new GachaResult
            {
                TotalPulls = pullCount,
                Timestamp = DateTime.UtcNow
            };

            for (int i = 0; i < pullCount; i++)
            {
                var item = WeightedRandomSelection(_gachaPool, item => item.BaseWeight);
                result.Items.Add(item);
            }

            return result;
        }

        public GachaResult PullPickupGacha(int pullCount, List<int> pickupItemIds, double boostMultiplier = 2.0)
        {
            if (pickupItemIds.Count > 5)
            {
                throw new ArgumentException("픽업 아이템은 최대 5개까지 선택할 수 있습니다.");
            }

            var result = new GachaResult
            {
                TotalPulls = pullCount,
                Timestamp = DateTime.UtcNow
            };

            // 픽업 가중치가 적용된 임시 풀 생성
            var pickupPool = _gachaPool.Select(item => new
            {
                Item = item,
                Weight = pickupItemIds.Contains(item.Id)
                    ? item.BaseWeight * boostMultiplier
                    : item.BaseWeight
            }).ToList();

            for (int i = 0; i < pullCount; i++)
            {
                var selected = WeightedRandomSelection(pickupPool, x => x.Weight);
                result.Items.Add(selected.Item);
            }

            return result;
        }

        /// <summary>
        /// 가중치 기반 랜덤 선택 알고리즘
        /// </summary>
        private T WeightedRandomSelection<T>(IEnumerable<T> items, Func<T, double> weightSelector)
        {
            var itemList = items.ToList();
            var totalWeight = itemList.Sum(weightSelector);
            var randomValue = _random.NextDouble() * totalWeight;

            double cumulativeWeight = 0;
            foreach (var item in itemList)
            {
                cumulativeWeight += weightSelector(item);
                if (randomValue <= cumulativeWeight)
                {
                    return item;
                }
            }

            // 폴백 (일반적으로 도달하지 않음)
            return itemList.Last();
        }
    }
}
