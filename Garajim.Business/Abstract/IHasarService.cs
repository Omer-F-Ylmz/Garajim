using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Abstract
{
    public interface IHasarService
    {
        Task<IDataResult<List<HasarDto>>> GetListAsync(int userId, int? vehicleId);
        Task<IDataResult<HasarDto>> GetAsync(int userId, int id);
        Task<IDataResult<HasarDto>> OlusturAsync(int userId, HasarOlusturDto dto);
        Task<IResult> GuncelleAsync(int userId, int id, HasarGuncelleDto dto);
        Task<IResult> SilAsync(int userId, int id);
        Task<IDataResult<HasarFotoDto>> FotoEkleAsync(int userId, int id, HasarFotoEtiketi etiket, string dosyaAdi, byte[] icerik);
        Task<IResult> FotoSilAsync(int userId, int id, int fotoId);
        Task<int> AcikDosyaSayisiAsync(List<int> vehicleIds);
        Task<List<HasarKarneSatiriDto>> KarneSatirlariAsync(int vehicleId);
    }
}
