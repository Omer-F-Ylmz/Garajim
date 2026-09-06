using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Dal.Sorgular;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfReceiptDraftDal : EfEntityRepositoryBase<ReceiptDraft, GarajimDbContext>, IReceiptDraftDal
    {
        public EfReceiptDraftDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<int> BekleyenSayisiAsync()
        {
            return await Context.ReceiptDrafts
                .AsNoTracking()
                .CountAsync(d => d.Durum == ReceiptDraftStatus.Bekliyor);
        }

        public async Task<int> GetMonthlyCountAsync(DateTime ayBasi)
        {
            return await Context.ReceiptDrafts
                .AsNoTracking()
                .CountAsync(d => d.OlusturmaTarihi >= ayBasi);
        }
        public Task<SayfaliSonuc<ReceiptDraft>> SayfaAsync(ReceiptDraftStatus? durum, int? surucuKullaniciId, List<int> erisilebilirAracIdler, ListeSorgusu sorgu, SiralamaSonucu siralama)
        {
            var sorgulama = Context.ReceiptDrafts.AsNoTracking().AsQueryable();

            if (durum != null)
            {
                sorgulama = sorgulama.Where(d => d.Durum == durum);
            }

            if (surucuKullaniciId != null)
            {
                var idler = erisilebilirAracIdler ?? new List<int>();
                sorgulama = sorgulama.Where(d => d.YukleyenUserId == surucuKullaniciId
                    || (d.VehicleId != null && idler.Contains(d.VehicleId.Value)));
            }

            var metin = TurkceArama.Iceren<ReceiptDraft>(sorgu.GecerliQ(), d => d.OrijinalAd, d => d.Plaka);

            return Sayfalayici.UygulaAsync(sorgulama, sorgu, siralama, d => d.OlusturmaTarihi, metin, Sirala);
        }

        private static IQueryable<ReceiptDraft> Sirala(IQueryable<ReceiptDraft> sorgulama, SiralamaSonucu siralama)
        {
            return siralama.Alan switch
            {
                "tutar" => siralama.Artan
                    ? sorgulama.OrderBy(d => d.ToplamTutar).ThenBy(d => d.Id)
                    : sorgulama.OrderByDescending(d => d.ToplamTutar).ThenByDescending(d => d.Id),
                "guven" => siralama.Artan
                    ? sorgulama.OrderBy(d => d.GuvenSkoru).ThenBy(d => d.Id)
                    : sorgulama.OrderByDescending(d => d.GuvenSkoru).ThenByDescending(d => d.Id),
                _ => siralama.Artan
                    ? sorgulama.OrderBy(d => d.OlusturmaTarihi).ThenBy(d => d.Id)
                    : sorgulama.OrderByDescending(d => d.OlusturmaTarihi).ThenByDescending(d => d.Id)
            };
        }
    }
}

