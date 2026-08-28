using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = CompanyRoles.OwnerOrManager)]
    public class AssignmentsController : SecureControllerBase
    {
        private readonly IAssignmentService _assignmentService;

        public AssignmentsController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory([FromQuery] int vehicleId)
        {
            var result = await _assignmentService.GetHistoryAsync(CurrentUserId, vehicleId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Assign(AssignmentCreateDto dto)
        {
            var result = await _assignmentService.AssignAsync(CurrentUserId, dto);
            if (!result.Success)
                return BulunamadiMi(result.Message) ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpPut("transfer")]
        public async Task<IActionResult> Transfer(AssignmentCreateDto dto)
        {
            var result = await _assignmentService.TransferAsync(CurrentUserId, dto);
            if (!result.Success)
                return BulunamadiMi(result.Message) ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpPut("end")]
        public async Task<IActionResult> End(AssignmentEndDto dto)
        {
            var result = await _assignmentService.EndAsync(CurrentUserId, dto);
            if (!result.Success)
                return BulunamadiMi(result.Message) ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        private static bool BulunamadiMi(string message)
        {
            return message == Messages.VehicleNotFound
                || message == Messages.UserNotFound
                || message == Messages.AssignmentNotFound;
        }
    }
}
