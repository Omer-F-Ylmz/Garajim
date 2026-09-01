using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class EvrakController : SecureControllerBase
    {
        private readonly IEvrakService _evrakService;

        public EvrakController(IEvrakService evrakService)
        {
            _evrakService = evrakService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int? vehicleId)
        {
            var result = await _evrakService.GetListAsync(CurrentUserId, vehicleId);
            return Sonuc(result);
        }

        [HttpGet("takvim")]
        public async Task<IActionResult> Takvim([FromQuery] string ay)
        {
            var result = await _evrakService.GetTakvimAsync(CurrentUserId, ay);
            return Sonuc(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _evrakService.GetByIdAsync(CurrentUserId, id);
            return Sonuc(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add(EvrakCreateDto dto)
        {
            var result = await _evrakService.AddAsync(CurrentUserId, dto);
            return Sonuc(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, EvrakUpdateDto dto)
        {
            var result = await _evrakService.UpdateAsync(CurrentUserId, id, dto);
            return Sonuc(result);
        }

        [HttpPost("{id}/yenile")]
        public async Task<IActionResult> Yenile(int id)
        {
            var result = await _evrakService.YenileAsync(CurrentUserId, id);
            return Sonuc(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _evrakService.DeleteAsync(CurrentUserId, id);
            return Sonuc(result);
        }

        private IActionResult Sonuc(Core.Utilities.Results.IResult result)
        {
            if (result.Success)
                return Ok(result);

            if (result.Message == Messages.AuthorizationDenied)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.Message == Messages.EvrakNotFound
                || result.Message == Messages.VehicleNotFound
                || result.Message == Messages.UserNotFound)
                return NotFound(result);

            return BadRequest(result);
        }
    }
}
