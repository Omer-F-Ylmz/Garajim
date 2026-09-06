using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class YolculukController : SecureControllerBase
    {
        private readonly IYolculukService _yolculukService;

        public YolculukController(IYolculukService yolculukService)
        {
            _yolculukService = yolculukService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int? vehicleId, [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, [FromQuery] ListeSorgusu sorgu)
        {
            if (sorgu != null && sorgu.ZarfIster)
            {
                var sayfali = await _yolculukService.GetSayfaAsync(CurrentUserId, vehicleId, sorgu);

                if (!sayfali.Success)
                    return sayfali.Message == Messages.SiralamaGecersiz ? BadRequest(sayfali) : NotFound(sayfali);

                return Ok(sayfali);
            }

            return Sonuc(await _yolculukService.GetListAsync(CurrentUserId, vehicleId, baslangic, bitis));
        }

        [HttpGet("ozet")]
        public async Task<IActionResult> GetOzet([FromQuery] int? vehicleId, [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis)
        {
            return Sonuc(await _yolculukService.GetOzetAsync(CurrentUserId, vehicleId, baslangic, bitis));
        }

        [ServiceFilter(typeof(Garajim.API.Startup.TekrarKorumasi))]

        [HttpPost]
        public async Task<IActionResult> Add(YolculukCreateDto dto)
        {
            return Sonuc(await _yolculukService.AddAsync(CurrentUserId, dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, YolculukUpdateDto dto)
        {
            return Sonuc(await _yolculukService.UpdateAsync(CurrentUserId, id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Sonuc(await _yolculukService.DeleteAsync(CurrentUserId, id));
        }

        private IActionResult Sonuc(Core.Utilities.Results.IResult result)
        {
            if (result.Success)
                return Ok(result);

            if (result.Message == Messages.AuthorizationDenied)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.Message == Messages.VehicleNotFound || result.Message == Messages.UserNotFound || result.Message == Messages.YolculukBulunamadi)
                return NotFound(result);

            return BadRequest(result);
        }
    }
}
