using Garajim.Business.Constants;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class Denetim2ListeSinirTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        [Fact]
        public async Task YolculukListesiBesYuzKayitlaSinirli()
        {
            var user = _db.KullaniciEkle("y@garajim.local");
            var arac = _db.AracEkle(user.Id, "34LM0001");

            for (var i = 0; i < QueryLimits.MaxListSize + 40; i++)
            {
                _db.Context.YolculukKayitlari.Add(new YolculukKaydi
                {
                    CompanyId = arac.CompanyId,
                    VehicleId = arac.Id,
                    UserId = user.Id,
                    Tarih = DateTime.UtcNow.Date.AddDays(-i),
                    BaslangicKm = 1000 + i,
                    BitisKm = 1100 + i,
                    MesafeKm = 100,
                    Amac = YolculukAmaci.Is,
                    OlusturmaTarihi = DateTime.UtcNow
                });
            }
            _db.Context.SaveChanges();

            var kayitlar = await _db.YolculukDal.GetListeAsync(
                new List<int> { arac.Id }, DateTime.MinValue, DateTime.MaxValue, QueryLimits.MaxListSize);

            Assert.Equal(QueryLimits.MaxListSize, kayitlar.Count);
        }

        [Fact]
        public async Task UstaSohbetListesiBesYuzKayitlaSinirli()
        {
            var user = _db.KullaniciEkle("u@garajim.local");
            var arac = _db.AracEkle(user.Id, "34LM0002");

            for (var i = 0; i < QueryLimits.MaxListSize + 25; i++)
            {
                _db.Context.UstaSohbetleri.Add(new UstaSohbet
                {
                    CompanyId = arac.CompanyId,
                    VehicleId = arac.Id,
                    UserId = user.Id,
                    Baslik = "sohbet " + i,
                    OlusturmaTarihi = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            _db.Context.SaveChanges();

            var sohbetler = await _db.UstaSohbetDal.GetListeAsync(arac.Id, null, QueryLimits.MaxListSize);

            Assert.Equal(QueryLimits.MaxListSize, sohbetler.Count);
        }

        [Fact]
        public async Task LastikListesiBesYuzKayitlaSinirli()
        {
            var user = _db.KullaniciEkle("l@garajim.local");
            var arac = _db.AracEkle(user.Id, "34LM0003");

            for (var i = 0; i < QueryLimits.MaxListSize + 10; i++)
            {
                _db.Context.LastikSetleri.Add(new LastikSeti
                {
                    CompanyId = arac.CompanyId,
                    VehicleId = arac.Id,
                    Ad = "set " + i,
                    Mevsim = LastikMevsimi.Yaz,
                    TakilmaTarihi = DateTime.UtcNow.Date.AddDays(-i),
                    TakilmaKm = 1000 + i,
                    SokulmeTarihi = DateTime.UtcNow.Date.AddDays(-i).AddDays(1),
                    SokulmeKm = 1100 + i,
                    ToplamKm = 100,
                    Takili = false,
                    OlusturmaTarihi = DateTime.UtcNow
                });
            }
            _db.Context.SaveChanges();

            var setler = await _db.LastikDal.GetListeAsync(arac.Id, QueryLimits.MaxListSize);

            Assert.Equal(QueryLimits.MaxListSize, setler.Count);
        }

        public void Dispose() => _db.Dispose();
    }
}
