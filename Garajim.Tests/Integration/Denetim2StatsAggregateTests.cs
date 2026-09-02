using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class Denetim2StatsAggregateTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        [Fact]
        public async Task IstatistikSqlTarafindaDogruHesaplanir()
        {
            var user = _db.KullaniciEkle("stat@garajim.local");
            var arac = _db.AracEkle(user.Id, "34ST0001");

            var sohbet = new UstaSohbet
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                UserId = user.Id,
                Baslik = "34ST0001",
                OlusturmaTarihi = DateTime.UtcNow
            };
            _db.Context.UstaSohbetleri.Add(sohbet);
            _db.Context.SaveChanges();

            for (var i = 0; i < 200; i++)
            {
                _db.Context.UstaMesajlari.Add(new UstaMesaj
                {
                    CompanyId = arac.CompanyId,
                    SohbetId = sohbet.Id,
                    Rol = UstaRol.Usta,
                    Metin = "yanit " + i,
                    KirmiziCizgi = i % 4 == 0,
                    GeriBildirim = i % 2 == 0 ? UstaGeriBildirim.Olumlu : UstaGeriBildirim.Yok,
                    CozumBakimId = i % 10 == 0 ? 1 : null,
                    TokenGiris = 1000,
                    TokenCikis = 200,
                    SureMs = 50,
                    OlusturmaTarihi = DateTime.UtcNow
                });
            }

            _db.Context.UstaMesajlari.Add(new UstaMesaj
            {
                CompanyId = arac.CompanyId,
                SohbetId = sohbet.Id,
                Rol = UstaRol.Kullanici,
                Metin = "soru",
                GeriBildirim = UstaGeriBildirim.Yok,
                OlusturmaTarihi = DateTime.UtcNow
            });
            _db.Context.SaveChanges();

            var ozet = await _db.UstaMesajDal.IstatistikAsync();

            Assert.Equal(200, ozet.Toplam);
            Assert.Equal(100, ozet.Puanlanan);
            Assert.Equal(100, ozet.Olumlu);
            Assert.Equal(50, ozet.KirmiziCizgi);
            Assert.Equal(20, ozet.CozumBagli);
            Assert.Equal(200000L, ozet.TokenGiris);
            Assert.Equal(40000L, ozet.TokenCikis);
            Assert.Equal(10000L, ozet.SureMs);
        }

        [Fact]
        public async Task KayitYokkenIstatistikSifirDoner()
        {
            _db.KullaniciEkle("bos@garajim.local");

            var ozet = await _db.UstaMesajDal.IstatistikAsync();

            Assert.Equal(0, ozet.Toplam);
            Assert.Equal(0L, ozet.TokenGiris);
        }

        public void Dispose() => _db.Dispose();
    }
}
