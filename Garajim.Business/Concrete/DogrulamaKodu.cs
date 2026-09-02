using System.Security.Cryptography;
using System.Text;

namespace Garajim.Business.Concrete
{
    public static class DogrulamaKodu
    {
        public const int Uzunluk = 6;
        public const int GecerlilikDakika = 10;
        public const int MaxDeneme = 5;
        public const int GonderimAraligiSaniye = 60;
        public const int SaatlikGonderimSiniri = 5;

        public static string Uret()
        {
            var sayi = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return sayi.ToString("D" + Uzunluk);
        }

        public static string Hashle(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
            {
                return null;
            }

            var bayt = SHA256.HashData(Encoding.UTF8.GetBytes(kod.Trim()));
            return Convert.ToHexString(bayt).ToLowerInvariant();
        }

        public static bool Esit(string kod, string hash)
        {
            if (string.IsNullOrWhiteSpace(kod) || string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Hashle(kod)),
                Encoding.UTF8.GetBytes(hash));
        }

        public static string EpostaGovdesi(string kod)
        {
            return
                "Garajım doğrulama kodunuz: " + kod + Environment.NewLine + Environment.NewLine +
                "Kod " + GecerlilikDakika + " dakika geçerlidir." + Environment.NewLine +
                "Bu kodu kimseyle paylaşmayın; Garajım sizden kodu asla telefonla ya da e-postayla istemez." + Environment.NewLine +
                Environment.NewLine +
                "Bu kaydı siz istemediyseniz bu e-postayı yok sayın; hesap açılmaz ve adresiniz kullanılmaz.";
        }

        public const string EpostaKonusu = "Garajım doğrulama kodunuz";
    }
}
