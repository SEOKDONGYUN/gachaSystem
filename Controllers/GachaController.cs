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
        /// 사용 가능한 가챠 풀 목록 조회
        /// </summary>
        [HttpGet("pools")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<string>> GetAvailablePools()
        {
            var pools = _gachaService.GetAvailablePools();
            return Ok(new
            {
                Message = "사용 가능한 가챠 풀 목록",
                Pools = pools
            });
        }

        /// <summary>
        /// 일반 가챠 뽑기 (10회 고정)
        /// </summary>
        [HttpPost("pull")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<GachaResult> PullGacha()
        {
            try
            {
                var result = _gachaService.PullGacha();

                _logger.LogInformation("가챠 10회 실행 (풀: normal)");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "가챠 실행 중 오류 발생");
                return StatusCode(500, new { message = "가챠 실행 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 픽업 가챠 뽑기 (10회 고정, SSR 아이템만 픽업 가능)
        /// </summary>
        [HttpPost("pull/pickup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<GachaResult> PullPickupGacha([FromBody] PickupGachaRequest request)
        {
            if (request.PickupItemIds == null || request.PickupItemIds.Count == 0)
            {
                return BadRequest(new { message = "픽업할 아이템을 최소 1개 이상 선택해야 합니다." });
            }

            if (request.PickupItemIds.Count > 5)
            {
                return BadRequest(new { message = "픽업 아이템은 최대 5개까지 선택할 수 있습니다." });
            }

            try
            {
                var result = _gachaService.PullPickupGacha(request.PickupItemIds);

                _logger.LogInformation($"픽업 가챠 10회 실행 (풀: pickup, 픽업: {string.Join(", ", request.PickupItemIds)})");
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
    }
}
