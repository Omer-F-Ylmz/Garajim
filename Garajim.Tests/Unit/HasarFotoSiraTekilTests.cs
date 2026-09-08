using Garajim.Entity.Concrete;
using Garajim.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Unit
{
    public class HasarFotoSiraTekilTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public void Dispose()
        {
            _db.Dispose();
        }

        [Fact]
        public void DosyaVeSiraCiftiTekildir()
        {
            var indeksler = _db.Context.Model.FindEntityType(typeof(HasarFoto)).GetIndexes().ToList();

            var tekil = indeksler.FirstOrDefault(i => i.IsUnique
                && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "HasarDosyasiId", "Sira" }));

            Assert.NotNull(tekil);
        }

        [Fact]
        public void FotoEklemeSayimVeYazmayiAyniIslemdeYapar()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            var kaynak = File.ReadAllText(Path.Combine(kok.FullName,
                "Garajim.Business", "Concrete", "HasarManager.cs"));

            var bas = kaynak.IndexOf("public async Task<IDataResult<HasarFotoDto>> FotoEkleAsync", StringComparison.Ordinal);
            var son = kaynak.IndexOf("private async Task YuklemeyiGeriAlAsync", bas, StringComparison.Ordinal);
            var govde = kaynak.Substring(bas, son - bas);

            var islem = govde.IndexOf("BeginTransactionAsync", StringComparison.Ordinal);
            var sayim = govde.IndexOf("_fotoDal.SayiAsync(id) >= MaxFoto", islem, StringComparison.Ordinal);
            var yazma = govde.IndexOf("_fotoDal.AddAsync(foto)", sayim, StringComparison.Ordinal);

            Assert.True(islem > 0 && sayim > islem && yazma > sayim,
                "sayım ile yazma arasında işlem sınırı yok");
        }
    }
}
