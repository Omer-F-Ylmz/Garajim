using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Garajim.Tests.Integration
{
    public class SwaggerUretimKapisiTests : IDisposable
    {
        private static readonly Dictionary<string, string> UretimDegiskenleri = new Dictionary<string, string>
        {
            ["ConnectionStrings__Default"] = "Server=uzak-sunucu;Database=Garajim;User Id=garajim;Password=testte-kullanilan-deger;",
            ["Jwt__Key"] = "uretim-testi-icin-en-az-32-karakterlik-anahtar",
            ["Smtp__Host"] = "smtp.ornek.test",
            ["Smtp__User"] = "gonderen@ornek.test",
            ["Smtp__From"] = "gonderen@ornek.test",
            ["Smtp__Pass"] = "smtp-testi"
        };

        private readonly Dictionary<string, string> _oncekiler = new Dictionary<string, string>();

        public SwaggerUretimKapisiTests()
        {
            foreach (var giris in UretimDegiskenleri)
            {
                _oncekiler[giris.Key] = Environment.GetEnvironmentVariable(giris.Key);
                Environment.SetEnvironmentVariable(giris.Key, giris.Value);
            }
        }

        public void Dispose()
        {
            foreach (var giris in _oncekiler)
            {
                Environment.SetEnvironmentVariable(giris.Key, giris.Value);
            }

            Environment.SetEnvironmentVariable("Swagger__Enabled", null);
        }

        private sealed class UretimFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.UseEnvironment("Production");
            }
        }

        [Fact]
        public async Task UretimdeSwaggerKapalidir()
        {
            Environment.SetEnvironmentVariable("Swagger__Enabled", null);
            using var factory = new UretimFactory();

            var cevap = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task UretimdeAyarlaAcilabilir()
        {
            Environment.SetEnvironmentVariable("Swagger__Enabled", "true");
            using var factory = new UretimFactory();

            var cevap = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }
    }
}
