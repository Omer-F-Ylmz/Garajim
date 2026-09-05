namespace Garajim.Tests.Unit
{
    public class IsAkisiSozdizimiTests
    {
        private static readonly string TersBoluN = new string(new[] { '\\', 'n' });

        [Fact]
        public void DesenGercektenTersBoluVeNHarfiniArar()
        {
            Assert.Equal(2, TersBoluN.Length);
            Assert.Equal('\\', TersBoluN[0]);
            Assert.Equal('n', TersBoluN[1]);
        }

        private static string Kok()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok.FullName;
        }

        [Fact]
        public void KabukSatirlariDuzMetinKacisTasimaz()
        {
            var yol = Path.Combine(Kok(), ".github", "workflows", "ci.yml");
            var satirlar = File.ReadAllLines(yol);
            var bozuk = new List<string>();

            for (var i = 0; i < satirlar.Length; i++)
            {
                if (satirlar[i].Contains(TersBoluN, StringComparison.Ordinal))
                {
                    bozuk.Add($"satır {i + 1}: {satirlar[i].Trim()}");
                }
            }

            Assert.True(bozuk.Count == 0,
                "ci.yml icinde duz metin ters bolu-n var (betik üretirken sızmış olabilir): " + string.Join(" | ", bozuk));
        }

        [Fact]
        public void SatirDevamiSonrasindaBaskaKomutGelmez()
        {
            var yol = Path.Combine(Kok(), ".github", "workflows", "ci.yml");

            foreach (var satir in File.ReadAllLines(yol))
            {
                var kirpik = satir.TrimEnd();

                if (!kirpik.EndsWith("\\", StringComparison.Ordinal))
                {
                    continue;
                }

                var govde = kirpik.Substring(0, kirpik.Length - 1);

                Assert.False(govde.Contains(" \\", StringComparison.Ordinal),
                    "satır devamından önce fazladan ters bölü var: " + kirpik.Trim());
            }
        }
    }
}
