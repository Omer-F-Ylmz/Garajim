using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IKarneService
    {
        Task<IDataResult<KarneLinkDto>> OlusturAsync(int userId, int vehicleId, KarneOlusturDto dto);
        Task<IResult> KapatAsync(int userId, int vehicleId);
        Task<IDataResult<KarneDto>> GoruntuleAsync(string token);
        Task<IDataResult<DocumentContentDto>> BelgeAsync(string token, int documentId);
        Task<IDataResult<AcilKartDto>> AcilKartAsync(string token);
        Task<IDataResult<KarneStatsDto>> StatsAsync(int userId);
    }
}
