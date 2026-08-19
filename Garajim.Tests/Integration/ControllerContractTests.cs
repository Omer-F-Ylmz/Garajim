using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Garajim.Tests.Integration
{
    public class ControllerContractTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ControllerContractTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        public static IEnumerable<object[]> KorumaliGetUclari()
        {
            yield return new object[] { "/api/Vehicles" };
            yield return new object[] { "/api/Vehicles/1" };
            yield return new object[] { "/api/Maintenance?vehicleId=1" };
            yield return new object[] { "/api/Fuel?vehicleId=1" };
            yield return new object[] { "/api/Expenses?vehicleId=1" };
            yield return new object[] { "/api/Reminders?vehicleId=1" };
            yield return new object[] { "/api/Reminders/upcoming?days=30" };
            yield return new object[] { "/api/Reports/summary?vehicleId=1&start=2026-01-01&end=2026-12-31" };
            yield return new object[] { "/api/Reports/monthly?vehicleId=1" };
            yield return new object[] { "/api/Reports/fuel-stats?vehicleId=1" };
        }

        public static IEnumerable<object[]> KorumaliPostUclari()
        {
            yield return new object[] { "/api/Vehicles" };
            yield return new object[] { "/api/Maintenance" };
            yield return new object[] { "/api/Fuel" };
            yield return new object[] { "/api/Expenses" };
            yield return new object[] { "/api/Reminders" };
            yield return new object[] { "/api/price/estimate" };
        }

        [Theory]
        [MemberData(nameof(KorumaliGetUclari))]
        public async Task TokensizGetIstekleri401Doner(string path)
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(KorumaliPostUclari))]
        public async Task TokensizPostIstekleri401Doner(string path)
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync(path, new StringContent("{}", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TokensizYazmaVeSilmeUclari401Doner()
        {
            var client = _factory.CreateClient();

            var complete = await client.PutAsync("/api/Reminders/1/complete", null);
            var deleteVehicle = await client.DeleteAsync("/api/Vehicles/1");
            var deleteFuel = await client.DeleteAsync("/api/Fuel/1");

            Assert.Equal(HttpStatusCode.Unauthorized, complete.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, deleteVehicle.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, deleteFuel.StatusCode);
        }

        [Fact]
        public async Task GecersizTokenIle401Doner()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer gecersiz.token.degeri");

            var response = await client.GetAsync("/api/Vehicles");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthUclariTokensizErisilebilir()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Auth/login", new { email = "yok@garajim.local", password = "yanlis123" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var payload = await response.Content.ReadAsStringAsync();
            Assert.Contains("E-posta veya şifre hatalı.", payload);
        }

        [Fact]
        public async Task BozukJsonGovdesi400Doner()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/Auth/register",
                new StringContent("{ bu gecerli json degil", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task YanlisTiptekiAlan400Doner()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/Auth/register",
                new StringContent("{\"email\":\"a@b.com\",\"fullName\":\"Test\",\"password\":12345}", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var payload = await response.Content.ReadAsStringAsync();
            Assert.Contains("password", payload, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TanimsizEnumMetniIle400Doner()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/Auth/register",
                new StringContent("{\"email\":\"a@b.com\",\"fullName\":\"Test\",\"password\":\"gizli123\",\"extra\":}", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task KokAdresUygulamayiTokensizServisEder()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
            Assert.Contains("<title>Garajım</title>", body);
        }

        [Fact]
        public async Task StatikDosyalarTokensizServisEdilir()
        {
            var client = _factory.CreateClient();

            var script = await client.GetAsync("/app.js");
            var style = await client.GetAsync("/styles.css");

            Assert.Equal(HttpStatusCode.OK, script.StatusCode);
            Assert.Equal(HttpStatusCode.OK, style.StatusCode);
        }
    }
}
