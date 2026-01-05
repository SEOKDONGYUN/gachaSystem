using GachaSystem.Models;
using System.Text.Json;
using Services;

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

        // ========== WeightedRandom 사용 예제 ==========

        /// <summary>
        /// 예제 1: WeightedRandom을 사용한 일반 가챠
        /// </summary>
        public GachaResult PullGachaWithWeightedRandom(int pullCount)
        {
            var weightedRandom = new WeightedRandom<GachaItem>();

            // 가챠 풀의 모든 아이템을 WeightedRandom에 추가
            foreach (var item in _gachaPool)
            {
                weightedRandom.Push(item.BaseWeight, item);
            }

            var result = new GachaResult
            {
                TotalPulls = pullCount,
                Timestamp = DateTime.UtcNow
            };

            for (int i = 0; i < pullCount; i++)
            {
                var item = weightedRandom.Random();
                result.Items.Add(item);
            }

            return result;
        }

        /// <summary>
        /// 예제 2: WeightedExtractor를 사용한 중복 없는 가챠
        /// 같은 아이템이 중복으로 나오지 않도록 보장
        /// </summary>
        public List<GachaItem> PullUniqueItems(int count)
        {
            if (count > _gachaPool.Count)
            {
                throw new ArgumentException($"요청한 개수({count})가 전체 아이템 수({_gachaPool.Count})보다 많습니다.");
            }

            var weightedRandom = new WeightedRandom<GachaItem>();

            foreach (var item in _gachaPool)
            {
                weightedRandom.Push(item.BaseWeight, item);
            }

            var extractor = weightedRandom.Extractor();
            var uniqueItems = new List<GachaItem>();

            for (int i = 0; i < count && extractor.Length > 0; i++)
            {
                var item = extractor.Random();
                uniqueItems.Add(item);
            }

            return uniqueItems;
        }

        /// <summary>
        /// 예제 3: 특정 아이템을 제외한 가챠
        /// 이미 보유한 아이템을 제외하고 뽑기
        /// </summary>
        public GachaResult PullExcludingItems(int pullCount, List<int> excludeItemIds)
        {
            var weightedRandom = new WeightedRandom<GachaItem>();

            foreach (var item in _gachaPool)
            {
                weightedRandom.Push(item.BaseWeight, item);
            }

            var extractor = weightedRandom.Extractor();

            // 제외할 아이템 목록 생성
            var excludeItems = _gachaPool.Where(item => excludeItemIds.Contains(item.Id)).ToList();
            extractor.Exclude(excludeItems, (a, b) => a.Id == b.Id);

            if (extractor.Length == 0)
            {
                throw new InvalidOperationException("제외 후 남은 아이템이 없습니다.");
            }

            var result = new GachaResult
            {
                TotalPulls = pullCount,
                Timestamp = DateTime.UtcNow
            };

            for (int i = 0; i < pullCount; i++)
            {
                // Slice를 통해 매번 새로운 extractor 생성하여 중복 허용
                var tempExtractor = weightedRandom.Extractor();
                tempExtractor.Exclude(excludeItems, (a, b) => a.Id == b.Id);
                var item = tempExtractor.Random();
                result.Items.Add(item);
            }

            return result;
        }

        /// <summary>
        /// 예제 4: 레어도별 가챠 (특정 레어도만 뽑기)
        /// </summary>
        public List<GachaItem> PullByRarity(int rarity, int pullCount)
        {
            var rarityItems = _gachaPool.Where(item => item.Rarity == rarity).ToList();

            if (rarityItems.Count == 0)
            {
                throw new ArgumentException($"레어도 {rarity}에 해당하는 아이템이 없습니다.");
            }

            var weightedRandom = new WeightedRandom<GachaItem>();

            foreach (var item in rarityItems)
            {
                weightedRandom.Push(item.BaseWeight, item);
            }

            var items = new List<GachaItem>();
            for (int i = 0; i < pullCount; i++)
            {
                items.Add(weightedRandom.Random());
            }

            return items;
        }

        /// <summary>
        /// 예제 5: 가챠 풀 정보 조회 (ForEach 사용)
        /// </summary>
        public void PrintGachaPoolInfo()
        {
            var weightedRandom = new WeightedRandom<GachaItem>();

            foreach (var item in _gachaPool)
            {
                weightedRandom.Push(item.BaseWeight, item);
            }

            int totalWeight = weightedRandom.Weight();

            Console.WriteLine("=== 가챠 풀 정보 ===");
            Console.WriteLine($"전체 아이템 수: {weightedRandom.Length}");
            Console.WriteLine($"전체 가중치: {totalWeight}\n");

            Console.WriteLine("아이템 목록:");
            weightedRandom.ForEach(itemInfo =>
            {
                double probability = (itemInfo.Weight / (double)totalWeight) * 100;
                Console.WriteLine($"  [{itemInfo.Value.Rarity}★] {itemInfo.Value.Name,-10} - " +
                                $"가중치: {itemInfo.Weight,3} (Base: {itemInfo.Base,3}) | " +
                                $"확률: {probability:F2}%");
            });
        }

        /// <summary>
        /// 예제 6: Next 메서드를 사용한 순차/랜덤 선택
        /// </summary>
        public List<GachaItem> GetItemsSequentially(int count)
        {
            var weightedRandom = new WeightedRandom<GachaItem>();

            foreach (var item in _gachaPool)
            {
                weightedRandom.Push(item.BaseWeight, item);
            }

            var items = new List<GachaItem>();
            for (int i = 0; i < count; i++)
            {
                items.Add(weightedRandom.Next(step: true));
            }

            return items;
        }

        /// <summary>
        /// 예제 7: 픽업 가챠 (WeightedRandom 버전)
        /// 특정 아이템의 가중치를 증가시켜 확률 상승
        /// </summary>
        public GachaResult PullPickupGachaWithWeightedRandom(int pullCount, List<int> pickupItemIds, double boostMultiplier = 2.0)
        {
            if (pickupItemIds.Count > 5)
            {
                throw new ArgumentException("픽업 아이템은 최대 5개까지 선택할 수 있습니다.");
            }

            var weightedRandom = new WeightedRandom<GachaItem>();

            foreach (var item in _gachaPool)
            {
                int weight = pickupItemIds.Contains(item.Id)
                    ? (int)(item.BaseWeight * boostMultiplier)
                    : item.BaseWeight;

                weightedRandom.Push(weight, item);
            }

            var result = new GachaResult
            {
                TotalPulls = pullCount,
                Timestamp = DateTime.UtcNow
            };

            for (int i = 0; i < pullCount; i++)
            {
                var item = weightedRandom.Random();
                result.Items.Add(item);
            }

            return result;
        }
    }
}
