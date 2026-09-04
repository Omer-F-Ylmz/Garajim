using Microsoft.EntityFrameworkCore;
using Garajim.Business.Jobs;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class UstaSaklamaJobTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private UstaOzetJob OzetJob()
        {
            return new UstaOzetJob(_db.CompanyDal, _db.UstaMesajDal, _db.UstaSohbetDal, _db.VehicleDal, _db.PartDal, _db.UstaCozumOzetiDal, _db.UserDal, _db.UnitOfWork, _db.Tenant, _db.Yapilandirma);
        }

        private UstaSaklamaJob SaklamaJob()
        {
            return new UstaSaklamaJob(_db.CompanyDal, _db.UstaSohbetDal, _db.UstaMesajDal, OzetJob(), _db.Tenant);
        }

        private int SohbetEkle(string eposta, string plaka, int ayOnce, bool cozumBagla = false)
        {
            var user = _db.KullaniciEkle(eposta);
            var arac = _db.AracEkle(user.Id, plaka);
            arac.Brand = "Renault";
            arac.Model = "Clio";
            _db.Context.SaveChanges();

            var tarih = DateTime.UtcNow.AddMonths(-ayOnce);

            int? bakimId = null;
            if (cozumBagla)
            {
                var bakim = new MaintenanceRecord
                {
                    CompanyId = arac.CompanyId,
                    VehicleId = arac.Id,
                    Type = MaintenanceType.FrenBakimi,
                    Date = tarih,
                    Km = arac.CurrentKm,
                    Cost = 2500m,
                    ServiceName = "Servis"
                };
                _db.Context.MaintenanceRecords.Add(bakim);
                _db.Context.SaveChanges();

                _db.Context.MaintenanceParts.Add(new MaintenancePart
                {
                    CompanyId = arac.CompanyId,
                    MaintenanceRecordId = bakim.Id,
                    VehicleId = arac.Id,
                    ParcaTuru = ParcaTuru.FrenBalatasiOn,
                    Adet = 1
                });
                _db.Context.SaveChanges();
                bakimId = bakim.Id;
            }

            var sohbet = new UstaSohbet
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                UserId = user.Id,
                Baslik = plaka,
                OlusturmaTarihi = tarih
            };
            _db.Context.UstaSohbetleri.Add(sohbet);
            _db.Context.SaveChanges();

            _db.Context.UstaMesajlari.Add(new UstaMesaj
            {
                CompanyId = arac.CompanyId,
                SohbetId = sohbet.Id,
                Rol = UstaRol.Kullanici,
                Metin = "soru",
                GeriBildirim = UstaGeriBildirim.Yok,
                OlusturmaTarihi = tarih
            });
            _db.Context.UstaMesajlari.Add(new UstaMesaj
            {
                CompanyId = arac.CompanyId,
                SohbetId = sohbet.Id,
                Rol = UstaRol.Usta,
                Metin = "yanit",
                BilgiKategorisi = "fren",
                GeriBildirim = cozumBagla ? UstaGeriBildirim.Olumlu : UstaGeriBildirim.Yok,
                CozumBakimId = bakimId,
                OlusturmaTarihi = tarih
            });
            _db.Context.SaveChanges();

            return sohbet.Id;
        }

        [Fact]
        public async Task YirmiBesAylikSohbetSilinir()
        {
            var eski = SohbetEkle("a@garajim.local", "34SK0001", 25);

            await SaklamaJob().RunAsync();

            Assert.Empty(_db.Context.UstaSohbetleri.IgnoreQueryFilters().Where(s => s.Id == eski));
            Assert.Empty(_db.Context.UstaMesajlari.IgnoreQueryFilters().Where(m => m.SohbetId == eski));
        }

        [Fact]
        public async Task YirmiUcAylikSohbetKalir()
        {
            var yeni = SohbetEkle("b@garajim.local", "34SK0002", 23);

            await SaklamaJob().RunAsync();

            Assert.Single(_db.Context.UstaSohbetleri.IgnoreQueryFilters().Where(s => s.Id == yeni));
            Assert.Equal(2, _db.Context.UstaMesajlari.IgnoreQueryFilters().Count(m => m.SohbetId == yeni));
        }

        [Fact]
        public async Task SinirdakiYirmiDortAylikSohbetKalir()
        {
            var sinir = SohbetEkle("c@garajim.local", "34SK0003", 24);

            await SaklamaJob().RunAsync();

            Assert.Single(_db.Context.UstaSohbetleri.IgnoreQueryFilters().Where(s => s.Id == sinir));
        }

        [Fact]
        public async Task EskiSohbetinCozumOzetiSilinmedenOnceUretilir()
        {
            SohbetEkle("d@garajim.local", "34SK0004", 30, cozumBagla: true);

            await SaklamaJob().RunAsync();

            var ozet = Assert.Single(await _db.UstaCozumOzetiDal.GetTumuAsync());
            Assert.Equal("Renault", ozet.Marka);
            Assert.Equal("fren", ozet.BelirtiKategori);
            Assert.Equal("FrenBalatasiOn", ozet.ParcaTuru);
            Assert.Empty(_db.Context.UstaSohbetleri.IgnoreQueryFilters());
        }

        [Fact]
        public async Task CozumOzetiSilmeSonrasiKorunur()
        {
            SohbetEkle("e@garajim.local", "34SK0005", 30, cozumBagla: true);

            await SaklamaJob().RunAsync();
            var ilkSayi = (await _db.UstaCozumOzetiDal.GetTumuAsync()).Count;

            await SaklamaJob().RunAsync();

            Assert.Equal(1, ilkSayi);
            Assert.Equal(ilkSayi, (await _db.UstaCozumOzetiDal.GetTumuAsync()).Count);
        }

        [Fact]
        public async Task FarkliSirketlerinEskiKayitlariDaSilinir()
        {
            SohbetEkle("f@garajim.local", "34SK0006", 26);
            SohbetEkle("g@garajim.local", "34SK0007", 27);
            var kalan = SohbetEkle("h@garajim.local", "34SK0008", 5);

            await SaklamaJob().RunAsync();

            var sohbetler = _db.Context.UstaSohbetleri.IgnoreQueryFilters().ToList();
            Assert.Single(sohbetler);
            Assert.Equal(kalan, sohbetler[0].Id);
        }

        [Fact]
        public async Task SilinecekKayitYokkenSorunsuzCalisir()
        {
            SohbetEkle("i@garajim.local", "34SK0009", 3);

            var hata = await Record.ExceptionAsync(() => SaklamaJob().RunAsync());

            Assert.Null(hata);
            Assert.Single(_db.Context.UstaSohbetleri.IgnoreQueryFilters());
        }

        public void Dispose() => _db.Dispose();
    }
}
