using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IUstaService
    {
        string GuncelOnaySurumu();
        Task<IDataResult<UstaOnayDurumDto>> OnayDurumuAsync(int userId);
        Task<IResult> OnayVerAsync(int userId, UstaOnayVerDto dto);
        Task<IDataResult<UstaSohbetDto>> SohbetOlusturAsync(int userId, UstaSohbetOlusturDto dto);
        Task<IDataResult<UstaMesajSonucDto>> MesajGonderAsync(int userId, int sohbetId, UstaMesajGonderDto dto, CancellationToken ct);
        Task<IDataResult<List<UstaSohbetDto>>> SohbetListesiAsync(int userId, int? vehicleId);
        Task<IDataResult<UstaSohbetDto>> SohbetAsync(int userId, int sohbetId);
        Task<IResult> SohbetSilAsync(int userId, int sohbetId);
        Task<IResult> GeriBildirimAsync(int userId, int mesajId, UstaGeriBildirimDto dto);
        Task<IDataResult<List<UstaBakimSecenegiDto>>> CozumBakimSecenekleriAsync(int userId, int sohbetId);
        Task<IDataResult<UstaStatsDto>> StatsAsync(int userId);
    }
}
