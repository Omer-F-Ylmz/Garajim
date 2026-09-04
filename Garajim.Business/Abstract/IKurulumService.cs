using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IKurulumService
    {
        Task<IDataResult<KurulumDurumDto>> DurumAsync(int userId);

        Task<IResult> GizleAsync(int userId);
    }
}
