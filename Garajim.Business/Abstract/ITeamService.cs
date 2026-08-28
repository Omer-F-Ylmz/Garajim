using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface ITeamService
    {
        Task<IDataResult<List<TeamMemberDto>>> GetListAsync(int currentUserId);
        Task<IDataResult<TeamMemberCreatedDto>> AddAsync(int currentUserId, TeamMemberCreateDto dto);
        Task<IResult> ChangeRoleAsync(int currentUserId, int targetUserId, TeamMemberRoleDto dto);
        Task<IResult> DeactivateAsync(int currentUserId, int targetUserId);
    }
}
