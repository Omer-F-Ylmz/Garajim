using Garajim.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Startup
{
    public static class BaglamaHatasi
    {
        public const string Genel = "Gönderilen bilgi geçersiz; alanları kontrol edin.";

        public static IActionResult Yanit(ActionContext context)
        {
            var alan = context.ModelState
                .Where(k => k.Value?.Errors.Count > 0)
                .Select(k => k.Key)
                .FirstOrDefault();

            return new BadRequestObjectResult(new ErrorResult(Mesaj(alan)));
        }

        public static string Mesaj(string alan)
        {
            var ad = (alan ?? string.Empty).Replace("$.", string.Empty).Trim();
            var kucuk = ad.ToLowerInvariant();

            if (kucuk.Contains("tarih") || kucuk.Contains("date"))
            {
                return "Tarih geçersiz.";
            }

            if (kucuk.Contains("tutar") || kucuk.Contains("amount") || kucuk.Contains("cost")
                || kucuk.Contains("km") || kucuk.Contains("litre") || kucuk.Contains("id"))
            {
                return "Sayı alanı geçersiz.";
            }

            if (kucuk.Contains("tur") || kucuk.Contains("type") || kucuk.Contains("kategori")
                || kucuk.Contains("category") || kucuk.Contains("amac") || kucuk.Contains("neden"))
            {
                return "Seçilen değer geçersiz.";
            }

            return Genel;
        }
    }
}
