using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class DemoSeedZenginlestirmeTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public void Dispose()
        {
            _db.Dispose();
        }

        private async Task<(int Evrak, int Lastik, int Parca, int Hasar, int Deger, int Yolculuk, int Zimmet)> SayimAsync()
        {
            return (
                await _db.Context.EvrakKayitlari.IgnoreQueryFilters().CountAsync(),
                await _db.Context.LastikSetleri.IgnoreQueryFilters().CountAsync(),
                await _db.Context.MaintenanceParts.IgnoreQueryFilters().CountAsync(),
                await _db.Context.HasarDosyalari.IgnoreQueryFilters().CountAsync(),
                await _db.Context.AracDegerleri.IgnoreQueryFilters().CountAsync(),
                await _db.Context.YolculukKayitlari.IgnoreQueryFilters().CountAsync(),
                await _db.Context.VehicleAssignments.IgnoreQueryFilters().CountAsync());
        }

        [Fact]
        public async Task UcAcilisEkleSonraIkiKezNoOp()
        {
            var birinci = await _db.DemoSeeder().RunAsync();
            var birinciSayim = await SayimAsync();

            var ikinci = await _db.DemoSeeder().RunAsync();
            var ikinciSayim = await SayimAsync();

            var ucuncu = await _db.DemoSeeder().RunAsync();
            var ucuncuSayim = await SayimAsync();

            Assert.True(birinci);
            Assert.False(ikinci);
            Assert.False(ucuncu);

            Assert.Equal(birinciSayim, ikinciSayim);
            Assert.Equal(birinciSayim, ucuncuSayim);
        }

        [Fact]
        public async Task IlkAcilistaBeklenenKayitlarOlusur()
        {
            await _db.DemoSeeder().RunAsync();
            var sayim = await SayimAsync();

            Assert.Equal(2, sayim.Evrak);
            Assert.Equal(2, sayim.Lastik);
            Assert.Equal(3, sayim.Parca);
            Assert.Equal(1, sayim.Hasar);
            Assert.Equal(2, sayim.Deger);
            Assert.Equal(1, sayim.Yolculuk);
        }

        [Fact]
        public async Task EvraklardanBiriYirmiGunIcindeBiter()
        {
            await _db.DemoSeeder().RunAsync();
            var bugun = DateTime.UtcNow.Date;

            var evraklar = await _db.Context.EvrakKayitlari.IgnoreQueryFilters().ToListAsync();
            var yaklasan = evraklar.Where(e => e.BitisTarihi >= bugun && e.BitisTarihi <= bugun.AddDays(20)).ToList();

            Assert.Single(yaklasan);
            Assert.Equal(EvrakTuru.Muayene, yaklasan[0].EvrakTuru);
            Assert.Contains(evraklar, e => e.EvrakTuru == EvrakTuru.TrafikSigortasi && e.BitisTarihi > bugun.AddDays(20));
            Assert.All(evraklar, e => Assert.True(e.Aktif));
        }

        [Fact]
        public async Task BirKisBirYazSetiVarYalnizBiriTakili()
        {
            await _db.DemoSeeder().RunAsync();

            var setler = await _db.Context.LastikSetleri.IgnoreQueryFilters().ToListAsync();

            Assert.Contains(setler, s => s.Mevsim == LastikMevsimi.Kis);
            Assert.Contains(setler, s => s.Mevsim == LastikMevsimi.Yaz);
            Assert.Single(setler.Where(s => s.Takili));
            Assert.Equal(LastikMevsimi.Yaz, setler.Single(s => s.Takili).Mevsim);

            var sokulen = setler.Single(s => !s.Takili);
            Assert.Equal(sokulen.SokulmeKm - sokulen.TakilmaKm, sokulen.ToplamKm);
        }

        [Fact]
        public async Task HasarDosyasiKapanmisVeFotografsiz()
        {
            await _db.DemoSeeder().RunAsync();

            var dosya = await _db.Context.HasarDosyalari.IgnoreQueryFilters().SingleAsync();
            var fotoSayisi = await _db.Context.HasarFotograflari.IgnoreQueryFilters().CountAsync();

            Assert.Equal(HasarDurumu.Kapandi, dosya.Durum);
            Assert.Equal(0, fotoSayisi);
            Assert.Null(dosya.KarsiTarafPlaka);
        }

        [Fact]
        public async Task UcParcaTekBakimaBaglidir()
        {
            await _db.DemoSeeder().RunAsync();

            var parcalar = await _db.Context.MaintenanceParts.IgnoreQueryFilters().ToListAsync();

            Assert.Equal(3, parcalar.Count);
            Assert.Single(parcalar.Select(p => p.MaintenanceRecordId).Distinct());
            Assert.Equal(3, parcalar.Select(p => p.ParcaTuru).Distinct().Count());
        }

        [Fact]
        public async Task AktifZimmeteDokunulmaz()
        {
            await _db.DemoSeeder().RunAsync();

            var once = await _db.Context.VehicleAssignments.IgnoreQueryFilters().SingleAsync();

            await _db.DemoSeeder().RunAsync();
            await _db.DemoSeeder().RunAsync();

            var sonra = await _db.Context.VehicleAssignments.IgnoreQueryFilters().SingleAsync();

            Assert.Equal(once.Id, sonra.Id);
            Assert.Equal(once.UserId, sonra.UserId);
            Assert.Equal(once.StartDate, sonra.StartDate);
            Assert.Null(sonra.EndDate);
        }

        [Fact]
        public async Task DemoAracKasaVitesMotorAlirVeTahmineHazirdir()
        {
            await _db.DemoSeeder().RunAsync();

            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().SingleAsync();

            Assert.NotNull(arac.KasaTipi);
            Assert.Equal(KasaTipi.Hatchback5, arac.KasaTipi);
            Assert.False(string.IsNullOrWhiteSpace(arac.Vites));
            Assert.False(string.IsNullOrWhiteSpace(arac.Motor));
        }

        [Fact]
        public async Task EksikOlanAlanDoldurulurDoluOlanaDokunulmaz()
        {
            await _db.DemoSeeder().RunAsync();

            var arac = await _db.Context.Vehicles.IgnoreQueryFilters().SingleAsync();
            arac.Vites = null;
            arac.Motor = "Elle girilmiş motor";
            arac.KasaTipi = KasaTipi.Suv;
            await _db.Context.SaveChangesAsync();

            var eklendi = await _db.DemoSeeder().RunAsync();

            var sonra = await _db.Context.Vehicles.IgnoreQueryFilters().SingleAsync();

            Assert.True(eklendi);
            Assert.False(string.IsNullOrWhiteSpace(sonra.Vites));
            Assert.Equal("Elle girilmiş motor", sonra.Motor);
            Assert.Equal(KasaTipi.Suv, sonra.KasaTipi);
        }

        [Fact]
        public async Task MevcutDemoAracaEksikKayitlarSonradanEklenir()
        {
            await _db.DemoSeeder().RunAsync();

            var degerler = await _db.Context.AracDegerleri.IgnoreQueryFilters().ToListAsync();
            _db.Context.AracDegerleri.RemoveRange(degerler);
            await _db.Context.SaveChangesAsync();

            var eklendi = await _db.DemoSeeder().RunAsync();
            var sonrasi = await SayimAsync();

            Assert.True(eklendi);
            Assert.Equal(2, sonrasi.Deger);
            Assert.Equal(2, sonrasi.Evrak);
            Assert.Equal(1, sonrasi.Zimmet);
        }
    }
}
