using System.Text.RegularExpressions;

namespace Garajim.Tests.Unit
{
    public class Denetim2FiltreIstisnaTests
    {
        private static readonly Dictionary<string, int> IzinliKullanim = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["EfUserDal.cs"] = 2,
            ["EfKarnePaylasimiDal.cs"] = 2,
            ["EfTakvimAbonelikDal.cs"] = 1,
            ["EfCompanyDal.cs"] = 4
        };

        private static string DepoKoku()
        {
            var klasor = new DirectoryInfo(AppContext.BaseDirectory);

            while (klasor != null && !File.Exists(Path.Combine(klasor.FullName, "Garajim.sln")))
            {
                klasor = klasor.Parent;
            }

            Assert.NotNull(klasor);
            return klasor.FullName;
        }

        private static Dictionary<string, int> GercekKullanim()
        {
            var kok = DepoKoku();
            var sonuc = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var proje in new[] { "Garajim.Dal", "Garajim.Business", "Garajim.API", "Garajim.Core" })
            {
                var klasor = Path.Combine(kok, proje);
                if (!Directory.Exists(klasor))
                {
                    continue;
                }

                foreach (var dosya in Directory.GetFiles(klasor, "*.cs", SearchOption.AllDirectories))
                {
                    if (dosya.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                        dosya.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                    {
                        continue;
                    }

                    var sayi = Regex.Matches(File.ReadAllText(dosya), @"IgnoreQueryFilters\(\)").Count;
                    if (sayi > 0)
                    {
                        sonuc[Path.GetFileName(dosya)] = sayi;
                    }
                }
            }

            return sonuc;
        }

        [Fact]
        public void IgnoreQueryFiltersYalnizIzinliDosyalardaVeSayidaKullanilir()
        {
            var gercek = GercekKullanim();

            var fazla = gercek.Keys.Except(IzinliKullanim.Keys).ToList();
            Assert.True(fazla.Count == 0, "İzin listesinde olmayan dosyada filtre atlanıyor: " + string.Join(", ", fazla));

            foreach (var izinli in IzinliKullanim)
            {
                gercek.TryGetValue(izinli.Key, out var sayi);
                Assert.True(sayi == izinli.Value,
                    $"{izinli.Key} içinde beklenen {izinli.Value} kullanım yerine {sayi} bulundu.");
            }
        }

        [Fact]
        public void ClaudeMdIstisnaListesiKodlaBirebir()
        {
            var claudeMd = File.ReadAllText(Path.Combine(DepoKoku(), "CLAUDE.md"));

            foreach (var dosya in IzinliKullanim.Keys)
            {
                var ad = Path.GetFileNameWithoutExtension(dosya);
                Assert.True(claudeMd.Contains(ad, StringComparison.Ordinal),
                    $"CLAUDE.md filtre istisna listesinde {ad} anılmıyor.");
            }
        }
    }
}
