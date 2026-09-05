using Garajim.API.Controllers;
using Garajim.Entity.Dtos;

namespace Garajim.Tests.Unit
{
    public class TutanakYazdirTests
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
        public void TutanakHtmlSatirIciScriptTasimaz()
        {
            var html = TutanakSayfasi.Olustur(new HasarDto
            {
                Id = 1,
                Plaka = "34ABC123",
                OlayTarihi = new DateTime(2026, 9, 1),
                Aciklama = "Test",
                Konum = "On tampon"
            });

            Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("class=\"yazdir\"", html);
        }

        [Fact]
        public void PencereTiklamaAnindaAcilirVeYazdirmaDinleyiciyleBaglanir()
        {
            var app = AppJs();
            var bas = app.IndexOf("function hasarTutanagiAc(", StringComparison.Ordinal);
            var son = app.IndexOf("function bindHasar(", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);

            var govde = app.Substring(bas, son - bas);
            var pencereAc = govde.IndexOf("window.open(", StringComparison.Ordinal);
            var getir = govde.IndexOf("fetch(", StringComparison.Ordinal);

            Assert.True(pencereAc > 0 && getir > pencereAc,
                "pencere fetch'ten önce açılmalı, yoksa açılır pencere engelleyicisine takılır");
            Assert.Contains("addEventListener(\"click\"", govde);
            Assert.Contains("pencere.print()", govde);
        }
    }
}
