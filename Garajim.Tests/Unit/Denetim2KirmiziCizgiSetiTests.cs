using Garajim.Business.Usta;

namespace Garajim.Tests.Unit
{
    public class Denetim2KirmiziCizgiSetiTests
    {
        private static readonly string[] Pozitifler =
        {
            "fren tutmuyo abi hic",
            "frene basiyorum pedal yere kadar gidiyo",
            "fren pedali bosaldi durmuyor",
            "arabanin freni yok, kavsakta zor durdum",
            "pedal tas gibi degil, yere dayaniyor fren tutmiyor",
            "direksiyon kilitlendi cevirmiyorum",
            "direksiyon aniden kilitli kaldi",
            "direksiyon cok agirlasti ve cevirirken catirti geliyor",
            "cevirince gicirti var, direksiyon cok sert",
            "panelde kirmizi yag lambasi yandi",
            "kirmizi hararet ikazi yaniyor panelde",
            "sarj lambasi yandi rengi kirmizi",
            "kirmizi yag ikazi yandi ne yapmaliyim",
            "araba hararet yapti ibre tavana vurdu",
            "kaputtan buhar cikiyor motor kaynadi",
            "radyator kayniyor sicaklik ibresi kirmizida",
            "hararet yapiyor surekli, su eksiliyor",
            "arabada cok yogun benzin kokusu var",
            "altindan mazot damliyor ve koku geliyor",
            "yakit kokusu geliyor icerde duramiyorum",
            "kabine duman doluyor",
            "iceriye duman geliyor yanik plastik kokuyor",
            "motordan metal sesi geliyor ve arac titriyor",
            "arac titriyor altindan demir sesi var",
            "otoyolda giderken arac stop etti"
        };

        private static readonly string[] Negatifler =
        {
            "fren balatalarim ne zaman degismeli",
            "fren diski torna yapilir mi",
            "fren hidroligi kac yilda bir degisir",
            "frende hafif gicirti var, balata bitmis olabilir mi",
            "el freni biraz gevsek, ayar yapilir mi",
            "direksiyon ayari ne kadar tutar",
            "direksiyon simidi kilifi ariyorum",
            "direksiyon hidrolik yagi hangi marka olmali",
            "panelde motor ariza lambasi sari yaniyor",
            "yag degisim lambasi yandi bakim zamani gelmis",
            "lastik basinci ikazi yaniyor",
            "klima gazi bitmis galiba sogutmuyor",
            "egzozdan hafif mavi duman geliyor",
            "egzozdan sabahlari beyaz duman cikiyor",
            "depoyu doldurunca tuketim nasil olculur",
            "benzin fiyatlari ne kadar oldu",
            "bozuk yolda tak tak sesi geliyor",
            "rolantide hafif titreme var",
            "arac rolantide bazen stop ediyor",
            "muayene icin hangi evraklari goturmeliyim",
            "kis lastigi ne zaman zorunlu oluyor",
            "aku kac yilda bir degisir",
            "triger kayisi kac kilometrede degisir",
            "sanziman yagi degisimi ne kadar tutar",
            "yakit filtresi ne zaman degismeli"
        };

        [Fact]
        public void YirmiBesPozitifCumleninTamamiYakalanir()
        {
            var kacan = Pozitifler.Where(c => !KirmiziCizgiler.VarMi(c)).ToList();

            Assert.True(kacan.Count == 0, "Kaçırılan kırmızı çizgi cümleleri: " + string.Join(" | ", kacan));
        }

        [Fact]
        public void YirmiBesNegatifCumledeYanlisPozitifEnFazlaIki()
        {
            var yanlis = Negatifler.Where(KirmiziCizgiler.VarMi).ToList();

            Assert.True(yanlis.Count <= 2, "Yanlış pozitif sayısı 2'yi aştı: " + string.Join(" | ", yanlis));
        }

        [Fact]
        public void PozitifCumlelerinTamaminaKodAtanir()
        {
            Assert.All(Pozitifler, c =>
            {
                var bulgu = KirmiziCizgiler.Bul(c);
                Assert.NotNull(bulgu);
                Assert.False(string.IsNullOrWhiteSpace(bulgu.Kod));
                Assert.False(string.IsNullOrWhiteSpace(bulgu.Baslik));
            });
        }
    }
}
