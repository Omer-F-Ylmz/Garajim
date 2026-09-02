using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class PlanController : SecureControllerBase
    {
        private readonly IPlanService _planService;

        public PlanController(IPlanService planService)
        {
            _planService = planService;
        }

        [HttpPost("yukseltme-talebi")]
        public async Task<IActionResult> YukseltmeTalebi(PlanYukseltmeTalebiDto dto)
        {
            var result = await _planService.YukseltmeTalebiAsync(CurrentUserId, dto);
            if (!result.Success)
            {
                if (result.Message == Messages.AuthorizationDenied)
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                if (result.Message == Messages.UserNotFound)
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
