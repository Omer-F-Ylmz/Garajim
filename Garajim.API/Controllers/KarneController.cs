using Garajim.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [ApiController]
    [Route("api/karne")]
    [AllowAnonymous]
    [EnableRateLimiting(KarneController.RateLimitPolicy)]
    public class KarneController : ControllerBase
    {
        public const string RateLimitPolicy = "karne";

        private readonly IKarneService _karneService;

        public KarneController(IKarneService karneService)
        {
            _karneService = karneService;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> Goruntule(string token)
        {
            var result = await _karneService.GoruntuleAsync(token);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("{token}/acil")]
        public async Task<IActionResult> AcilKart(string token)
        {
            var result = await _karneService.AcilKartAsync(token);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("{token}/belge/{documentId}")]
        public async Task<IActionResult> Belge(string token, int documentId)
        {
            var result = await _karneService.BelgeAsync(token, documentId);
            if (!result.Success)
                return NotFound(result);
            return File(result.Data.Content, result.Data.ContentType, result.Data.OriginalName);
        }
    }
}
