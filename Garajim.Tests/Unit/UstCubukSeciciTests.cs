namespace Garajim.Tests.Unit
{
    public class UstCubukSeciciTests
    {
        private static string Css()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "styles.css"));
        }

        private static string Blok(string css, string secici)
        {
            var bas = css.IndexOf(secici + " {", StringComparison.Ordinal);
            Assert.True(bas > 0, secici + " yok");
            return css.Substring(bas, css.IndexOf('}', bas) - bas);
        }

        [Fact]
        public void MasaustundeSeciciGenisligiSinirli()
        {
            Assert.Contains("max-width:", Blok(Css(), ".topbar select"));
        }

        [Fact]
        public void MobildeSeciciOkunacakKadarGenis()
        {
            var css = Css();
            var mobil = css.Split("@media").FirstOrDefault(b => b.Contains("max-width: 640px") && b.Contains(".topbar select"));

            Assert.NotNull(mobil);

            var bas = mobil.IndexOf(".topbar select {", StringComparison.Ordinal);
            var blok = mobil.Substring(bas, mobil.IndexOf('}', bas) - bas);

            Assert.DoesNotContain("min-width: 0", blok);
            Assert.Contains("min-width:", blok);
        }
    }
}
