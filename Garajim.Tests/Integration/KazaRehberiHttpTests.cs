using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Concrete;

namespace Garajim.Tests.Integration
{
    public class KazaRehberiHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public KazaRehberiHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("rehber"), fullName = "Rehber Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task RehberDoluDoner()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await sahip.GetAsync("/api/Hasar/rehber");
            var rehber = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(rehber.GetProperty("ozet").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(rehber.GetProperty("bildirimSuresi").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(rehber.GetProperty("kaynak").GetString()));

            Assert.True(rehber.GetProperty("anlasmaliTutanakKosullari").GetArrayLength() >= 5);
            Assert.True(rehber.GetProperty("polisGerekliHaller").GetArrayLength() >= 5);
            Assert.True(rehber.GetProperty("fotografListesi").GetArrayLength() >= 4);
            Assert.True(rehber.GetProperty("alinacakBilgiler").GetArrayLength() >= 4);

            var adimlar = rehber.GetProperty("adimlar");
            Assert.True(adimlar.GetArrayLength() >= 4);

            foreach (var adim in adimlar.EnumerateArray())
            {
                Assert.False(string.IsNullOrWhiteSpace(adim.GetProperty("baslik").GetString()));
                Assert.True(adim.GetProperty("maddeler").GetArrayLength() > 0);

                foreach (var madde in adim.GetProperty("maddeler").EnumerateArray())
                {
                    Assert.False(string.IsNullOrWhiteSpace(madde.GetString()));
                }
            }
        }

        [Fact]
        public async Task RehberKimliksizIstegeKapali()
        {
            var anonim = _factory.CreateClient();

            var cevap = await anonim.GetAsync("/api/Hasar/rehber");

            Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        }

        [Fact]
        public void RehberYasalKaynagiTasir()
        {
            var rehber = KazaRehberi.Olustur();

            Assert.Equal(KazaRehberi.KaynakNotu, rehber.Kaynak);
            Assert.Contains("Karayolları Trafik Kanunu", rehber.Kaynak);
            Assert.Contains("Zorunlu Mali Sorumluluk Sigortası", rehber.Kaynak);
            Assert.Contains("5 iş günü", rehber.BildirimSuresi);
            Assert.Contains("112", rehber.Ozet);
        }
    }
}
