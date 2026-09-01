using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = CompanyRoles.Owner)]
    public class TeamController : SecureControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpGet]
        [Authorize(Roles = CompanyRoles.OwnerOrManager)]
        public async Task<IActionResult> GetList()
        {
            var result = await _teamService.GetListAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add(TeamMemberCreateDto dto)
        {
            var result = await _teamService.AddAsync(CurrentUserId, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> ChangeRole(int id, TeamMemberRoleDto dto)
        {
            var result = await _teamService.ChangeRoleAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return result.Message == Messages.UserNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _teamService.DeactivateAsync(CurrentUserId, id);
            if (!result.Success)
                return result.Message == Messages.UserNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }
    }

    [Route("api/Team")]
    [Authorize(Roles = CompanyRoles.OwnerOrManager)]
    public class TeamDocumentsController : SecureControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamDocumentsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpGet("belgeler")]
        public async Task<IActionResult> GetBelgeler()
        {
            var result = await _teamService.GetBelgelerAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }
}
