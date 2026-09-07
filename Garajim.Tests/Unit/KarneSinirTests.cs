namespace Garajim.Tests.Unit
{
    public class KarneSinirTests
    {
        private static string Govde()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);

            var metin = File.ReadAllText(Path.Combine(kok.FullName, "Garajim.Business", "Concrete", "KarneManager.cs"));
            var bas = metin.IndexOf("public async Task<IDataResult<KarneDto>> GoruntuleAsync", StringComparison.Ordinal);

            Assert.True(bas > 0, "GoruntuleAsync bulunamadı");

            var son = metin.IndexOf("\n        private ", bas, StringComparison.Ordinal);

            Assert.True(son > bas);
            return metin.Substring(bas, son - bas);
        }

        [Fact]
        public void BakimListesiSinirliCekilir()
        {
            var govde = Govde();

            Assert.DoesNotContain("_maintenanceDal.GetListAsync(", govde);
            Assert.Contains("_maintenanceDal.GetRecentAsync(", govde);
        }

        [Fact]
        public void BakimToplamiSinirdanEtkilenmez()
        {
            var govde = Govde();

            Assert.Contains("_maintenanceDal.GetTotalCostAsync(", govde);
        }

        [Fact]
        public void YakitOzetiSqlTarafindaHesaplanir()
        {
            var govde = Govde();

            Assert.DoesNotContain("_fuelDal.GetListAsync(", govde);
            Assert.Contains("_fuelDal.KarneOzetiAsync(", govde);
        }

        [Fact]
        public void BelgeListesiSinirliVeSqlSuzgecli()
        {
            var govde = Govde();

            Assert.DoesNotContain("_documentDal.GetListAsync(", govde);
            Assert.Contains("_documentDal.KarneBelgeleriAsync(", govde);
        }

        [Fact]
        public void SinirTekKaynaktanGelir()
        {
            var govde = Govde();

            Assert.Contains("QueryLimits.MaxListSize", govde);
        }
    }
}
