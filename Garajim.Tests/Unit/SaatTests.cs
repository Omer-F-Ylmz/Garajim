using System.Text.RegularExpressions;
using Garajim.Business.Concrete;

namespace Garajim.Tests.Unit
{
    public class SaatTests
    {
        [Fact]
        public void AyBasiTurkiyeAyininIlkGunudur()
        {
            var ayBasi = Saat.AyBasiUtc();
            var yerel = Saat.Yerel(ayBasi);

            Assert.Equal(DateTimeKind.Utc, ayBasi.Kind);
            Assert.Equal(1, yerel.Day);
            Assert.Equal(TimeSpan.Zero, yerel.TimeOfDay);
            Assert.Equal(Saat.BugunTr().Month, yerel.Month);
            Assert.Equal(Saat.BugunTr().Year, yerel.Year);
        }

        [Fact]
        public void AyBasiGunBasindanSonraOlamaz()
        {
            Assert.True(Saat.AyBasiUtc() <= Saat.GunBasiUtc());
        }

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

        [Fact]
        public void TurkiyeDilimiCozulur()
        {
            Assert.Equal(TimeSpan.FromHours(3), Saat.Dilim.GetUtcOffset(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc)));
            Assert.Equal(TimeSpan.FromHours(3), Saat.Dilim.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void GeceYarisindanOnceUtcOncekiGundeykenTrAyniGundedir()
        {
            var utc = new DateTime(2026, 3, 10, 20, 30, 0, DateTimeKind.Utc);

            var yerel = Saat.Yerel(utc);

            Assert.Equal(new DateTime(2026, 3, 10, 23, 30, 0), yerel);
            Assert.Equal(new DateTime(2026, 3, 10), yerel.Date);
        }

        [Fact]
        public void UtcGunuDonmedenTrErtesiGuneGecer()
        {
            var utc = new DateTime(2026, 3, 10, 21, 30, 0, DateTimeKind.Utc);

            var yerel = Saat.Yerel(utc);

            Assert.Equal(new DateTime(2026, 3, 11, 0, 30, 0), yerel);
            Assert.Equal(new DateTime(2026, 3, 11), yerel.Date);
        }

        [Fact]
        public void VarsayilanSaatTrGununuVerir()
        {
            var bugun = Saat.Varsayilan.Bugun;

            Assert.Equal(bugun, Saat.Yerel(DateTime.UtcNow).Date);
            Assert.Equal(default, bugun.TimeOfDay);
        }

        [Fact]
        public void UrunKodundaYerelSaatKullanilmaz()
        {
            var kok = DepoKoku();
            var bulgular = new List<string>();

            foreach (var proje in new[] { "Garajim.API", "Garajim.Business", "Garajim.Dal", "Garajim.Core" })
            {
                var klasor = Path.Combine(kok, proje);
                if (!Directory.Exists(klasor))
                {
                    continue;
                }

                foreach (var dosya in Directory.GetFiles(klasor, "*.cs", SearchOption.AllDirectories))
                {
                    if (dosya.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        || dosya.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        || dosya.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
                    {
                        continue;
                    }

                    var metin = File.ReadAllText(dosya);

                    if (Regex.IsMatch(metin, @"DateTime\.Now\b") || Regex.IsMatch(metin, @"DateTime\.Today\b"))
                    {
                        bulgular.Add(Path.GetFileName(dosya));
                    }
                }
            }

            Assert.True(bulgular.Count == 0,
                "Yerel saat kullanımı bulundu (UTC ya da Saat kullanın): " + string.Join(", ", bulgular));
        }
    }
}
