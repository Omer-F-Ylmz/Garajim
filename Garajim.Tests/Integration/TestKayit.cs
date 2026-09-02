using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public static class TestKayit
    {
        public static async Task<string> TokenAl(HttpClient client, HttpResponseMessage kayitCevabi)
        {
            var ham = await kayitCevabi.Content.ReadAsStringAsync();
            var govde = JsonDocument.Parse(ham).RootElement;

            if (!govde.TryGetProperty("data", out var veri) || veri.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Kayıt başarısız: " + ham);
            }

            if (veri.TryGetProperty("token", out var hazir) && hazir.ValueKind == JsonValueKind.String)
            {
                return hazir.GetString();
            }

            var eposta = veri.GetProperty("email").GetString();
            var kod = SahteEpostaGonderici.Ortak.SonKod(eposta)
                      ?? throw new InvalidOperationException("Doğrulama kodu e-postası yakalanamadı: " + eposta);

            var dogrula = await client.PostAsJsonAsync("/api/Auth/dogrula", new { email = eposta, kod });
            var dogrulaGovde = await dogrula.Content.ReadAsStringAsync();

            if (!dogrula.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Doğrulama başarısız: " + dogrulaGovde);
            }

            return JsonDocument.Parse(dogrulaGovde).RootElement.GetProperty("data").GetProperty("token").GetString();
        }
    }
}
