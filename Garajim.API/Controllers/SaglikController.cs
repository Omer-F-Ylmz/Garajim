using System.Diagnostics;
using Garajim.API.Startup;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Entity.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class SaglikController : SecureControllerBase
    {
        [HttpGet("ping")]
        [AllowAnonymous]
        [EnableRateLimiting(KarneController.RateLimitPolicy)]
        public IActionResult Ping()
        {
            return Content("ok", "text/plain");
        }

        [HttpGet]
        public IActionResult Durum()
        {
            if (CurrentRole == CompanyRole.Driver.ToString())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ErrorResult(Messages.AuthorizationDenied));
            }

            return Ok(new SuccessDataResult<object>(BellekDurumu.Oku()));
        }
    }
}
