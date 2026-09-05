using System.Text.RegularExpressions;

namespace Garajim.Tests.Unit
{
    public class TabloSarmalayiciTests
    {
        private static string Oku(string dosya)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", dosya));
        }

        [Fact]
        public void HerTabloKaydirilabilirKapsayicidadir()
        {
            var html = Oku("index.html");
            var sarmasiz = new List<int>();

            foreach (Match eslesme in Regex.Matches(html, "<table"))
            {
                var onceki = html.LastIndexOf("<div class=\"table-wrap\">", eslesme.Index, StringComparison.Ordinal);
                var araKapanis = onceki < 0 ? -1 : html.IndexOf("</div>", onceki, StringComparison.Ordinal);

                if (onceki < 0 || araKapanis < eslesme.Index)
                {
                    sarmasiz.Add(eslesme.Index);
                }
            }

            Assert.True(sarmasiz.Count == 0,
                sarmasiz.Count + " tablo table-wrap dışında (ilk konum " +
                (sarmasiz.Count > 0 ? sarmasiz[0].ToString() : "-") + ")");
        }
    }
}
