using Garajim.Business.Constants;

namespace Garajim.Business.Concrete.Documents
{
    public static class DocumentContentValidator
    {
        public const long VarsayilanDosyaSiniri = 5 * 1024 * 1024;
        public const long VarsayilanKota = 250 * 1024 * 1024;

        private static readonly Dictionary<string, string> IzinliUzantilar = new Dictionary<string, string>
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".pdf"] = "application/pdf"
        };

        private static readonly Dictionary<string, byte[][]> SihirliBaytlar = new Dictionary<string, byte[][]>
        {
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }
        };

        public static string GuvenliAd(string dosyaAdi)
        {
            var ad = Path.GetFileName(dosyaAdi ?? string.Empty).Replace("\\", string.Empty);
            if (string.IsNullOrWhiteSpace(ad))
            {
                return null;
            }

            return ad.Length > 260 ? ad.Substring(0, 260) : ad;
        }

        public static string IcerikTipi(string uzanti)
        {
            return IzinliUzantilar.TryGetValue(uzanti, out var tip) ? tip : null;
        }

        public static string Dogrula(string orijinalAd, byte[] icerik, long dosyaSiniri)
        {
            if (string.IsNullOrWhiteSpace(orijinalAd))
            {
                return Messages.InvalidValue;
            }

            var uzanti = Path.GetExtension(orijinalAd).ToLowerInvariant();
            if (!IzinliUzantilar.ContainsKey(uzanti))
            {
                return Messages.DocumentExtensionNotAllowed;
            }

            if (icerik == null || icerik.LongLength == 0)
            {
                return Messages.InvalidValue;
            }

            if (icerik.LongLength > dosyaSiniri)
            {
                return Messages.DocumentTooLarge;
            }

            if (!SihirliBaytUyuyorMu(uzanti, icerik))
            {
                return Messages.DocumentContentMismatch;
            }

            return null;
        }

        private static bool SihirliBaytUyuyorMu(string uzanti, byte[] icerik)
        {
            if (!SihirliBaytlar.TryGetValue(uzanti, out var imzalar))
            {
                return false;
            }

            return imzalar.Any(imza => icerik.Length >= imza.Length && icerik.Take(imza.Length).SequenceEqual(imza));
        }
    }
}
