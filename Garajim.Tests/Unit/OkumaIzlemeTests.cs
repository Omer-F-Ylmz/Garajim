using System.Text.RegularExpressions;

namespace Garajim.Tests.Unit
{
    public class OkumaIzlemeTests
    {
        private static string DepoKoku()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok.FullName;
        }

        private static string Oku(params string[] parcalar) =>
            File.ReadAllText(Path.Combine(new[] { DepoKoku() }.Concat(parcalar).ToArray()));

        [Fact]
        public void AktifZimmetOkumasiTakipsizdir()
        {
            var metin = Oku("Garajim.Dal", "Concrete", "EfVehicleAssignmentDal.cs");
            var bas = metin.IndexOf("GetActiveByVehicleAsync", StringComparison.Ordinal);

            Assert.True(bas > 0);

            var govde = metin.Substring(bas, 260);

            Assert.Contains("AsNoTracking()", govde);
        }

        [Fact]
        public void BilincliIzlemeliYazmaYollariKorunur()
        {
            var fuel = Oku("Garajim.Dal", "Concrete", "EfFuelDal.cs");
            var token = Oku("Garajim.Dal", "Concrete", "EfAiTokenDal.cs");

            var supheli = fuel.Substring(fuel.IndexOf("SupheliGuncelleAsync", StringComparison.Ordinal), 900);

            Assert.DoesNotContain("AsNoTracking", supheli);
            Assert.Contains("SaveChangesAsync", supheli);
            Assert.Contains("SaveChangesAsync", token);
        }
    }
}
