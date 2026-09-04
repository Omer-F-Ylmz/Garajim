using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IOrnekAracService
    {
        Task<IDataResult<VehicleDto>> OlusturAsync(int userId);

        Task<IResult> SilAsync(int userId);
    }
}
