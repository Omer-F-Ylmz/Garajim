namespace Garajim.Tests.Unit
{
    public class DosyaSecimiTests
    {
        private static string Html()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "index.html"));
        }

        [Fact]
        public void DosyaSecicilerKameraylaSinirlanmaz()
        {
            Assert.DoesNotContain("capture=", Html());
        }

        [Fact]
        public void FisVeTopluYuklemeAyniTurleriKabulEder()
        {
            var html = Html();
            var tek = html.Substring(html.IndexOf("id=\"receipt-file\"", StringComparison.Ordinal), 120);
            var toplu = html.Substring(html.IndexOf("id=\"bulk-files\"", StringComparison.Ordinal), 120);

            Assert.Contains("accept=\"image/*,.pdf\"", tek);
            Assert.Contains("accept=\"image/*,.pdf\"", toplu);
        }
    }
}
