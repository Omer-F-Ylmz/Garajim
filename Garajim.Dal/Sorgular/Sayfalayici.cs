using System.Linq.Expressions;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Sorgular
{
    public static class Sayfalayici
    {
        public static async Task<SayfaliSonuc<T>> UygulaAsync<T>(
            IQueryable<T> sorgulama,
            ListeSorgusu sorgu,
            SiralamaSonucu siralama,
            Expression<Func<T, DateTime>> tarihAlani,
            Expression<Func<T, bool>> metinSuzgeci,
            Func<IQueryable<T>, SiralamaSonucu, IQueryable<T>> sirala)
        {
            if (tarihAlani != null && sorgu.Baslangic != null)
            {
                sorgulama = sorgulama.Where(TarihSuzgeci(tarihAlani, sorgu.Baslangic.Value.Date, true));
            }

            if (tarihAlani != null && sorgu.Bitis != null)
            {
                sorgulama = sorgulama.Where(TarihSuzgeci(tarihAlani, sorgu.Bitis.Value.Date, false));
            }

            if (metinSuzgeci != null)
            {
                sorgulama = sorgulama.Where(metinSuzgeci);
            }

            var toplam = await sorgulama.CountAsync();
            var sayfaNo = sorgu.GecerliSayfa();
            var boyut = sorgu.GecerliBoyut();

            var kayitlar = await sirala(sorgulama, siralama)
                .Skip((sayfaNo - 1) * boyut)
                .Take(boyut)
                .ToListAsync();

            return new SayfaliSonuc<T>(kayitlar, toplam, sayfaNo, boyut);
        }

        private static Expression<Func<T, bool>> TarihSuzgeci<T>(Expression<Func<T, DateTime>> alan, DateTime deger, bool buyukEsit)
        {
            var sabit = Expression.Constant(deger, typeof(DateTime));

            var karsilastirma = buyukEsit
                ? Expression.GreaterThanOrEqual(alan.Body, sabit)
                : Expression.LessThanOrEqual(alan.Body, sabit);

            return Expression.Lambda<Func<T, bool>>(karsilastirma, alan.Parameters[0]);
        }
    }
}
