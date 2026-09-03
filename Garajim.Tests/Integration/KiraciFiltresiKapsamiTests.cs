using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class KiraciFiltresiKapsamiTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public KiraciFiltresiKapsamiTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private GarajimDbContext Baglam(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<GarajimDbContext>();

        [Fact]
        public void CompanyIdTasiyanHerEntityFiltreVeIndeksTasir()
        {
            using var scope = _factory.Services.CreateScope();
            var model = Baglam(scope).Model;

            var filtresizler = new List<string>();
            var indekssizler = new List<string>();

            foreach (var tur in model.GetEntityTypes())
            {
                if (tur.FindProperty("CompanyId") == null)
                {
                    continue;
                }

                if (tur.GetQueryFilter() == null)
                {
                    filtresizler.Add(tur.ClrType.Name);
                }

                var indeksli = tur.GetIndexes()
                    .Any(i => i.Properties.Any(p => p.Name == "CompanyId"));

                if (!indeksli)
                {
                    indekssizler.Add(tur.ClrType.Name);
                }
            }

            Assert.True(filtresizler.Count == 0, "HasQueryFilter eksik: " + string.Join(", ", filtresizler));
            Assert.True(indekssizler.Count == 0, "CompanyId indeksi eksik: " + string.Join(", ", indekssizler));
        }

        [Fact]
        public void AnonimOgrenmeTablosuBilerekKiracisizdir()
        {
            using var scope = _factory.Services.CreateScope();
            var tur = Baglam(scope).Model.FindEntityType(typeof(UstaCozumOzeti));

            Assert.NotNull(tur);
            Assert.Null(tur.FindProperty("CompanyId"));
        }

        [Fact]
        public void KmDuzeltmeLoguSirketiniAsmaz()
        {
            using var scope = _factory.Services.CreateScope();
            var tur = Baglam(scope).Model.FindEntityType(typeof(KmDuzeltmeLog));

            Assert.NotNull(tur);
            Assert.NotNull(tur.FindProperty("CompanyId"));
            Assert.NotNull(tur.GetQueryFilter());
        }
    }
}
