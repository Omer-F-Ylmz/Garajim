using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IPlanService
    {
        Task<IResult> YukseltmeTalebiAsync(int userId, PlanYukseltmeTalebiDto dto);
    }
}
