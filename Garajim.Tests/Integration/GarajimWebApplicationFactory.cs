using Garajim.Business.Abstract;
using Garajim.Dal.Concrete.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class GarajimWebApplicationFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection _connection;

        public GarajimWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable("UseBackgroundJobs", "false");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["RateLimiting:AuthPermitPerMinute"] = "10000",
                    ["Jwt:Key"] = "test-ortami-icin-en-az-32-karakterlik-gizli-anahtar",
                    ["Jwt:Issuer"] = "Garajim",
                    ["Jwt:Audience"] = "GarajimClient",
                    ["Jwt:ExpireDays"] = "7"
                });
            });

            builder.ConfigureServices(services =>
            {
                foreach (var epostaKaydi in services.Where(d => d.ServiceType == typeof(IEmailSender)).ToList())
                {
                    services.Remove(epostaKaydi);
                }

                services.AddSingleton<IEmailSender>(SahteEpostaGonderici.Ortak);

                var existing = services
                    .Where(descriptor => descriptor.ServiceType == typeof(DbContextOptions<GarajimDbContext>)
                                         || descriptor.ServiceType == typeof(GarajimDbContext))
                    .ToList();

                foreach (var descriptor in existing)
                {
                    services.Remove(descriptor);
                }

                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<GarajimDbContext>(options => options
                    .UseSqlite(_connection)
                    .ReplaceService<IModelCustomizer, SqliteDecimalModelCustomizer>());

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                scope.ServiceProvider.GetRequiredService<GarajimDbContext>().Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && _connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
