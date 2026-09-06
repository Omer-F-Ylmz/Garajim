using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class FuelController : SecureControllerBase
    {
        private readonly IFuelService _fuelService;

        public FuelController(IFuelService fuelService)
        {
            _fuelService = fuelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int vehicleId, [FromQuery] ListeSorgusu sorgu)
        {
            if (sorgu != null && sorgu.ZarfIster)
            {
                var sayfali = await _fuelService.GetSayfaAsync(CurrentUserId, vehicleId, sorgu);

                if (!sayfali.Success)
                    return sayfali.Message == Messages.SiralamaGecersiz ? BadRequest(sayfali) : NotFound(sayfali);

                return Ok(sayfali);
            }

            var result = await _fuelService.GetListAsync(CurrentUserId, vehicleId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [ServiceFilter(typeof(Garajim.API.Startup.TekrarKorumasi))]

        [HttpPost]
        public async Task<IActionResult> Add(FuelCreateDto dto)
        {
            var result = await _fuelService.AddAsync(CurrentUserId, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, FuelUpdateDto dto)
        {
            var result = await _fuelService.UpdateAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return result.Message == Messages.RecordNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _fuelService.DeleteAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }
}
