using Garajim.Business.Abstract;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class VehiclesController : SecureControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _vehicleService.GetAllAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _vehicleService.GetByIdAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add(VehicleCreateDto dto)
        {
            var result = await _vehicleService.AddAsync(CurrentUserId, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, VehicleUpdateDto dto)
        {
            var result = await _vehicleService.UpdateAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _vehicleService.DeleteAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }
}
