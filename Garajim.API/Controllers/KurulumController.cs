using Garajim.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = CompanyRoles.OwnerOrManager)]
    public class KurulumController : SecureControllerBase
    {
        private readonly IKurulumService _kurulumService;

        public KurulumController(IKurulumService kurulumService)
        {
            _kurulumService = kurulumService;
        }

        [HttpGet]
        public async Task<IActionResult> Durum()
        {
            var result = await _kurulumService.DurumAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost("gizle")]
        public async Task<IActionResult> Gizle()
        {
            var result = await _kurulumService.GizleAsync(CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
