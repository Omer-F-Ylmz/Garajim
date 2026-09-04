using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class GeriBildirimController : SecureControllerBase
    {
        private readonly IGeriBildirimService _geriBildirimService;

        public GeriBildirimController(IGeriBildirimService geriBildirimService)
        {
            _geriBildirimService = geriBildirimService;
        }

        [HttpPost]
        public async Task<IActionResult> Ekle(GeriBildirimCreateDto dto)
        {
            var result = await _geriBildirimService.EkleAsync(CurrentUserId, dto);
            if (!result.Success)
            {
                if (result.Message == Messages.GeriBildirimGunlukSinir)
                    return StatusCode(StatusCodes.Status429TooManyRequests, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Sonlar()
        {
            return Ok(await _geriBildirimService.SonlariAsync(GeriBildirimManager.ListeSiniri));
        }
    }
}
