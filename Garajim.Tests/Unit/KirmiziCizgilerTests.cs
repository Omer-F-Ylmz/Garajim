using Garajim.Business.Usta;

namespace Garajim.Tests.Unit
{
    public class KirmiziCizgilerTests
    {
        [Theory]
        [InlineData("Fren pedalı yere kadar gidiyor, hiç tutmuyor.", "FrenBosaldi")]
        [InlineData("frene basıyorum fren tutmuyor", "FrenBosaldi")]
        [InlineData("Pedal yere dayanıyor, fren boşaldı gibi.", "FrenBosaldi")]
        [InlineData("Aracın freni yok, hiç durmuyor.", "FrenBosaldi")]
        [InlineData("Direksiyon kilitlendi, hiç dönmüyor.", "DireksiyonKilit")]
        [InlineData("Direksiyon çok ağırlaştı ve çevirince ses geliyor.", "DireksiyonKilit")]
        [InlineData("Çevirirken gıcırtı sesi var, direksiyon çok sert.", "DireksiyonKilit")]
        [InlineData("Panelde kırmızı yağ lambası yandı.", "KirmiziLamba")]
        [InlineData("kırmızı hararet ikazı yanıyor", "KirmiziLamba")]
        [InlineData("Şarj lambası yandı, rengi kırmızı.", "KirmiziLamba")]
        [InlineData("Araç hararet yaptı, ibre tavana vurdu.", "Hararet")]
        [InlineData("Kaputtan buhar çıkıyor.", "Hararet")]
        [InlineData("Radyatör kaynıyor, sıcaklık ibresi kırmızıda.", "Hararet")]
        [InlineData("Araçta çok yoğun benzin kokusu var.", "YakitKokusu")]
        [InlineData("Altından mazot damlıyor, koku da geliyor.", "YakitKokusu")]
        [InlineData("Kabine duman doluyor.", "KabinDumani")]
        [InlineData("İçeriye duman geliyor, yanık plastik kokuyor.", "KabinDumani")]
        [InlineData("Motordan metal sesi geliyor ve araç titriyor.", "MetalSesiTitreme")]
        [InlineData("Araç titriyor, altından demir sesi var.", "MetalSesiTitreme")]
        [InlineData("Otoyolda giderken araç stop etti.", "SeyirdeStop")]
        [InlineData("Seyir hâlinde stop ediyor sürekli.", "SeyirdeStop")]
        public void KirmiziCizgiCumleleriYakalanir(string cumle, string beklenenKod)
        {
            var bulgu = KirmiziCizgiler.Bul(cumle);

            Assert.NotNull(bulgu);
            Assert.Equal(beklenenKod, bulgu.Kod);
        }

        [Theory]
        [InlineData("Fren balatalarım ne zaman değişmeli?")]
        [InlineData("Fren diski torna yapılır mı?")]
        [InlineData("Fren hidroliği kaç yılda bir değişir?")]
        [InlineData("Direksiyon ayarı için ne kadar ücret alınır?")]
        [InlineData("Direksiyon simidi kılıfı arıyorum.")]
        [InlineData("Panelde motor arıza lambası sarı yanıyor.")]
        [InlineData("Yağ değişim lambası yandı, bakım zamanı gelmiş.")]
        [InlineData("Klima gazı bitmiş galiba, soğutmuyor.")]
        [InlineData("Egzozdan hafif mavi duman geliyor.")]
        [InlineData("Egzozdan beyaz duman çıkıyor sabahları.")]
        [InlineData("Depoyu doldurunca yakıt tüketimi nasıl ölçülür?")]
        [InlineData("Benzin fiyatları ne kadar oldu?")]
        [InlineData("Bozuk yolda tak tak sesi geliyor.")]
        [InlineData("Rölantide hafif titreme var.")]
        [InlineData("Araç rölantide stop ediyor bazen.")]
        [InlineData("Muayene için hangi evrakları götürmeliyim?")]
        [InlineData("Kış lastiği ne zaman zorunlu oluyor?")]
        [InlineData("Akü kaç yılda bir değişir?")]
        [InlineData("Triger kayışı kaç kilometrede değişir?")]
        [InlineData("Şanzıman yağı değişimi ne kadar tutar?")]
        public void NormalSorularKirmiziCizgiSaymaz(string cumle)
        {
            Assert.Null(KirmiziCizgiler.Bul(cumle));
        }

        [Fact]
        public void BosMetinKirmiziCizgiDegildir()
        {
            Assert.Null(KirmiziCizgiler.Bul(null));
            Assert.Null(KirmiziCizgiler.Bul("   "));
        }

        [Fact]
        public void SabitCevapSurmemeUyarisiVeCekiciOnerisiIcerir()
        {
            Assert.Contains("yola çıkma", KirmiziCizgiler.Cevap);
            Assert.Contains("çekici", KirmiziCizgiler.Cevap);
        }
    }
}
