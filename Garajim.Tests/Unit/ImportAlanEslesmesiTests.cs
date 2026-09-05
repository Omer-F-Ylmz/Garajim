using Garajim.Business.Concrete.Import;

namespace Garajim.Tests.Unit
{
    public class ImportAlanEslesmesiTests
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

        private static List<string> IstemciAlanlari(string kayitTuru)
        {
            var app = AppJs();
            var bas = app.IndexOf("function importAlanlari(", StringComparison.Ordinal);
            var son = app.IndexOf("function importZorunlular(", bas, StringComparison.Ordinal);
            var govde = app.Substring(bas, son - bas);

            var dal = kayitTuru switch
            {
                "Yakit" => govde.Split("\"Yakit\"")[1].Split(']')[0],
                "Bakim" => govde.Split("\"Bakim\"")[1].Split(']')[0],
                _ => govde.Split("return [")[govde.Split("return [").Length - 1]
            };

            return System.Text.RegularExpressions.Regex.Matches(dal, "\"([a-z]+)\"")
                .Select(m => m.Groups[1].Value)
                .Where(d => d != "Yakit" && d != "Bakim")
                .ToList();
        }

        [Theory]
        [InlineData("Yakit")]
        [InlineData("Bakim")]
        [InlineData("Masraf")]
        public void IstemciAlanListesiSunucuylaAynidir(string kayitTuru)
        {
            Assert.Equal(ImportSablonlari.AlanlariAl(kayitTuru), IstemciAlanlari(kayitTuru));
        }

        [Fact]
        public void TamDolumAlaniIstemcideTanimli()
        {
            var app = AppJs();
            var bas = app.IndexOf("IMPORT_ALANLARI = [", StringComparison.Ordinal);
            var blok = app.Substring(bas, app.IndexOf("];", bas, StringComparison.Ordinal) - bas);

            Assert.Contains("\"tamdolum\"", blok);
        }
    }
}
