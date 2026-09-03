using Garajim.Business.Jobs;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class UstaOzetJobTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private UstaOzetJob JobOlustur()
        {
            return new UstaOzetJob(
                _db.CompanyDal,
                _db.UstaMesajDal,
                _db.UstaSohbetDal,
                _db.VehicleDal,
                _db.PartDal,
                _db.UstaCozumOzetiDal,
                _db.UserDal,
                _db.UnitOfWork,
                _db.Tenant);
        }

        private (Vehicle Arac, int MesajId) SenaryoKur(string eposta, string plaka, string marka, string model, UstaGeriBildirim geri, string kategori, bool bakimBagla = true)
        {
            var user = _db.KullaniciEkle(eposta);
            var arac = _db.AracEkle(user.Id, plaka);
            arac.Brand = marka;
            arac.Model = model;
            arac.Motor = "1.5 dCi";
            _db.Context.SaveChanges();

            var bakim = new MaintenanceRecord
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                Type = MaintenanceType.FrenBakimi,
                Date = DateTime.UtcNow.Date.AddDays(-5),
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

            var sohbet = new UstaSohbet
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                UserId = user.Id,
                Baslik = plaka,
                OlusturmaTarihi = DateTime.UtcNow
            };
            _db.Context.UstaSohbetleri.Add(sohbet);
            _db.Context.SaveChanges();

            var mesaj = new UstaMesaj
            {
                CompanyId = arac.CompanyId,
                SohbetId = sohbet.Id,
                Rol = UstaRol.Usta,
                Metin = "yanit",
                KirmiziCizgi = false,
                BilgiKategorisi = kategori,
                GeriBildirim = geri,
                CozumBakimId = bakimBagla ? bakim.Id : null,
                OlusturmaTarihi = DateTime.UtcNow
            };
            _db.Context.UstaMesajlari.Add(mesaj);
            _db.Context.SaveChanges();

            return (arac, mesaj.Id);
        }

        [Fact]
        public async Task OlumluVeBakimBagliMesajOzeteDonusur()
        {
            SenaryoKur("a@garajim.local", "34OZ0001", "Renault", "Clio", UstaGeriBildirim.Olumlu, "Fren");

            await JobOlustur().RunAsync();

            var ozet = Assert.Single(await _db.UstaCozumOzetiDal.GetTumuAsync());
            Assert.Equal("Renault", ozet.Marka);
            Assert.Equal("Clio", ozet.Model);
            Assert.Equal("Fren", ozet.BelirtiKategori);
            Assert.Equal("FrenBalatasiOn", ozet.ParcaTuru);
            Assert.Equal(1, ozet.Sayi);
        }

        [Fact]
        public async Task OzetTablosundaSirketKimligiTasinmaz()
        {
            SenaryoKur("b@garajim.local", "34OZ0002", "Renault", "Clio", UstaGeriBildirim.Olumlu, "Fren");

            await JobOlustur().RunAsync();

            var ozetler = await _db.UstaCozumOzetiDal.GetTumuAsync();
            var alanlar = typeof(UstaCozumOzeti).GetProperties().Select(p => p.Name).ToList();

            Assert.DoesNotContain("CompanyId", alanlar);
            Assert.DoesNotContain("UserId", alanlar);
            Assert.DoesNotContain("VehicleId", alanlar);
            Assert.DoesNotContain("Plaka", alanlar);
            Assert.NotEmpty(ozetler);
        }

        [Fact]
        public async Task FarkliSirketlerAyniSatirdaBirikir()
        {
            SenaryoKur("c@garajim.local", "34OZ0003", "Renault", "Clio", UstaGeriBildirim.Olumlu, "Fren");
            SenaryoKur("d@garajim.local", "34OZ0004", "Renault", "Clio", UstaGeriBildirim.Olumlu, "Fren");

            await JobOlustur().RunAsync();

            var ozet = Assert.Single(await _db.UstaCozumOzetiDal.GetTumuAsync());
            Assert.Equal(2, ozet.Sayi);
        }

        [Fact]
        public async Task OlumsuzGeriBildirimOzeteGirmez()
        {
            SenaryoKur("e@garajim.local", "34OZ0005", "Renault", "Clio", UstaGeriBildirim.Olumsuz, "Fren");

            await JobOlustur().RunAsync();

            Assert.Empty(await _db.UstaCozumOzetiDal.GetTumuAsync());
        }

        [Fact]
        public async Task BakimBaginiOlmayanMesajOzeteGirmez()
        {
            SenaryoKur("f@garajim.local", "34OZ0006", "Renault", "Clio", UstaGeriBildirim.Olumlu, "Fren", bakimBagla: false);

            await JobOlustur().RunAsync();

            Assert.Empty(await _db.UstaCozumOzetiDal.GetTumuAsync());
        }

        [Fact]
        public async Task KategorisizMesajOzeteGirmez()
        {
            SenaryoKur("g@garajim.local", "34OZ0007", "Renault", "Clio", UstaGeriBildirim.Olumlu, null);

            await JobOlustur().RunAsync();

            Assert.Empty(await _db.UstaCozumOzetiDal.GetTumuAsync());
        }

        [Fact]
        public async Task TekrarCalistirmaSayiyiKatlamaz()
        {
            SenaryoKur("h@garajim.local", "34OZ0008", "Renault", "Clio", UstaGeriBildirim.Olumlu, "Fren");

            await JobOlustur().RunAsync();
            await JobOlustur().RunAsync();

            var ozet = Assert.Single(await _db.UstaCozumOzetiDal.GetTumuAsync());
            Assert.Equal(1, ozet.Sayi);
        }

        public void Dispose() => _db.Dispose();
    }
}
