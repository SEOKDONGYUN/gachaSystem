using GachaSystem.Models;
using System.Text.Json;
using Services;

namespace GachaSystem.Services
{
    public class GachaService : IGachaService
    {
        private readonly GachaTable _gachaTable;
        private readonly Random _random;

        public GachaService()
        {
            _random = new Random();
            _gachaTable = GachaTable.Instance;
        }

        // ========== WeightedRandom 사용 ==========

        /// <summary>
        /// 일반 가챠 (10회)
        /// 10회째는 confirm 풀을 사용하여 레어리티 2 이상 확정
        /// </summary>
        public GachaResult PullGacha()
        {
            int pullCount = 10;
            string poolName = "normal";

            var pulledItems = new List<GachaItem>();

            for (int i = 0; i < pullCount; i++)
            {
                // 10회째(마지막)는 confirm 풀 사용, 나머지는 요청한 풀 사용
                var currentPoolName = (i == pullCount - 1) ? "confirm" : poolName;
                var weightedRandom = _gachaTable.GetWeightedRandomPool(currentPoolName);

                var item = weightedRandom.Random();
                pulledItems.Add(item);
            }

            return CreateGachaResult(pulledItems, new List<int>(), isPickup: false);
        }

        /// <summary>
        /// 픽업 가챠 (10회)
        /// 특정 아이템의 가중치에 boostMultiplier를 더해 확률 상승
        /// 10회째는 pickup-confirm 풀을 사용하여 레어리티 2 이상 확정
        /// 레어리티 3(SSR) 아이템 3개 픽업 필수
        /// </summary>
        public GachaResult PullPickupGacha(List<int> pickupItemIds)
        {
            int pullCount = 10;
            int boostMultiplier = _gachaTable.Settings.BoostMultiplier;
            string poolName = "pickup";

            if (pickupItemIds.Count != 3)
            {
                throw new ArgumentException("픽업 아이템은 정확히 3개를 선택해야 합니다.");
            }

            // 픽업 아이템들이 레어리티 3인지 확인
            var pool = _gachaTable.GetGachaPool(poolName);
            foreach (var itemId in pickupItemIds)
            {
                var item = pool.FirstOrDefault(x => x.Id == itemId);
                if (item == null)
                {
                    throw new ArgumentException($"픽업 아이템 ID {itemId}를 찾을 수 없습니다.");
                }
                if (item.Rarity != Rarity.SSR)
                {
                    throw new ArgumentException($"픽업 아이템 '{item.Name}'은(는) 레어리티 3이 아닙니다. 레어리티 3 아이템만 픽업할 수 있습니다.");
                }
            }

            // 1-9회용 WeightedRandom 생성 (pickup 풀)
            var normalPool = _gachaTable.GetGachaPool(poolName);
            var normalWeightedRandom = new WeightedRandom<GachaItem>();
            foreach (var item in normalPool)
            {
                int weight = pickupItemIds.Contains(item.Id)
                    ? item.BaseWeight + boostMultiplier
                    : item.BaseWeight;
                normalWeightedRandom.Push(weight, item);
            }

            // 10회용 WeightedRandom 생성 (pickup-confirm 풀)
            var confirmPool = _gachaTable.GetGachaPool("pickup-confirm");
            var confirmWeightedRandom = new WeightedRandom<GachaItem>();
            foreach (var item in confirmPool)
            {
                int weight = pickupItemIds.Contains(item.Id)
                    ? item.BaseWeight + boostMultiplier
                    : item.BaseWeight;
                confirmWeightedRandom.Push(weight, item);
            }

            var pulledItems = new List<GachaItem>();

            for (int i = 0; i < pullCount; i++)
            {
                // 10회째(마지막)는 pickup-confirm 풀 사용, 나머지는 pickup 풀 사용
                var weightedRandom = (i == pullCount - 1) ? confirmWeightedRandom : normalWeightedRandom;
                var selectedItem = weightedRandom.Random();
                pulledItems.Add(selectedItem);
            }

            return CreateGachaResult(pulledItems, pickupItemIds, isPickup: true);
        }

        /// <summary>
        /// 뽑은 아이템 리스트를 GachaResult로 변환
        /// </summary>
        private GachaResult CreateGachaResult(List<GachaItem> pulledItems, List<int> pickupItemIds, bool isPickup = false)
        {
            var result = new GachaResult();

            // Result: 뽑은 모든 아이템 (10개 그대로)
            result.Result = pulledItems.Select(item => new GachaResultItem
            {
                Id = item.Id,
                Name = item.Name,
                Rarity = item.Rarity,
                PickUp = pickupItemIds.Contains(item.Id)  // 픽업 캐릭터가 당첨되었을 때 true
            }).ToList();

            return result;
        }
    }
}
