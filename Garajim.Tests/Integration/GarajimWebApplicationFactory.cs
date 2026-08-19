using Garajim.Dal.Concrete.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
                    ["RateLimiting:AuthPermitPerMinute"] = "10000"
                });
            });

            builder.ConfigureServices(services =>
            {
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

                services.AddDbContext<GarajimDbContext>(options => options.UseSqlite(_connection));

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
