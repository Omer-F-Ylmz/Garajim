using Garajim.Business.Concrete;
using Garajim.Entity.Enums;
using Garajim.ML.Models;

namespace Garajim.Tests.Unit
{
    public class KasaTipiSozlukTests
    {
        private static FiyatModeliSozlugu Sozluk()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return FiyatModeliSozlugu.Yukle(Path.Combine(kok.FullName, "Garajim.API", "MLModels", "price-model.zip"));
        }

        [Fact]
        public void EnumModelSozluguyleBirebirOrtusur()
        {
            var sozluk = Sozluk();
            var enumDegerleri = Enum.GetValues<KasaTipi>().Select(KasaTipiAdlari.ModelDegeri).ToList();

            Assert.Equal(10, enumDegerleri.Count);
            Assert.Equal(enumDegerleri.Count, enumDegerleri.Distinct().Count());
            Assert.Equal(sozluk.KasaSayisi, enumDegerleri.Count);

            foreach (var deger in enumDegerleri)
            {
                Assert.True(sozluk.KasaTaniniyor(deger), "Model sözlüğünde yok: " + deger);
            }
        }

        [Fact]
        public void ModeldekiHerKasaEnumdaKarsilikBulur()
        {
            var sozluk = Sozluk();
            var enumDegerleri = Enum.GetValues<KasaTipi>().Select(KasaTipiAdlari.ModelDegeri).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var kasa in sozluk.Kasalar)
            {
                Assert.Contains(kasa, enumDegerleri);
            }
        }

        [Fact]
        public void HerKasaTipininTurkceAdiVardir()
        {
            foreach (var kasa in Enum.GetValues<KasaTipi>())
            {
                Assert.False(string.IsNullOrWhiteSpace(KasaTipiAdlari.Ad(kasa)));
            }

            Assert.Equal("Hatchback (5 kapı)", KasaTipiAdlari.Ad(KasaTipi.Hatchback5));
            Assert.Equal("Hatchback/5", KasaTipiAdlari.ModelDegeri(KasaTipi.Hatchback5));
            Assert.Equal("Pick-up", KasaTipiAdlari.ModelDegeri(KasaTipi.PickUp));
        }
    }
}
