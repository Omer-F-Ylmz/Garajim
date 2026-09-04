namespace Garajim.Tests.Unit
{
    public class BellekAyarlariTests
    {
        private static string Csproj()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "Garajim.API.csproj"));
        }

        [Fact]
        public void SunucuGcKapali()
        {
            var csproj = Csproj();

            Assert.Contains("<ServerGarbageCollection>false</ServerGarbageCollection>", csproj);
            Assert.Contains("<ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>", csproj);
        }

        [Fact]
        public void YigitSertSiniriTanimli()
        {
            var csproj = Csproj();

            Assert.Contains("System.GC.HeapHardLimit", csproj);

            var baslangic = csproj.IndexOf("System.GC.HeapHardLimit", StringComparison.Ordinal);
            var satir = csproj.Substring(baslangic, Math.Min(160, csproj.Length - baslangic));
            var deger = System.Text.RegularExpressions.Regex.Match(satir, "Value=\"([0-9]+)\"");

            Assert.True(deger.Success, "HeapHardLimit degeri yok: " + satir);

            var bayt = long.Parse(deger.Groups[1].Value);
            Assert.InRange(bayt / (1024 * 1024), 150, 200);
        }
    }
}
