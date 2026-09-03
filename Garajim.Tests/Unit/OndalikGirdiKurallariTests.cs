namespace Garajim.Tests.Unit
{
    public class OndalikGirdiKurallariTests
    {
        private static readonly string[] OndalikAlanlar =
        {
            "receipt-amount", "receipt-liters", "maintenance-cost",
            "fuel-liters", "fuel-cost", "fuel-kwh", "expense-amount", "deger-tutar"
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

        private static string Oku(string ad) =>
            File.ReadAllText(Path.Combine(DepoKoku(), "Garajim.API", "wwwroot", ad));

        [Fact]
        public void OndalikAlanlarInputmodeDecimalTasir()
        {
            var html = Oku("index.html");

            foreach (var alan in OndalikAlanlar)
            {
                var satir = html.Split('\n').Single(s => s.Contains("id=\"" + alan + "\""));

                Assert.Contains("inputmode=\"decimal\"", satir);
                Assert.DoesNotContain("type=\"number\"", satir);
            }
        }

        [Fact]
        public void OndalikAlanlarSayiOkuyucusuylaGonderilir()
        {
            var js = Oku("app.js");

            Assert.Contains("function sayiOku(", js);

            foreach (var alan in OndalikAlanlar)
            {
                Assert.DoesNotContain("Number(el(\"" + alan + "\").value)", js);
            }
        }
    }
}
