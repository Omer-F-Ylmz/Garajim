namespace Garajim.KatalogUretici
{
    public sealed class Ayarlar
    {
        public const int VarsayilanBeklemeMs = 200;
        public const int VarsayilanDeneme = 3;

        public static readonly string[] AracTipleri = { "car", "mpv", "truck" };

        public string TrKatalogYolu { get; private set; }

        public string CiktiYolu { get; private set; }

        public string OnbellekKlasoru { get; private set; }

        public int BeklemeMs { get; private set; } = VarsayilanBeklemeMs;

        public int DenemeSayisi { get; private set; } = VarsayilanDeneme;

        public string Surum { get; private set; }

        public static Ayarlar Coz(string[] argumanlar)
        {
            var kok = DepoKoku();

            var ayarlar = new Ayarlar
            {
                TrKatalogYolu = Path.Combine(kok, "Garajim.Business", "Katalog", "arac-katalogu.json"),
                CiktiYolu = Path.Combine(kok, "Garajim.Business", "Katalog", "arac-katalogu-global.json"),
                OnbellekKlasoru = Path.Combine(kok, ".katalog-onbellek"),
            };

            for (var i = 0; i < (argumanlar?.Length ?? 0); i++)
            {
                var deger = i + 1 < argumanlar.Length ? argumanlar[i + 1] : null;

                switch (argumanlar[i])
                {
                    case "--tr":
                        ayarlar.TrKatalogYolu = deger;
                        i++;
                        break;
                    case "--cikti":
                        ayarlar.CiktiYolu = deger;
                        i++;
                        break;
                    case "--onbellek":
                        ayarlar.OnbellekKlasoru = deger;
                        i++;
                        break;
                    case "--surum":
                        ayarlar.Surum = deger;
                        i++;
                        break;
                    case "--bekle":
                        ayarlar.BeklemeMs = Sayi(deger, VarsayilanBeklemeMs);
                        i++;
                        break;
                    case "--deneme":
                        ayarlar.DenemeSayisi = Sayi(deger, VarsayilanDeneme);
                        i++;
                        break;
                }
            }

            return ayarlar;
        }

        private static int Sayi(string deger, string varsayilan) => Sayi(deger, int.Parse(varsayilan));

        private static int Sayi(string deger, int varsayilan)
        {
            return int.TryParse(deger, out var sonuc) && sonuc > 0 ? sonuc : varsayilan;
        }

        private static string DepoKoku()
        {
            var klasor = new DirectoryInfo(AppContext.BaseDirectory);

            while (klasor != null && !File.Exists(Path.Combine(klasor.FullName, "Garajim.sln")))
            {
                klasor = klasor.Parent;
            }

            return klasor?.FullName ?? Directory.GetCurrentDirectory();
        }
    }
}
