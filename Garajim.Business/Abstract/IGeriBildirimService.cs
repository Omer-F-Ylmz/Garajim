using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IGeriBildirimService
    {
        Task<IResult> EkleAsync(int userId, GeriBildirimCreateDto dto);

        Task<IDataResult<List<GeriBildirimDto>>> SonlariAsync(int limit);
    }
}
