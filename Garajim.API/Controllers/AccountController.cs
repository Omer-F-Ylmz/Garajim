using Garajim.Business.Abstract;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [EnableRateLimiting(AuthController.RateLimitPolicy)]
    [Route("api/[controller]")]
    public class AccountController : SecureControllerBase
    {
        private readonly IHesapService _hesapService;

        public AccountController(IHesapService hesapService)
        {
            _hesapService = hesapService;
        }

        [HttpGet("durum")]
        public async Task<IActionResult> Durum()
        {
            var result = await _hesapService.DurumAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost("sil-kod")]
        [Authorize(Roles = CompanyRoles.Owner)]
        public async Task<IActionResult> SilmeKodu()
        {
            return Ok(await _hesapService.SilmeKoduGonderAsync(CurrentUserId));
        }

        [HttpPost("sil")]
        [Authorize(Roles = CompanyRoles.Owner)]
        public async Task<IActionResult> Sil(HesapSilDto dto)
        {
            var result = await _hesapService.SilmeyiPlanlaAsync(CurrentUserId, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> UyeSil()
        {
            var result = await _hesapService.UyeHesabiniSilAsync(CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("sil-iptal")]
        [Authorize(Roles = CompanyRoles.Owner)]
        public async Task<IActionResult> SilIptal()
        {
            var result = await _hesapService.SilmeyiIptalEtAsync(CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
