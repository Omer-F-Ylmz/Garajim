using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IAssignmentService
    {
        Task<IDataResult<List<AssignmentDto>>> GetHistoryAsync(int currentUserId, int vehicleId);
        Task<IDataResult<AssignmentDto>> AssignAsync(int currentUserId, AssignmentCreateDto dto);
        Task<IDataResult<AssignmentDto>> TransferAsync(int currentUserId, AssignmentCreateDto dto);
        Task<IResult> EndAsync(int currentUserId, AssignmentEndDto dto);
    }
}
