using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Dal.Concrete.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class UyeHesapSilmeTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose() => _factory.Dispose();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Sahip, string SahipEposta)> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var eposta = Eposta(on);
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Ekip Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, eposta);
        }

        private async Task<(HttpClient Client, string Eposta, int UserId)> SurucuAsync(HttpClient sahip)
        {
            var eposta = Eposta("surucu");
            var ekle = await sahip.PostAsJsonAsync("/api/Team",
                new { email = eposta, fullName = "Silinecek Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login",
                new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return (client, eposta, veri.GetProperty("userId").GetInt32());
        }

        private T Oku<T>(Func<GarajimDbContext, T> islem)
        {
            using var scope = _factory.Services.CreateScope();
            return islem(scope.ServiceProvider.GetRequiredService<GarajimDbContext>());
        }

        [Fact]
        public async Task SurucuKendiHesabiniSilerVeKisiselBilgisiKalmaz()
        {
            var (sahip, sahipEposta) = await SahipAsync("uyesil");
            var (surucu, surucuEposta, userId) = await SurucuAsync(sahip);

            var cevap = await surucu.DeleteAsync("/api/Account");
            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            var kayit = Oku(c => c.Users.IgnoreQueryFilters().Single(u => u.Id == userId));

            Assert.False(kayit.IsActive);
            Assert.Equal("Silinmiş kullanıcı", kayit.FullName);
            Assert.DoesNotContain(surucuEposta, kayit.Email);
            Assert.Empty(kayit.PasswordHash);

            var sahipDurur = Oku(c => c.Users.IgnoreQueryFilters().Any(u => u.Email == sahipEposta));
            Assert.True(sahipDurur);
        }

        [Fact]
        public async Task SilinenUyeninTokeniIleIstekReddedilir()
        {
            var (sahip, _) = await SahipAsync("token");
            var (surucu, _, _) = await SurucuAsync(sahip);

            await surucu.DeleteAsync("/api/Account");

            var sonra = await surucu.GetAsync("/api/Vehicles");

            Assert.Equal(HttpStatusCode.Unauthorized, sonra.StatusCode);
        }

        [Fact]
        public async Task SahipBuUctanHesabiniSilemez()
        {
            var (sahip, _) = await SahipAsync("sahipsil");

            var cevap = await sahip.DeleteAsync("/api/Account");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task SilinenUyeIcinSahibeEpostaGider()
        {
            var (sahip, sahipEposta) = await SahipAsync("bildirim");
            var oncekiSayi = SahteEpostaGonderici.Ortak.SayiOf(sahipEposta);
            var (surucu, _, _) = await SurucuAsync(sahip);

            await surucu.DeleteAsync("/api/Account");

            Assert.True(SahteEpostaGonderici.Ortak.SayiOf(sahipEposta) > oncekiSayi);
        }
    }
}
