using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(AuthController.RateLimitPolicy)]
    public class AuthController : ControllerBase
    {
        public const string RateLimitPolicy = "auth";
        public const string EmailDogrulanmadiKodu = "EMAIL_DOGRULANMADI";

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPost("dogrula")]
        public async Task<IActionResult> Dogrula(DogrulaDto dto)
        {
            var result = await _authService.DogrulaAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("kod-gonder")]
        public async Task<IActionResult> KodGonder(KodGonderDto dto)
        {
            return Ok(await _authService.KodGonderAsync(dto));
        }

        [HttpPost("sifre-sifirla-kod")]
        public async Task<IActionResult> SifreSifirlamaKodu(SifreSifirlamaKodDto dto)
        {
            return Ok(await _authService.SifreSifirlamaKoduAsync(dto));
        }

        [HttpPost("sifre-sifirla")]
        public async Task<IActionResult> SifreSifirla(SifreSifirlaDto dto)
        {
            var result = await _authService.SifreSifirlaAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
            {
                if (result.Message == Messages.EmailDogrulanmadi)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = result.Message, kod = EmailDogrulanmadiKodu });

                return Unauthorized(result);
            }
            return Ok(result);
        }
    }
}
