using System.Text.Json;
using System.Text.Json.Serialization;
using Garajim.Business.Usta;

namespace Garajim.Business.Katalog
{
    public class SssKaydi
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("baslik")]
        public string Baslik { get; set; }

        [JsonPropertyName("cevap")]
        public string Cevap { get; set; }

        [JsonPropertyName("anahtarlar")]
        public List<string> Anahtarlar { get; set; } = new List<string>();
    }

    public static class YardimSss
    {
        public const string DosyaAdi = "yardim-sss.json";
        public const string KlasorAdi = "Katalog";
        public const string Kategori = "uygulama-kullanim";
        public const string Kaynak = "Garajım yardım sayfası";
        public const string Guncelleme = "2026-09-05";

        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static IReadOnlyList<SssKaydi> Yukle(string klasor)
        {
            var yol = Path.Combine(klasor, DosyaAdi);

            if (!File.Exists(yol))
                throw new InvalidOperationException($"Yardım SSS dosyası bulunamadı: {yol}");

            List<SssKaydi> kayitlar;

            try
            {
                kayitlar = JsonSerializer.Deserialize<List<SssKaydi>>(File.ReadAllText(yol), Secenekler);
            }
            catch (JsonException hata)
            {
                throw new InvalidOperationException($"Yardım SSS dosyası okunamadı: {hata.Message}", hata);
            }

            if (kayitlar == null || kayitlar.Count == 0)
                throw new InvalidOperationException("Yardım SSS dosyası boş.");

            var idler = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kayit in kayitlar)
            {
                Dogrula(kayit, idler);
            }

            return kayitlar;
        }

        public static IReadOnlyList<BilgiKaydi> BilgiKayitlari(IReadOnlyList<SssKaydi> kayitlar)
        {
            return kayitlar.Select(k => new BilgiKaydi
            {
                Id = k.Id,
                Kategori = Kategori,
                Anahtarlar = k.Anahtarlar.ToList(),
                Metin = k.Baslik + " " + k.Cevap,
                Kaynak = Kaynak,
                Guncelleme = Guncelleme
            }).ToList();
        }

        private static void Dogrula(SssKaydi kayit, HashSet<string> idler)
        {
            if (string.IsNullOrWhiteSpace(kayit.Id))
                throw new InvalidOperationException("Yardım SSS şeması hatalı: 'id' boş.");

            if (!idler.Add(kayit.Id))
                throw new InvalidOperationException($"Yardım SSS şeması hatalı: id tekrar ediyor ({kayit.Id}).");

            if (string.IsNullOrWhiteSpace(kayit.Baslik))
                throw new InvalidOperationException($"Yardım SSS şeması hatalı: {kayit.Id} kaydında 'baslik' boş.");

            if (string.IsNullOrWhiteSpace(kayit.Cevap))
                throw new InvalidOperationException($"Yardım SSS şeması hatalı: {kayit.Id} kaydında 'cevap' boş.");

            if (kayit.Anahtarlar == null || kayit.Anahtarlar.Count == 0 || kayit.Anahtarlar.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"Yardım SSS şeması hatalı: {kayit.Id} kaydında 'anahtarlar' boş ya da boş eleman içeriyor.");
        }
    }
}
