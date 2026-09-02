using Garajim.Business.Usta;
using Xunit.Abstractions;

namespace Garajim.Tests.Unit
{
    public class BilgiButceOlcumuTests
    {
        private static readonly string[] OrnekSorular =
        {
            "P0420 kodu çıktı",
            "egea 1.4 triger ne zaman",
            "kış lastiği zorunlu mu",
            "frende metalik ses geliyor",
            "muayeneden kaldım sinyal rengi"
        };

        private readonly ITestOutputHelper _cikti;

        public BilgiButceOlcumuTests(ITestOutputHelper cikti)
        {
            _cikti = cikti;
        }

        private static IReadOnlyList<BilgiKaydi> Kayitlar()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return new BilgiYukleyici().Yukle(Path.Combine(kok.FullName, "Garajim.Business", "Usta", "Bilgi"));
        }

        private static BilgiSecici Secici()
        {
            return new BilgiSecici(Kayitlar());
        }

        [Fact]
        public void ButceOlcumuRaporlanir()
        {
            var tumu = Kayitlar();
            var secici = new BilgiSecici(tumu);

            _cikti.WriteLine($"Bilgi tabanı {tumu.Count} kayıt · bütçe {BilgiSecici.MaxToken} token / en fazla {BilgiSecici.MaxKayit} kayıt");

            foreach (var soru in OrnekSorular)
            {
                var secilen = secici.Sec(soru);
                var token = secilen.Sum(k => BilgiSecici.TokenTahmini(k.Metin));

                _cikti.WriteLine($"\"{soru}\" -> {secilen.Count} kayıt, {token} token ({string.Join(", ", secilen.Select(k => k.Id))})");

                Assert.True(token <= BilgiSecici.MaxToken, soru + " bütçeyi aştı: " + token);
                Assert.True(secilen.Count <= BilgiSecici.MaxKayit);
            }
        }
    }
}
