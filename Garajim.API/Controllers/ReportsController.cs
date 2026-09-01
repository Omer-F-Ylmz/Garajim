using Garajim.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class ReportsController : SecureControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int vehicleId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var result = await _reportService.GetSummaryAsync(CurrentUserId, vehicleId, start, end);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthly([FromQuery] int vehicleId)
        {
            var result = await _reportService.GetMonthlyAsync(CurrentUserId, vehicleId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("fuel-stats")]
        public async Task<IActionResult> GetFuelStats([FromQuery] int vehicleId)
        {
            var result = await _reportService.GetFuelStatsAsync(CurrentUserId, vehicleId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _reportService.GetDashboardAsync(CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("filo-maliyet")]
        public async Task<IActionResult> GetFiloMaliyet([FromQuery] DateTime baslangic, [FromQuery] DateTime bitis)
        {
            var result = await _reportService.GetFiloMaliyetAsync(CurrentUserId, baslangic, bitis);
            if (!result.Success)
            {
                if (result.Message == Garajim.Business.Constants.Messages.AuthorizationDenied)
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

    }
}
