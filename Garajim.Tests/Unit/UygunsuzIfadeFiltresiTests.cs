using Garajim.Business.Katalog;

namespace Garajim.Tests.Unit
{
    public class UygunsuzIfadeFiltresiTests
    {
        private static UygunsuzIfadeFiltresi Filtre() =>
            UygunsuzIfadeFiltresi.Yukle(Path.Combine(AppContext.BaseDirectory, UygunsuzIfadeFiltresi.KlasorAdi));

        [Theory]
        [InlineData("orospu")]
        [InlineData("Orospu Çocuğu")]
        [InlineData("piç")]
        [InlineData("amk servisi")]
        [InlineData("bu iş sikik")]
        [InlineData("YARRAK")]
        [InlineData("ibne")]
        [InlineData("puşt")]
        [InlineData("kahpe usta")]
        [InlineData("şerefsiz oto")]
        [InlineData("daşak arabası")]
        public void UygunsuzIfadeYakalanir(string metin)
        {
            Assert.False(Filtre().Temiz(metin));
        }

        [Theory]
        [InlineData("Şişli Oto Servis")]
        [InlineData("Kartal Sanayi")]
        [InlineData("Sarıyer Lastik")]
        [InlineData("Balata değişimi yapıldı")]
        [InlineData("Amasya Otomotiv")]
        [InlineData("Sikke Sokak No 3")]
        [InlineData("Gotik Tasarım Oto Kuaför")]
        [InlineData("Yağ filtresi ve hava filtresi")]
        [InlineData("")]
        [InlineData(null)]
        public void TemizMetinGecer(string metin)
        {
            Assert.True(Filtre().Temiz(metin));
        }

        [Fact]
        public void ListeOtuzIleElliArasindadir()
        {
            var adet = Filtre().Kokler.Count;

            Assert.InRange(adet, 30, 50);
        }

        [Fact]
        public void TurkceKarakterVeBuyukHarfAyirmaz()
        {
            var filtre = Filtre();

            Assert.False(filtre.Temiz("PIÇ"));
            Assert.False(filtre.Temiz("pic"));
            Assert.False(filtre.Temiz("Piç"));
        }

        [Fact]
        public void ListeDosyasiDepodaDuruyor()
        {
            var klasor = new DirectoryInfo(AppContext.BaseDirectory);

            while (klasor != null && !File.Exists(Path.Combine(klasor.FullName, "Garajim.sln")))
            {
                klasor = klasor.Parent;
            }

            Assert.NotNull(klasor);
            Assert.True(File.Exists(Path.Combine(klasor.FullName, "Garajim.Business", "Katalog", UygunsuzIfadeFiltresi.DosyaAdi)));
        }
    }
}
