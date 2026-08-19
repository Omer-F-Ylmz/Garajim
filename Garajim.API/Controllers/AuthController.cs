using Garajim.Business.Abstract;
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
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
                return Unauthorized(result);
            return Ok(result);
        }
    }
}
