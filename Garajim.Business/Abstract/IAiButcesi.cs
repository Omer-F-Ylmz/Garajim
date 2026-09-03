using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IAiButcesi
    {
        Task<AiButceDurumuDto> DurumAsync();

        Task<bool> AsildiMiAsync();

        Task KaydetAsync(int giris, int cikis);
    }
}
