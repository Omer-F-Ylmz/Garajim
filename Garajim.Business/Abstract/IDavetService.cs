using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IDavetService
    {
        Task<IDataResult<DavetDurumDto>> GetDurumAsync(int userId);
    }
}
