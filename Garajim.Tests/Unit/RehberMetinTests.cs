using Garajim.RehberUretici;

namespace Garajim.Tests.Unit
{
    public class RehberMetinTests
    {
        private const string BelirtiMetni =
            "Belirti: frene basınca metalik/gıcırtı sesi. | En sık: fren balatası bitmiş; aciliyet: BuHafta. " +
            "| Sık: yeni balatanın gıcırdaması; aciliyet: Bakimda. | Nadir: kaliper pimi kuruması. " +
            "| Kırmızı: sürekli öğütme sesi + fren mesafesi uzadıysa metal metale — hemen serviste. " +
            "| Ustaya: 'Frende metalik ses var, balata kalınlığına bakar mısın.'";

        private const string ObdMetni =
            "P0420 — Katalitik konvertör verimi eşiğin altında, Bank 1. Sık nedenler (en sıktan nadire): " +
            "arka oksijen sensörü, katalizör yorulması, egzoz kaçağı. Aciliyet: Bakimda. Kodu silmeden önce " +
            "dondurulmuş veri okunmalı.";

        [Fact]
        public void BelirtiBolumleriEtiketlereAyrilir()
        {
            var bolumler = MetinAyristirici.Bolumler(BelirtiMetni);

            Assert.Equal(new[] { "Belirti", "En sık", "Sık", "Nadir", "Kırmızı", "Ustaya" },
                bolumler.Select(b => b.Baslik).ToArray());
            Assert.Equal("frene basınca metalik/gıcırtı sesi.", bolumler[0].Metin);
            Assert.StartsWith("fren balatası bitmiş", bolumler[1].Metin);
        }

        [Fact]
        public void KirmiziBolumUyariRozetiTasir()
        {
            var bolumler = MetinAyristirici.Bolumler(BelirtiMetni);

            Assert.True(bolumler.Single(b => b.Baslik == "Kırmızı").Uyari);
            Assert.False(bolumler.Single(b => b.Baslik == "En sık").Uyari);
        }

        [Fact]
        public void EtiketsizMetinTekBolumKalir()
        {
            var bolumler = MetinAyristirici.Bolumler(ObdMetni);

            Assert.Single(bolumler);
            Assert.Null(bolumler[0].Baslik);
        }

        [Theory]
        [InlineData("Aciliyet: Bakimda.", "Bakimda")]
        [InlineData("... aciliyet: BuHafta. | Sık: ...", "BuHafta")]
        [InlineData("hiç aciliyet yok", null)]
        public void AciliyetMetindenOkunur(string metin, string beklenen)
        {
            Assert.Equal(beklenen, MetinAyristirici.Aciliyet(metin));
        }

        [Fact]
        public void IlkCumleNoktadanKesilir()
        {
            Assert.Equal("frene basınca metalik/gıcırtı sesi",
                MetinAyristirici.IlkCumle("frene basınca metalik/gıcırtı sesi. Devamı var."));
            Assert.Equal("P0420 — Katalitik konvertör verimi eşiğin altında, Bank 1",
                MetinAyristirici.IlkCumle(ObdMetni));
        }

        [Fact]
        public void IlkCumleParantezIcindekiNoktayiSonSanmaz()
        {
            Assert.Equal("Zorunlu kış lastiği dönemi 15 Kasım-15 Nisan (4 Ekim 2025 tebliğiyle uzatıldı, 5 ay)",
                MetinAyristirici.IlkCumle("Zorunlu kış lastiği dönemi 15 Kasım-15 Nisan (4 Ekim 2025 tebliğiyle uzatıldı, 5 ay). Kapsam: ..."));
        }

        [Fact]
        public void OndalikNoktaCumleSonuSayilmaz()
        {
            Assert.Equal("Fiat 1.3 Multijet zinciri bakım gerektirmez",
                MetinAyristirici.IlkCumle("Fiat 1.3 Multijet zinciri bakım gerektirmez. Yağ+filtre 10.000 km."));
        }
    }
}
