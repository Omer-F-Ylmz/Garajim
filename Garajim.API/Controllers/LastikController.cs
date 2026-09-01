using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class LastikController : SecureControllerBase
    {
        private readonly ILastikService _lastikService;

        public LastikController(ILastikService lastikService)
        {
            _lastikService = lastikService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDurum([FromQuery] int vehicleId)
        {
            return Sonuc(await _lastikService.GetDurumAsync(CurrentUserId, vehicleId));
        }

        [HttpPost]
        public async Task<IActionResult> Tak(LastikTakDto dto)
        {
            return Sonuc(await _lastikService.TakAsync(CurrentUserId, dto));
        }

        [HttpPut("{id}/sok")]
        public async Task<IActionResult> Sok(int id, LastikSokDto dto)
        {
            return Sonuc(await _lastikService.SokAsync(CurrentUserId, id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Sonuc(await _lastikService.DeleteAsync(CurrentUserId, id));
        }

        private IActionResult Sonuc(Core.Utilities.Results.IResult result)
        {
            if (result.Success)
                return Ok(result);

            if (result.Message == Messages.AuthorizationDenied)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.Message == Messages.VehicleNotFound || result.Message == Messages.UserNotFound || result.Message == Messages.LastikBulunamadi)
                return NotFound(result);

            return BadRequest(result);
        }
    }
}
