using Garajim.Business.Concrete;
using Garajim.Core.Utilities.Security;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class EmailDogrulamaVeriTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        public void Dispose()
        {
            _db.Dispose();
        }

        [Fact]
        public void YeniKullaniciVarsayilanOlarakDogrulanmamis()
        {
            var kullanici = new AppUser();

            Assert.False(kullanici.EmailDogrulandi);
            Assert.Null(kullanici.DogrulamaKodHash);
            Assert.Null(kullanici.DogrulamaKodSonTarih);
            Assert.Equal(0, kullanici.DogrulamaDenemeSayisi);
            Assert.Null(kullanici.SonKodGonderim);
        }

        [Fact]
        public async Task DemoSeedKullanicilariDogrulanmisGelir()
        {
            await _db.DemoSeeder().RunAsync();

            var kullanicilar = await _db.Context.Users.IgnoreQueryFilters().ToListAsync();

            Assert.Equal(2, kullanicilar.Count);
            Assert.All(kullanicilar, k => Assert.True(k.EmailDogrulandi, k.Email + " doğrulanmamış geldi."));
        }

        [Fact]
        public async Task OwnerinDavetEttigiSurucuDogrulanmisAcilir()
        {
            var sahip = SahipEkle();
            var yonetici = new TeamManager(_db.UserDal, _db.EvrakDal, _db.AssignmentDal);

            var sonuc = await yonetici.AddAsync(sahip.Id, new TeamMemberCreateDto
            {
                Email = "davetli-surucu@garajim.local",
                FullName = "Davetli Sürücü",
                Role = CompanyRole.Driver
            });

            Assert.True(sonuc.Success, sonuc.Message);

            var surucu = await _db.Context.Users.IgnoreQueryFilters()
                .SingleAsync(u => u.Email == "davetli-surucu@garajim.local");

            Assert.True(surucu.EmailDogrulandi);
            Assert.Null(surucu.DogrulamaKodHash);
        }

        [Fact]
        public async Task MigrationMevcutSatirlariDogrulanmisYaparVeYalnizEklemeli()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);

            var dosya = Directory
                .GetFiles(Path.Combine(kok.FullName, "Garajim.Dal", "Migrations"), "*_EmailDogrulama.cs")
                .SingleOrDefault(d => !d.EndsWith(".Designer.cs", StringComparison.Ordinal));

            Assert.NotNull(dosya);

            var icerik = await File.ReadAllTextAsync(dosya);
            var up = icerik.Substring(icerik.IndexOf("protected override void Up", StringComparison.Ordinal));
            var upGovde = up.Substring(0, up.IndexOf("protected override void Down", StringComparison.Ordinal));

            Assert.Contains("EmailDogrulandi", upGovde);
            Assert.Contains("defaultValue: false", upGovde);
            Assert.Contains("UPDATE Users SET EmailDogrulandi = 1", upGovde);
            Assert.Contains("DogrulamaKodHash", upGovde);
            Assert.Contains("DogrulamaKodSonTarih", upGovde);
            Assert.Contains("DogrulamaDenemeSayisi", upGovde);
            Assert.Contains("SonKodGonderim", upGovde);

            foreach (var yasak in new[] { "DropColumn", "DropTable", "AlterColumn", "RenameColumn" })
            {
                Assert.DoesNotContain(yasak, upGovde);
            }
        }

        private AppUser SahipEkle()
        {
            HashingHelper.CreatePasswordHash("Test1234!", out var hash, out var salt);

            var sahip = new AppUser
            {
                CompanyId = _db.CompanyId,
                Role = CompanyRole.Owner,
                IsActive = true,
                EmailDogrulandi = true,
                Email = "davet-sahip@garajim.local",
                FullName = "Davet Sahibi",
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            };

            _db.Context.Users.Add(sahip);
            _db.Context.SaveChanges();
            return sahip;
        }
    }
}
