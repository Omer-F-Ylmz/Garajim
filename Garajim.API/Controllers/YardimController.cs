using Garajim.Business.Katalog;
using Garajim.Core.Utilities.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [EnableRateLimiting(KarneController.RateLimitPolicy)]
    [Route("api/[controller]")]
    public class YardimController : ControllerBase
    {
        private readonly IReadOnlyList<SssKaydi> _sss;
        private readonly IConfiguration _configuration;

        public YardimController(IReadOnlyList<SssKaydi> sss, IConfiguration configuration)
        {
            _sss = sss;
            _configuration = configuration;
        }

        [HttpGet("sss")]
        public IActionResult Sss()
        {
            return Ok(new SuccessDataResult<object>(new
            {
                destekEposta = (_configuration["App:DestekEposta"] ?? string.Empty).Trim(),
                sorular = _sss
            }));
        }
    }
}
