using Garajim.RehberUretici;

namespace Garajim.Tests.Unit
{
    public class RehberBaslikTests
    {
        private static RehberKaydi Kayit(string bolum, string id, string metin) => new RehberKaydi
        {
            Id = id,
            Bolum = bolum,
            Metin = metin,
            Kaynak = "kaynak",
            Guncelleme = "2026-09-02"
        };

        [Fact]
        public void BelirtiBasligiSoruyaCevrilir()
        {
            var kayit = Kayit(Bolumler.Belirti, "blr-001",
                "Belirti: frene basınca metalik/gıcırtı sesi. | En sık: balata bitmiş.");

            Assert.Equal("Frene basınca metalik/gıcırtı sesi neden olur?", Basliklar.Uret(kayit));
        }

        [Fact]
        public void ObdBasligiSabitBicimdedir()
        {
            var kayit = Kayit(Bolumler.Obd, "obd-P0420",
                "P0420 — Katalitik konvertör verimi eşiğin altında, Bank 1. Aciliyet: Bakimda.");

            Assert.Equal("P0420 arıza kodu nedir? Anlamı, nedenleri, aciliyet", Basliklar.Uret(kayit));
        }

        [Fact]
        public void BakimBasligiModelAdindanTurer()
        {
            var kayit = Kayit(Bolumler.Bakim, "bkm-001",
                "Fiat Egea/Linea/Doblo 1.4 Fire 95 HP (benzin, sıkça LPG'li): triger KAYIŞLI — 120.000 km.");

            Assert.Equal("Fiat Egea/Linea/Doblo 1.4 Fire 95 HP bakım aralıkları", Basliklar.Uret(kayit));
        }

        [Fact]
        public void BakimKuralKaydiKendiBasliginiAlir()
        {
            var kayit = Kayit(Bolumler.Bakim, "bkm-000", "KURAL: Bu dosyadaki aralıklar genel değerlerdir.");

            Assert.Equal("Periyodik bakım aralıkları: genel kural", Basliklar.Uret(kayit));
        }

        [Fact]
        public void MuayeneVeTurkiyeBasligiIlkCumledir()
        {
            var tvt = Kayit(Bolumler.Muayene, "tvt-001",
                "TÜVTÜRK muayenesinde sonuç dört sınıftır: Kusursuz ve Hafif Kusurlu geçer. Ağır kusur geçemez.");

            Assert.Equal("TÜVTÜRK muayenesinde sonuç dört sınıftır", Basliklar.Uret(tvt));
        }

        [Fact]
        public void BaslikAltmisKarakteriAsmaz()
        {
            var kayit = Kayit(Bolumler.Turkiye, "tro-001",
                "Bu cümle bilerek çok uzun tutulmuştur ve altmış karakteri fazlasıyla aşan bir başlık üretmelidir ki kırpma çalışsın. Devamı.");

            var baslik = Basliklar.Uret(kayit);

            Assert.True(baslik.Length <= 60, baslik.Length + ": " + baslik);
            Assert.EndsWith("…", baslik);
        }

        [Fact]
        public void AciklamaYuzElliBesKarakteriAsmaz()
        {
            var kayit = Kayit(Bolumler.Belirti, "blr-002",
                "Belirti: " + new string('a', 400) + ". | En sık: bir şey.");

            var aciklama = Basliklar.Aciklama(kayit);

            Assert.True(aciklama.Length <= 155, aciklama.Length.ToString());
        }
    }
}
