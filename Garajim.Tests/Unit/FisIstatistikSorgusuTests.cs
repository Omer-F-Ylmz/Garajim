namespace Garajim.Tests.Unit
{
    public class FisIstatistikSorgusuTests
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
            var metin = Oku("Garajim.Business", "Concrete", "ReceiptManager.cs");
            var bas = metin.IndexOf("public async Task<IDataResult<ReceiptStatsDto>> GetStatsAsync", StringComparison.Ordinal);

            Assert.True(bas > 0);

            var son = metin.IndexOf("\n        private static bool DuzeltilenIceriyorMu", bas, StringComparison.Ordinal);

            Assert.True(son > bas);
            return metin.Substring(bas, son - bas);
        }

        [Fact]
        public void IstatistikTumTaslaklariBellegeAlmaz()
        {
            var govde = Govde();

            Assert.DoesNotContain("_draftDal.GetListAsync()", govde);
            Assert.Contains("_draftDal.IstatistikAsync()", govde);
        }

        [Fact]
        public void DuzeltmeOraniYalnizGerekliKolonuCeker()
        {
            var govde = Govde();

            Assert.Contains("ElleOnaylananDuzeltmeAlanlariAsync()", govde);
        }

        [Fact]
        public void SayimlarSqlTarafindaYapilir()
        {
            var dal = Oku("Garajim.Dal", "Concrete", "EfReceiptDraftDal.cs");
            var bas = dal.IndexOf("IstatistikAsync", StringComparison.Ordinal);

            Assert.True(bas > 0);

            var govde = dal.Substring(bas, Math.Min(1600, dal.Length - bas));

            Assert.Contains("CountAsync", govde);
            Assert.Contains("SumAsync", govde);
            Assert.Contains("AsNoTracking", govde);
        }

        [Fact]
        public void DuzeltmeSorgusuYalnizAlanKolonunuSecer()
        {
            var dal = Oku("Garajim.Dal", "Concrete", "EfReceiptDraftDal.cs");
            var bas = dal.IndexOf("ElleOnaylananDuzeltmeAlanlariAsync", StringComparison.Ordinal);

            Assert.True(bas > 0);

            var govde = dal.Substring(bas, Math.Min(500, dal.Length - bas));

            Assert.Contains("Select(d => d.DuzeltilenAlanlar)", govde);
        }
    }
}
