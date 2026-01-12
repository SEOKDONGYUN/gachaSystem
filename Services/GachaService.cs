using GachaSystem.Models;
using System.Text.Json;
using System.Text.Encodings.Web;
using Services;

namespace GachaSystem.Services
{
    public class GachaService : IGachaService
    {
        private readonly GachaTable _gachaTable;
        private readonly Random _random;
        private readonly ILogger<GachaService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GachaService(ILogger<GachaService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _random = new Random();
            _gachaTable = GachaTable.Instance;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // ========== WeightedRandom 사용 ==========

        /// <summary>
        /// 일반 가챠 (10회)
        /// 10회째는 confirm 풀을 사용하여 레어리티 2 이상 확정
        /// </summary>
        public GachaResult PullGacha()
        {
            var clientIp = GetClientIpAddress();
            _logger.LogInformation($"[일반 가챠] 요청 시작 - IP: {clientIp}");

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

            var result = CreateGachaResult(pulledItems, new List<int>(), isPickup: false);

            // 결과 로깅
            var resultJson = SerializeResult(result);
            _logger.LogInformation($"[일반 가챠] 완료 - IP: {clientIp}, 결과: {resultJson}");

            return result;
        }

        /// <summary>
        /// 픽업 가챠 (10회)
        /// 특정 아이템의 가중치에 boostMultiplier를 더해 확률 상승
        /// 10회째는 pickup-confirm 풀을 사용하여 레어리티 2 이상 확정
        /// 레어리티 3(SSR) 아이템 3개 픽업 필수
        /// </summary>
        public GachaResult PullPickupGacha(List<int> pickupItemIds)
        {
            var clientIp = GetClientIpAddress();
            _logger.LogInformation($"[픽업 가챠] 요청 시작 - IP: {clientIp}, 픽업 아이템: [{string.Join(", ", pickupItemIds)}]");

            int pullCount = 10;
            int boostMultiplier = _gachaTable.Settings.BoostMultiplier;
            string poolName = "pickup";

            if (pickupItemIds.Count != 3)
            {
                _logger.LogWarning($"[픽업 가챠] 실패 - IP: {clientIp}, 사유: 픽업 아이템 개수 오류 ({pickupItemIds.Count}개)");
                throw new ArgumentException("픽업 아이템은 정확히 3개를 선택해야 합니다.");
            }

            // 픽업 아이템들이 레어리티 3인지 확인
            var pool = _gachaTable.GetGachaPool(poolName);
            foreach (var itemId in pickupItemIds)
            {
                var item = pool.FirstOrDefault(x => x.Id == itemId);
                if (item == null)
                {
                    _logger.LogWarning($"[픽업 가챠] 실패 - IP: {clientIp}, 사유: 아이템 ID {itemId} 없음");
                    throw new ArgumentException($"픽업 아이템 ID {itemId}를 찾을 수 없습니다.");
                }
                if (item.Rarity != Rarity.SSR)
                {
                    _logger.LogWarning($"[픽업 가챠] 실패 - IP: {clientIp}, 사유: 아이템 '{item.Name}'은 레어리티 {(int)item.Rarity}");
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

            var result = CreateGachaResult(pulledItems, pickupItemIds, isPickup: true);

            // 결과 로깅
            var resultJson = SerializeResult(result);
            _logger.LogInformation($"[픽업 가챠] 완료 - IP: {clientIp}, 결과: {resultJson}");

            return result;
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

        /// <summary>
        /// GachaResult를 한글이 포함된 JSON 문자열로 직렬화
        /// </summary>
        private string SerializeResult(GachaResult result)
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        /// <summary>
        /// 클라이언트 IP 주소 가져오기
        /// </summary>
        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return "Unknown";
            }

            // X-Forwarded-For 헤더 확인 (프록시 뒤에 있을 경우)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // RemoteIpAddress 사용
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
