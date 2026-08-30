using System.Globalization;
using System.Text.Json;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete.Receipts
{
    public static class ReceiptResponseParser
    {
        public const string Prompt =
            "Bu görüntü Türkiye'den bir araç harcama belgesidir: yazarkasa (ÖKC) fişi, akaryakıt pompa fişi " +
            "veya e-arşiv fatura görüntüsü olabilir. Görüntüden aşağıdaki alanları çıkar ve YALNIZCA tek bir " +
            "JSON nesnesi döndür; açıklama, kod bloğu veya başka metin ekleme. Şema: " +
            "{\"tarih\":\"YYYY-AA-GG\"|null,\"toplamTutar\":sayi|null,\"kdvTutari\":sayi|null," +
            "\"litre\":sayi|null,\"birimFiyat\":sayi|null,\"plaka\":\"metin\"|null,\"km\":tamsayi|null," +
            "\"tahminiTur\":\"Yakit\"|\"Bakim\"|\"Masraf\"|\"Bilinmiyor\"," +
            "\"kalemler\":[{\"ad\":\"metin\",\"tutar\":sayi}],\"guvenSkoru\":0 ile 1 arasi sayi}. " +
            "Kurallar: sayılarda ondalık ayracı nokta kullan. Akaryakıt pompa fişlerinde plaka, litre ve " +
            "birim fiyat basılı olabilir; varsa mutlaka çıkar. Emin olmadığın alanı null bırak, uydurma. " +
            "Toplam tutar KDV dahil tutardır. tahminiTur: yakıt satışı Yakit; servis, parça, işçilik Bakim; " +
            "otopark, yıkama, köprü, otoyol, sigorta, muayene gibi giderler Masraf; karar veremiyorsan Bilinmiyor.";

        public static ReceiptExtractionResult Bos(string hamYanit)
        {
            return new ReceiptExtractionResult { GuvenSkoru = 0, HamYanit = hamYanit };
        }

        public static ReceiptExtractionResult Parse(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return Bos(metin);
            }

            var baslangic = metin.IndexOf('{');
            var bitis = metin.LastIndexOf('}');
            if (baslangic < 0 || bitis <= baslangic)
            {
                return Bos(metin);
            }

            try
            {
                using var belge = JsonDocument.Parse(metin.Substring(baslangic, bitis - baslangic + 1));
                var kok = belge.RootElement;

                var sonuc = new ReceiptExtractionResult
                {
                    HamYanit = metin,
                    Tarih = TarihOku(Alan(kok, "tarih")),
                    ToplamTutar = OndalikOku(Alan(kok, "toplamTutar")),
                    KdvTutari = OndalikOku(Alan(kok, "kdvTutari")),
                    Litre = OndalikOku(Alan(kok, "litre")),
                    BirimFiyat = OndalikOku(Alan(kok, "birimFiyat")),
                    Plaka = PlakayiNormalizeEt(MetinOku(Alan(kok, "plaka"))),
                    Km = TamsayiOku(Alan(kok, "km")),
                    TahminiTur = TurOku(MetinOku(Alan(kok, "tahminiTur"))),
                    GuvenSkoru = Math.Clamp(OndalikOku(Alan(kok, "guvenSkoru")) is decimal skor ? (double)skor : 0, 0, 1)
                };

                if (Alan(kok, "kalemler") is JsonElement kalemler && kalemler.ValueKind == JsonValueKind.Array)
                {
                    foreach (var kalem in kalemler.EnumerateArray())
                    {
                        sonuc.KalemListesi.Add(new ReceiptItemResult
                        {
                            Ad = MetinOku(Alan(kalem, "ad")),
                            Tutar = OndalikOku(Alan(kalem, "tutar"))
                        });
                    }
                }

                return sonuc;
            }
            catch (JsonException)
            {
                return Bos(metin);
            }
        }

        public static string PlakayiNormalizeEt(string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                return null;
            }

            var temiz = new string(plaka.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return temiz.Length == 0 ? null : temiz;
        }

        private static JsonElement? Alan(JsonElement kaynak, string ad)
        {
            if (kaynak.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var ozellik in kaynak.EnumerateObject())
            {
                if (string.Equals(ozellik.Name, ad, StringComparison.OrdinalIgnoreCase))
                {
                    return ozellik.Value;
                }
            }

            return null;
        }

        private static string MetinOku(JsonElement? eleman)
        {
            if (eleman is not JsonElement e || e.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var deger = e.GetString();
            return string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();
        }

        private static decimal? OndalikOku(JsonElement? eleman)
        {
            if (eleman is not JsonElement e)
            {
                return null;
            }

            if (e.ValueKind == JsonValueKind.Number && e.TryGetDecimal(out var sayi))
            {
                return sayi;
            }

            if (e.ValueKind == JsonValueKind.String &&
                decimal.TryParse(e.GetString()?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var metinden))
            {
                return metinden;
            }

            return null;
        }

        private static int? TamsayiOku(JsonElement? eleman)
        {
            var deger = OndalikOku(eleman);
            return deger == null || deger < 0 || deger > int.MaxValue ? null : (int)deger.Value;
        }

        private static DateTime? TarihOku(JsonElement? eleman)
        {
            var metin = MetinOku(eleman);
            if (metin == null)
            {
                return null;
            }

            string[] bicimler = { "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy" };
            if (DateTime.TryParseExact(metin, bicimler, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tam))
            {
                return tam.Date;
            }

            return DateTime.TryParse(metin, CultureInfo.InvariantCulture, DateTimeStyles.None, out var genel)
                ? genel.Date
                : null;
        }

        private static ReceiptType TurOku(string deger)
        {
            var normal = deger?.Trim().ToLowerInvariant()
                .Replace('ı', 'i').Replace('İ', 'i');

            return normal switch
            {
                "yakit" => ReceiptType.Yakit,
                "bakim" => ReceiptType.Bakim,
                "masraf" => ReceiptType.Masraf,
                _ => ReceiptType.Bilinmiyor
            };
        }
    }
}
