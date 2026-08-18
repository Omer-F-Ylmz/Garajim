using Garajim.Business.Abstract;
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
        public async Task<IActionResult> GetList([FromQuery] int vehicleId)
        {
            var result = await _maintenanceService.GetListAsync(CurrentUserId, vehicleId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add(MaintenanceCreateDto dto)
        {
            var result = await _maintenanceService.AddAsync(CurrentUserId, dto);
            if (!result.Success)
                return BadRequest(result);
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
