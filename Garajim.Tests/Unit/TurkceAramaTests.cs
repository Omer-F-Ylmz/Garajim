using Garajim.Dal.Sorgular;

namespace Garajim.Tests.Unit
{
    public class TurkceAramaTests
    {
        private class Kayit
        {
            public string Ad { get; set; }
            public string Not { get; set; }
        }

        private static readonly List<Kayit> Veri = new List<Kayit>
        {
            new Kayit { Ad = "Yağ filtresi", Not = "Şubat bakımı" },
            new Kayit { Ad = "Fren balatası", Not = "ÖN takım" },
            new Kayit { Ad = "Lastik", Not = null },
            new Kayit { Ad = "ÇİFT kademeli", Not = "ısıtıcı" }
        };

        private static List<Kayit> Ara(string terim)
        {
            var suzgec = TurkceArama.Iceren<Kayit>(terim, k => k.Ad, k => k.Not);

            return suzgec == null
                ? Veri.ToList()
                : Veri.AsQueryable().Where(suzgec).ToList();
        }

        [Theory]
        [InlineData("YAĞ")]
        [InlineData("yag")]
        [InlineData("YAG")]
        [InlineData("yağ")]
        public void BuyukKucukVeTurkceKarakterDuyarsiz(string terim)
        {
            var sonuc = Ara(terim);

            Assert.Single(sonuc);
            Assert.Equal("Yağ filtresi", sonuc[0].Ad);
        }

        [Theory]
        [InlineData("subat")]
        [InlineData("ŞUBAT")]
        public void IkinciAlandaDaArar(string terim)
        {
            Assert.Single(Ara(terim));
        }

        [Fact]
        public void NoktaliIHarfiKatlanir()
        {
            Assert.Single(Ara("cift"));
            Assert.Single(Ara("ÇIFT"));
        }

        [Fact]
        public void IsitiriciNullAlanPatlatmaz()
        {
            Assert.Single(Ara("isitici"));
        }

        [Fact]
        public void BosTerimSuzgecUretmez()
        {
            Assert.Null(TurkceArama.Iceren<Kayit>("   ", k => k.Ad));
            Assert.Equal(Veri.Count, Ara(null).Count);
        }

        [Fact]
        public void EslesmeyenTerimBosDoner()
        {
            Assert.Empty(Ara("turbo"));
        }

        [Theory]
        [InlineData("Yağ filtresi", "yag filtresi")]
        [InlineData("ÇİĞDEM", "cigdem")]
        [InlineData("ŞIRINCE", "sirince")]
        [InlineData("Öğüt", "ogut")]
        public void SadelestirmeBeklendigiGibi(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, TurkceArama.Sadelestir(girdi));
        }
    }
}
