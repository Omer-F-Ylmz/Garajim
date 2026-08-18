using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IExpenseService
    {
        Task<IDataResult<List<ExpenseDto>>> GetListAsync(int userId, int vehicleId);
        Task<IDataResult<ExpenseDto>> AddAsync(int userId, ExpenseCreateDto dto);
        Task<IResult> DeleteAsync(int userId, int id);
    }
}
