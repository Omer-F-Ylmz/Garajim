using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Dal.Abstract
{
    public interface IReceiptDraftDal : IEntityRepository<ReceiptDraft>
    {
        Task<int> GetMonthlyCountAsync(DateTime ayBasi);
        Task<int> BekleyenSayisiAsync();
        Task<SayfaliSonuc<ReceiptDraft>> SayfaAsync(ReceiptDraftStatus? durum, int? surucuKullaniciId, List<int> erisilebilirAracIdler, ListeSorgusu sorgu, SiralamaSonucu siralama);
        Task<FisIstatistikSayilari> IstatistikAsync();
        Task<List<string>> ElleOnaylananDuzeltmeAlanlariAsync();
    }
}
