using Microsoft.AspNetCore.Mvc;
using GachaSystem.Models;
using GachaSystem.Services;

namespace GachaSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GachaController : ControllerBase
    {
        private readonly IGachaService _gachaService;
        private readonly ILogger<GachaController> _logger;

        public GachaController(IGachaService gachaService, ILogger<GachaController> logger)
        {
            _gachaService = gachaService;
            _logger = logger;
        }

        /// <summary>
        /// 모든 가챠 아이템 목록 조회
        /// </summary>
        [HttpGet("items")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<GachaItem>> GetAllItems()
        {
            var items = _gachaService.GetAllItems();
            return Ok(items);
        }

        /// <summary>
        /// 특정 아이템 조회
        /// </summary>
        [HttpGet("items/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GachaItem> GetItemById(int id)
        {
            var item = _gachaService.GetItemById(id);
            if (item == null)
            {
                return NotFound(new { message = $"아이템 ID {id}를 찾을 수 없습니다." });
            }
            return Ok(item);
        }

        /// <summary>
        /// 통상 가챠 뽑기
        /// </summary>
        [HttpPost("pull/normal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<GachaResult> PullNormalGacha([FromBody] GachaRequest request)
        {
            if (request.PullCount < 1 || request.PullCount > 100)
            {
                return BadRequest(new { message = "뽑기 횟수는 1~100 사이여야 합니다." });
            }

            try
            {
                var result = _gachaService.PullNormalGacha(request.PullCount);
                _logger.LogInformation($"통상 가챠 {request.PullCount}회 실행");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "통상 가챠 실행 중 오류 발생");
                return StatusCode(500, new { message = "가챠 실행 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 픽업 가챠 뽑기 (최대 5개 아이템 픽업 가능)
        /// </summary>
        [HttpPost("pull/pickup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<GachaResult> PullPickupGacha([FromBody] PickupGachaRequest request)
        {
            if (request.PullCount < 1 || request.PullCount > 100)
            {
                return BadRequest(new { message = "뽑기 횟수는 1~100 사이여야 합니다." });
            }

            if (request.PickupItemIds == null || request.PickupItemIds.Count == 0)
            {
                return BadRequest(new { message = "픽업할 아이템을 최소 1개 이상 선택해야 합니다." });
            }

            if (request.PickupItemIds.Count > 5)
            {
                return BadRequest(new { message = "픽업 아이템은 최대 5개까지 선택할 수 있습니다." });
            }

            if (request.PickupBoostMultiplier < 1.0 || request.PickupBoostMultiplier > 10.0)
            {
                return BadRequest(new { message = "픽업 배율은 1.0~10.0 사이여야 합니다." });
            }

            try
            {
                var result = _gachaService.PullPickupGacha(
                    request.PullCount,
                    request.PickupItemIds,
                    request.PickupBoostMultiplier
                );

                _logger.LogInformation($"픽업 가챠 {request.PullCount}회 실행 (픽업 아이템: {string.Join(", ", request.PickupItemIds)})");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "픽업 가챠 실행 중 오류 발생");
                return StatusCode(500, new { message = "가챠 실행 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 가챠 확률 정보 조회
        /// </summary>
        [HttpGet("rates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetGachaRates()
        {
            var items = _gachaService.GetAllItems();
            var totalWeight = items.Sum(x => x.BaseWeight);

            var rates = items.GroupBy(x => x.Rarity)
                .Select(g => new
                {
                    Rarity = g.Key,
                    Probability = $"{(g.Sum(x => x.BaseWeight) / totalWeight * 100):F2}%",
                    Items = g.Select(item => new
                    {
                        item.Id,
                        item.Name,
                        IndividualProbability = $"{(item.BaseWeight / totalWeight * 100):F4}%"
                    })
                })
                .OrderByDescending(x => x.Rarity);

            return Ok(new
            {
                Message = "통상 가챠 확률 정보",
                TotalWeight = totalWeight,
                Rates = rates
            });
        }

        /// <summary>
        /// 픽업 가챠 확률 정보 조회
        /// </summary>
        [HttpPost("rates/pickup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult GetPickupGachaRates([FromBody] PickupRateRequest request)
        {
            if (request.PickupItemIds == null || request.PickupItemIds.Count == 0)
            {
                return BadRequest(new { message = "픽업할 아이템을 선택해야 합니다." });
            }

            if (request.PickupItemIds.Count > 5)
            {
                return BadRequest(new { message = "픽업 아이템은 최대 5개까지 선택할 수 있습니다." });
            }

            var items = _gachaService.GetAllItems();
            var boostMultiplier = request.PickupBoostMultiplier;

            var adjustedItems = items.Select(item => new
            {
                Item = item,
                AdjustedWeight = request.PickupItemIds.Contains(item.Id)
                    ? item.BaseWeight * boostMultiplier
                    : item.BaseWeight
            }).ToList();

            var totalWeight = adjustedItems.Sum(x => x.AdjustedWeight);

            var rates = adjustedItems.Select(x => new
            {
                x.Item.Id,
                x.Item.Name,
                x.Item.Rarity,
                IsPickup = request.PickupItemIds.Contains(x.Item.Id),
                BaseWeight = x.Item.BaseWeight,
                AdjustedWeight = x.AdjustedWeight,
                Probability = $"{(x.AdjustedWeight / totalWeight * 100):F4}%"
            })
            .OrderByDescending(x => x.IsPickup)
            .ThenByDescending(x => x.Rarity);

            return Ok(new
            {
                Message = "픽업 가챠 확률 정보",
                PickupItemIds = request.PickupItemIds,
                BoostMultiplier = $"{boostMultiplier}x",
                TotalWeight = totalWeight,
                Rates = rates
            });
        }
    }

    public class PickupRateRequest
    {
        public List<int> PickupItemIds { get; set; } = new List<int>();
        public double PickupBoostMultiplier { get; set; } = 2.0;
    }
}
