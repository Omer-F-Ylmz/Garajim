using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IHesapService
    {
        Task<IDataResult<ProfilDto>> ProfilAsync(int userId);

        Task<IResult> ProfilGuncelleAsync(int userId, ProfilGuncelleDto dto);

        Task<IResult> EpostaKoduGonderAsync(int userId, EpostaDegistirKodDto dto);

        Task<IResult> EpostaDegistirAsync(int userId, EpostaDegistirDto dto);

        Task<IResult> SilmeKoduGonderAsync(int userId);

        Task<IResult> SilmeyiPlanlaAsync(int userId, HesapSilDto dto);

        Task<IResult> SilmeyiIptalEtAsync(int userId);

        Task<IDataResult<HesapDurumDto>> DurumAsync(int userId);

        Task<IResult> UyeHesabiniSilAsync(int userId);
    }
}
