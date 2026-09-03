using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IHesapService
    {
        Task<IResult> SilmeKoduGonderAsync(int userId);

        Task<IResult> SilmeyiPlanlaAsync(int userId, HesapSilDto dto);

        Task<IResult> SilmeyiIptalEtAsync(int userId);

        Task<IDataResult<HesapDurumDto>> DurumAsync(int userId);

        Task<IResult> UyeHesabiniSilAsync(int userId);
    }
}
