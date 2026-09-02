using Garajim.Business.Usta;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class UstaWebApplicationFactory : GarajimWebApplicationFactory
    {
        public SahteUstaIstemci Istemci { get; } = new SahteUstaIstemci();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                var mevcut = services.Where(d => d.ServiceType == typeof(IUstaIstemci)).ToList();
                foreach (var kayit in mevcut)
                {
                    services.Remove(kayit);
                }

                services.AddSingleton<IUstaIstemci>(Istemci);
            });
        }
    }
}
