using System.Text.RegularExpressions;

namespace Garajim.Tests.Unit
{
    public class BosCatchTests
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

        private static IEnumerable<string> UrunDosyalari()
        {
            var kok = DepoKoku();

            foreach (var proje in new[] { "Garajim.Core", "Garajim.Entity", "Garajim.Dal", "Garajim.Business", "Garajim.API", "Garajim.ML" })
            {
                var klasor = Path.Combine(kok, proje);

                if (!Directory.Exists(klasor))
                {
                    continue;
                }

                foreach (var dosya in Directory.GetFiles(klasor, "*.cs", SearchOption.AllDirectories))
                {
                    if (dosya.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                        || dosya.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        || dosya.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar))
                    {
                        continue;
                    }

                    yield return dosya;
                }
            }
        }

        private static readonly Regex BosCatch =
            new Regex(@"catch\s*(\([^)]*\))?\s*\{\s*\}", RegexOptions.Compiled);

        [Fact]
        public void UrunKodundaBosCatchYok()
        {
            var bulunanlar = new List<string>();

            foreach (var dosya in UrunDosyalari())
            {
                var metin = File.ReadAllText(dosya);

                foreach (Match eslesme in BosCatch.Matches(metin))
                {
                    var satir = metin.Take(eslesme.Index).Count(k => k == '\n') + 1;
                    bulunanlar.Add(Path.GetFileName(dosya) + ":" + satir);
                }
            }

            Assert.True(bulunanlar.Count == 0, "Gövdesi boş catch: " + string.Join(", ", bulunanlar));
        }

        [Fact]
        public void BekciGercektenBosCatchiYakalar()
        {
            Assert.Matches(BosCatch, "try { X(); } catch { }");
            Assert.Matches(BosCatch, "try { X(); } catch (Exception) {\n            }");
            Assert.DoesNotMatch(BosCatch, "try { X(); } catch (Exception e) { Log(e); }");
        }
    }
}
