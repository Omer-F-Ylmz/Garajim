namespace Garajim.Tests.Unit
{
    public class UstaSekmesiTests
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
        public void SekmeDurumuHerRoldeOkunabilirUctanGelir()
        {
            var app = AppJs();
            var bas = app.IndexOf("function ustaDurumunuUygula(", StringComparison.Ordinal);
            var son = app.IndexOf("function applyRole(", bas, StringComparison.Ordinal);
            var govde = app.Substring(bas, son - bas);

            Assert.Contains("/api/Saglik/ozellikler", govde);
            Assert.DoesNotContain("/api/Receipts/stats", govde);
        }

        [Fact]
        public void IstekBasarisizsaSekmeGizlenir()
        {
            var app = AppJs();
            var bas = app.IndexOf("function ustaDurumunuUygula(", StringComparison.Ordinal);
            var son = app.IndexOf("function applyRole(", bas, StringComparison.Ordinal);
            var govde = app.Substring(bas, son - bas);
            var yakala = govde.IndexOf(".catch(", StringComparison.Ordinal);

            Assert.True(yakala > 0);
            Assert.Contains("ustaSekmesiniGizle", govde.Substring(yakala));
        }
    }
}
