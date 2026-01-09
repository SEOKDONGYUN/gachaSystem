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
        /// 픽업 가챠 뽑기 (10회 고정, SSR 아이템 3개 픽업 필수)
        /// </summary>
        [HttpPost("pickup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<GachaResult> PullPickupGacha([FromBody] PickupGachaRequest request)
        {
            if (request.PickupItemIds == null || request.PickupItemIds.Count != 3)
            {
                return BadRequest(new { message = "픽업 아이템은 정확히 3개를 선택해야 합니다." });
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
