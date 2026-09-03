using Garajim.Business.Katalog;

namespace Garajim.Tests.Unit
{
    public class AracEslestiriciTests
    {
        private static AracKatalogu Katalog() =>
            AracKatalogu.Yukle(Path.Combine(AppContext.BaseDirectory, AracKatalogu.KlasorAdi));

        private static AracEslesmesi Esle(string marka, string model) =>
            AracEslestirici.Esle(Katalog(), marka, model);

        [Theory]
        [InlineData("fiat", "egea", "Fiat", "Egea", null)]
        [InlineData("FIAT", "EGEA", "Fiat", "Egea", null)]
        [InlineData("VW", "Golf", "Volkswagen", "Golf", null)]
        [InlineData("Volkswagen", "golf", "Volkswagen", "Golf", null)]
        [InlineData("Mercedes", "C", "Mercedes - Benz", "C", null)]
        [InlineData("Mercedes-Benz", "c", "Mercedes - Benz", "C", null)]
        [InlineData("Toyota", "Toyota Corolla", "Toyota", "Corolla", null)]
        [InlineData("Fiat", "Egea 1.4", "Fiat", "Egea", "1.4")]
        [InlineData("Renault", "Clio 1.5 dCi", "Renault", "Clio", "1.5 dCi")]
        [InlineData("Tofas", "Şahin", "Tofaş", "Şahin", null)]
        public void OrnekCiftlerKatalogaOturur(string marka, string model, string beklenenMarka, string beklenenSeri, string beklenenMotor)
        {
            var sonuc = Esle(marka, model);

            Assert.NotNull(sonuc);
            Assert.Equal(beklenenMarka, sonuc.Marka);
            Assert.Equal(beklenenSeri, sonuc.Seri);
            Assert.Equal(beklenenMotor, sonuc.Motor);
        }

        [Theory]
        [InlineData("Fiat", "Corolla")]
        [InlineData("Fiat", "")]
        [InlineData("Fiat", "daşak arabası")]
        public void EslesmeyenCiftNullDoner(string marka, string model)
        {
            Assert.Null(Esle(marka, model));
        }

        [Fact]
        public void MarkaBilinmezkenSeridenBulunur()
        {
            var sonuc = Esle("Bilinmeyen", "Egea");

            Assert.NotNull(sonuc);
            Assert.Equal("Fiat", sonuc.Marka);
            Assert.Equal("Egea", sonuc.Seri);
        }

        [Fact]
        public void TurkceKarakterDuyarsizEslesir()
        {
            var sonuc = Esle("TOFAŞ", "şahin");

            Assert.NotNull(sonuc);
            Assert.Equal("Tofaş", sonuc.Marka);
            Assert.Equal("Şahin", sonuc.Seri);
        }
    }
}
