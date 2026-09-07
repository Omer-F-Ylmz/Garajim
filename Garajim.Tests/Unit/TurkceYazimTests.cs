using System.Reflection;
using Garajim.Business.Constants;

namespace Garajim.Tests.Unit
{
    public class TurkceYazimTests
    {
        private static string Oku(string dosya)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", dosya));
        }

        private static IEnumerable<(string Ad, string Deger)> Sabitler()
        {
            foreach (var alan in typeof(Messages).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (alan.IsLiteral && alan.FieldType == typeof(string))
                {
                    yield return (alan.Name, (string)alan.GetRawConstantValue());
                }
            }
        }

        [Fact]
        public void MesajlardaLastikYazimiDogru()
        {
            var hatali = Sabitler()
                .Where(s => s.Deger.Contains("lastigi", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Ad)
                .ToList();

            Assert.True(hatali.Count == 0, "Eksik harfli 'lastigi': " + string.Join(", ", hatali));
        }

        [Fact]
        public void MesajlarRolKoduSizdirmaz()
        {
            var hatali = new List<string>();

            foreach (var sabit in Sabitler())
            {
                foreach (var rol in new[] { "Owner", "Manager", "Driver" })
                {
                    if (sabit.Deger.Contains(rol, StringComparison.Ordinal))
                    {
                        hatali.Add(sabit.Ad + " -> " + rol);
                    }
                }
            }

            Assert.True(hatali.Count == 0, "Kullanıcı metninde rol kodu: " + string.Join(", ", hatali));
        }

        [Fact]
        public void GrafikHatasiTekSozcukKullanir()
        {
            var app = Oku("app.js");

            Assert.DoesNotContain("kütüphane", app);
            Assert.Contains("kitaplığı", app);
        }

        [Fact]
        public void YuklemeIpuclariAyniUzantiListesiniYazar()
        {
            var html = Oku("index.html");

            Assert.DoesNotContain("jpg, png", html);
        }

        [Fact]
        public void MesajlarBosDegil()
        {
            Assert.All(Sabitler(), s => Assert.False(string.IsNullOrWhiteSpace(s.Deger), s.Ad + " boş"));
        }
    }
}
