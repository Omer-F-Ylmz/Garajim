using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class SwaggerHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public SwaggerHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<JsonElement> DokumanAsync()
        {
            var cevap = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.True(cevap.IsSuccessStatusCode, "Swagger dokümanı üretilemedi: " + govde);

            return JsonDocument.Parse(govde).RootElement;
        }

        [Fact]
        public async Task SwaggerDokumaniUretilir()
        {
            var dokuman = await DokumanAsync();

            Assert.True(dokuman.TryGetProperty("paths", out _));
        }

        [Theory]
        [InlineData("/api/Documents")]
        [InlineData("/api/Assignments")]
        [InlineData("/api/Team")]
        public async Task YeniUcNoktalarDokumandaGorunur(string yol)
        {
            var paths = (await DokumanAsync()).GetProperty("paths");

            Assert.True(paths.TryGetProperty(yol, out _), yol + " swagger dokümanında yok.");
        }

        [Fact]
        public async Task SwaggerGovdesiSirIcermez()
        {
            var cevap = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
            var govde = (await cevap.Content.ReadAsStringAsync()).ToLowerInvariant();

            Assert.DoesNotContain("localdb", govde);
            Assert.DoesNotContain("trusted_connection", govde);
            Assert.DoesNotContain("dev-ortami", govde);
            Assert.DoesNotContain("demo1234", govde);
            Assert.DoesNotContain("surucu1234", govde);
        }
    }
}
