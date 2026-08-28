using Garajim.Business.Concrete;

namespace Garajim.Tests.Unit
{
    public class DocumentStoragePathTests
    {
        private static string TemelKlasor =>
            AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        private static string UstKlasor => Directory.GetParent(TemelKlasor).FullName;

        [Fact]
        public void GoreliYolUygulamaKlasoruneGoreCozulur()
        {
            var goreli = Path.Combine("..", "data", "documents");
            var beklenen = Path.Combine(UstKlasor, "data", "documents");

            var sonuc = DocumentManager.DepoYolunuCoz(goreli);

            Assert.Equal(beklenen, sonuc);
            Assert.True(Path.IsPathRooted(sonuc), "Çözülen yol mutlak olmalı.");
        }

        [Fact]
        public void NoktasizGoreliYolUygulamaKlasorununAltinaDuser()
        {
            var beklenen = Path.Combine(TemelKlasor, "belgeler");

            var sonuc = DocumentManager.DepoYolunuCoz("belgeler");

            Assert.Equal(beklenen, sonuc);
        }

        [Fact]
        public void MutlakYolDegismedenKullanilir()
        {
            var mutlak = Path.Combine(Path.GetPathRoot(TemelKlasor), "garajim-mutlak-depo");

            var sonuc = DocumentManager.DepoYolunuCoz(mutlak);

            Assert.Equal(mutlak, sonuc);
            Assert.DoesNotContain(TemelKlasor, sonuc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void BosDegerAppDataAltinaDuser(string deger)
        {
            var beklenen = Path.Combine(TemelKlasor, "App_Data", "documents");

            var sonuc = DocumentManager.DepoYolunuCoz(deger);

            Assert.Equal(beklenen, sonuc);
        }
    }
}
