using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class MaintenanceController : SecureControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int vehicleId, [FromQuery] ListeSorgusu sorgu)
        {
            if (sorgu != null && sorgu.ZarfIster)
            {
                var sayfali = await _maintenanceService.GetSayfaAsync(CurrentUserId, vehicleId, sorgu);

                if (!sayfali.Success)
                    return sayfali.Message == Messages.SiralamaGecersiz ? BadRequest(sayfali) : NotFound(sayfali);

                return Ok(sayfali);
            }

            var result = await _maintenanceService.GetListAsync(CurrentUserId, vehicleId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [ServiceFilter(typeof(Garajim.API.Startup.TekrarKorumasi))]

        [HttpPost]
        public async Task<IActionResult> Add(MaintenanceCreateDto dto)
        {
            var result = await _maintenanceService.AddAsync(CurrentUserId, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, MaintenanceUpdateDto dto)
        {
            var result = await _maintenanceService.UpdateAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return result.Message == Messages.RecordNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _maintenanceService.DeleteAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }
}
