namespace Garajim.Tests.Unit
{
    public class RaporSekmesiTests
    {
        private static string AppJs()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "app.js"));
        }

        [Fact]
        public void SekmeAcilincaOzetDeYuklenir()
        {
            var app = AppJs();
            var bas = app.IndexOf("tab === \"rapor\"", StringComparison.Ordinal);
            var govde = app.Substring(bas, 220);

            Assert.Contains("loadSummary()", govde);
        }

        [Fact]
        public void KategoriTablosuBosDurumTasir()
        {
            var app = AppJs();
            var bas = app.IndexOf("function loadSummary(", StringComparison.Ordinal);
            var son = app.IndexOf("function loadFuelStats(", bas, StringComparison.Ordinal);
            var govde = app.Substring(bas, son - bas);

            Assert.Contains("emptyRow(", govde);
            Assert.Contains("clear(cards)", govde);
        }
    }
}
