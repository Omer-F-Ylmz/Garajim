namespace Garajim.Business.Concrete.Import
{
    public static class ImportSablonlari
    {
        private static readonly Dictionary<string, string[]> Esanlamlar = new Dictionary<string, string[]>
        {
            ["tarih"] = new[] { "tarih", "date", "data", "gun", "zaman", "datetime" },
            ["km"] = new[] { "km", "kilometre", "odometer", "odometre", "kilometraj", "mileage", "odometerkm" },
            ["litre"] = new[] { "litre", "lt", "volume", "fuelvolume", "miktar", "yakitmiktari", "quantity" },
            ["tutar"] = new[] { "tutar", "toplammaliyet", "totalcost", "total", "maliyet", "cost", "amount", "ucret", "fiyattoplam" },
            ["birimfiyat"] = new[] { "birimfiyat", "price", "fiyat", "unitprice", "litrefiyati" },
            ["kategori"] = new[] { "kategori", "category", "tur", "type", "costtitle", "masrafturu", "gidertipi" },
            ["aciklama"] = new[] { "aciklama", "not", "note", "notes", "description", "detay", "comment" },
            ["servis"] = new[] { "servis", "service", "workshop", "yer", "place", "istasyon", "station" },
            ["tamdolum"] = new[] { "tamdolum", "fulltank", "full", "depodoldu", "dolutank", "filledup", "tankfull", "komple" }
        };

        public static string Sez(CsvTablo tablo)
        {
            if (tablo.HamSatirlar.Any(s => s.TrimStart().StartsWith("## ", StringComparison.Ordinal)))
            {
                return "Fuelio";
            }

            var basliklar = tablo.Basliklar.Select(Sadelestir).ToList();
            var drivvoIzi = basliklar.Count(b => Esanlamlar["km"].Contains(b) || Esanlamlar["litre"].Contains(b) || Esanlamlar["tutar"].Contains(b));

            return drivvoIzi >= 2 ? "Drivvo" : "Genel";
        }

        public static Dictionary<string, int> SutunOner(CsvTablo tablo, string kayitTuru)
        {
            var eslesme = new Dictionary<string, int>();
            var kullanilan = new HashSet<int>();

            foreach (var alan in AlanlariAl(kayitTuru))
            {
                if (!Esanlamlar.TryGetValue(alan, out var anahtarlar))
                {
                    continue;
                }

                for (var i = 0; i < tablo.Basliklar.Count; i++)
                {
                    if (kullanilan.Contains(i))
                    {
                        continue;
                    }

                    var baslik = Sadelestir(tablo.Basliklar[i]);
                    if (anahtarlar.Contains(baslik))
                    {
                        eslesme[alan] = i;
                        kullanilan.Add(i);
                        break;
                    }
                }
            }

            return eslesme;
        }

        public static string[] AlanlariAl(string kayitTuru)
        {
            return kayitTuru switch
            {
                "Yakit" => new[] { "tarih", "km", "litre", "tutar", "birimfiyat", "tamdolum" },
                "Bakim" => new[] { "tarih", "km", "tutar", "servis", "aciklama" },
                _ => new[] { "tarih", "tutar", "kategori", "aciklama" }
            };
        }

        public static string Sadelestir(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return string.Empty;
            }

            var kucuk = metin.Trim().ToLowerInvariant()
                .Replace('ı', 'i').Replace('İ', 'i')
                .Replace('ş', 's').Replace('ğ', 'g')
                .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');

            return new string(kucuk.Where(char.IsLetterOrDigit).ToArray());
        }
    }
}
