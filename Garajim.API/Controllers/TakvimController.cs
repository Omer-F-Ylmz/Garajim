using Garajim.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [ApiController]
    [Route("api/takvim")]
    [AllowAnonymous]
    [EnableRateLimiting(KarneController.RateLimitPolicy)]
    public class TakvimAnonimController : ControllerBase
    {
        private readonly ITakvimService _takvimService;

        public TakvimAnonimController(ITakvimService takvimService)
        {
            _takvimService = takvimService;
        }

        [HttpGet("{token}.ics")]
        public async Task<IActionResult> Ics(string token)
        {
            var result = await _takvimService.IcsAsync(token);
            if (!result.Success)
                return NotFound(result);

            return Content(result.Data, "text/calendar; charset=utf-8");
        }
    }

    [Route("api/Takvim")]
    public class TakvimController : SecureControllerBase
    {
        private readonly ITakvimService _takvimService;

        public TakvimController(ITakvimService takvimService)
        {
            _takvimService = takvimService;
        }

        [HttpPost("abonelik")]
        public async Task<IActionResult> AbonelikOlustur()
        {
            var result = await _takvimService.AbonelikOlusturAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("abonelik")]
        public async Task<IActionResult> AbonelikKapat()
        {
            var result = await _takvimService.AbonelikKapatAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }
}
