using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IAiTokenDal : IEntityRepository<AiTokenSayaci>
    {
        Task<AiTokenSayaci> AyiAlAsync(int yil, int ay);

        Task EkleAsync(int yil, int ay, int giris, int cikis);

        Task KotaHatasiEkleAsync(int yil, int ay);

        Task<bool> BildirimiIsaretleAsync(int yil, int ay);
    }
}
