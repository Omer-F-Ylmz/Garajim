using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class DavetController : SecureControllerBase
    {
        private readonly IDavetService _davetService;

        public DavetController(IDavetService davetService)
        {
            _davetService = davetService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDurum()
        {
            var result = await _davetService.GetDurumAsync(CurrentUserId);
            if (!result.Success)
            {
                if (result.Message == Messages.AuthorizationDenied)
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                return NotFound(result);
            }
            return Ok(result);
        }
    }
}
