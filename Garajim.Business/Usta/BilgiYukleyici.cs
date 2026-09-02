using System.Text.Json;

namespace Garajim.Business.Usta
{
    public class BilgiYukleyici
    {
        public const string KlasorAdi = "Usta/Bilgi";

        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public IReadOnlyList<BilgiKaydi> Yukle(string klasor)
        {
            if (!Directory.Exists(klasor))
            {
                throw new InvalidOperationException($"AI Usta bilgi tabanı klasörü bulunamadı: {klasor}");
            }

            var dosyalar = Directory.GetFiles(klasor, "*.json").OrderBy(d => d, StringComparer.Ordinal).ToList();
            if (dosyalar.Count == 0)
            {
                throw new InvalidOperationException($"AI Usta bilgi tabanı boş: {klasor} içinde json dosyası yok.");
            }

            var kayitlar = new List<BilgiKaydi>();
            var idler = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dosya in dosyalar)
            {
                var ad = Path.GetFileName(dosya);
                List<BilgiKaydi> okunan;

                try
                {
                    okunan = JsonSerializer.Deserialize<List<BilgiKaydi>>(File.ReadAllText(dosya), Secenekler);
                }
                catch (JsonException hata)
                {
                    throw new InvalidOperationException($"AI Usta bilgi dosyası okunamadı ({ad}): {hata.Message}", hata);
                }

                if (okunan == null || okunan.Count == 0)
                {
                    throw new InvalidOperationException($"AI Usta bilgi dosyası boş: {ad}");
                }

                for (var i = 0; i < okunan.Count; i++)
                {
                    Dogrula(ad, i, okunan[i], idler);
                    kayitlar.Add(okunan[i]);
                }
            }

            return kayitlar;
        }

        private static void Dogrula(string dosya, int sira, BilgiKaydi kayit, HashSet<string> idler)
        {
            var konum = $"{dosya}[{sira}]";

            if (string.IsNullOrWhiteSpace(kayit.Id))
                throw new InvalidOperationException($"AI Usta bilgi şeması hatalı: {konum} kaydında 'id' boş.");

            if (!idler.Add(kayit.Id))
                throw new InvalidOperationException($"AI Usta bilgi şeması hatalı: {konum} kaydındaki id tekrar ediyor ({kayit.Id}).");

            if (string.IsNullOrWhiteSpace(kayit.Kategori))
                throw new InvalidOperationException($"AI Usta bilgi şeması hatalı: {konum} ({kayit.Id}) kaydında 'kategori' boş.");

            if (kayit.Anahtarlar == null || kayit.Anahtarlar.Count == 0 || kayit.Anahtarlar.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"AI Usta bilgi şeması hatalı: {konum} ({kayit.Id}) kaydında 'anahtarlar' boş ya da boş eleman içeriyor.");

            if (string.IsNullOrWhiteSpace(kayit.Metin))
                throw new InvalidOperationException($"AI Usta bilgi şeması hatalı: {konum} ({kayit.Id}) kaydında 'metin' boş.");

            if (string.IsNullOrWhiteSpace(kayit.Kaynak))
                throw new InvalidOperationException($"AI Usta bilgi şeması hatalı: {konum} ({kayit.Id}) kaydında 'kaynak' boş.");

            if (string.IsNullOrWhiteSpace(kayit.Guncelleme) || !DateTime.TryParse(kayit.Guncelleme, out _))
                throw new InvalidOperationException($"AI Usta bilgi şeması hatalı: {konum} ({kayit.Id}) kaydında 'guncelleme' tarihi okunamadı.");
        }
    }
}
