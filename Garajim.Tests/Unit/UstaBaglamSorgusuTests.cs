namespace Garajim.Tests.Unit
{
    public class UstaBaglamSorgusuTests
    {
        private static string Oku(params string[] parcalar)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(new[] { kok.FullName }.Concat(parcalar).ToArray()));
        }

        private static string Govde()
        {
            var metin = Oku("Garajim.Business", "Concrete", "UstaManager.cs");
            var bas = metin.IndexOf("private async Task<string> AracBaglamiAsync", StringComparison.Ordinal);

            Assert.True(bas > 0);

            var son = metin.IndexOf("\n        private ", bas + 20, StringComparison.Ordinal);

            Assert.True(son > bas);
            return metin.Substring(bas, son - bas);
        }

        [Fact]
        public void BaglamHicbirListeyiSinirsizCekmez()
        {
            var govde = Govde();

            Assert.DoesNotContain("_maintenanceDal.GetListAsync(", govde);
            Assert.DoesNotContain("_evrakDal.GetListAsync(", govde);
            Assert.DoesNotContain("_reminderDal.GetListAsync(", govde);
            Assert.DoesNotContain("_partDal.GetByVehicleAsync(", govde);
        }

        [Fact]
        public void BaglamSinirliDalMetotlariniCagirir()
        {
            var govde = Govde();

            Assert.Contains("_maintenanceDal.GetRecentAsync(", govde);
            Assert.Contains("_evrakDal.AktifListeAsync(", govde);
            Assert.Contains("_reminderDal.AcikListeAsync(", govde);
            Assert.Contains("_partDal.KayitlaraGoreAsync(", govde);
        }

        [Fact]
        public void SinirlarSabitOlarakDurur()
        {
            var metin = Oku("Garajim.Business", "Concrete", "UstaManager.cs");

            Assert.Contains("BaglamBakimSayisi = 5", metin);
            Assert.Contains("BaglamEvrakSayisi = 10", metin);
            Assert.Contains("BaglamHatirlatmaSayisi = 8", metin);
        }

        [Fact]
        public void ParcaSorgusuYalnizIlgiliKayitlariCeker()
        {
            var dal = Oku("Garajim.Dal", "Concrete", "EfMaintenancePartDal.cs");
            var bas = dal.IndexOf("KayitlaraGoreAsync", StringComparison.Ordinal);

            Assert.True(bas > 0);

            var govde = dal.Substring(bas, Math.Min(700, dal.Length - bas));

            Assert.Contains("kayitIdleri.Contains(p.MaintenanceRecordId)", govde);
            Assert.Contains("AsNoTracking", govde);
        }
    }
}
