using System.Security.Cryptography;

namespace Garajim.Business.Concrete.Davetler
{
    public static class DavetKoduUretici
    {
        private const string Alfabe = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int Uzunluk = 8;

        public static string Uret()
        {
            var karakterler = new char[Uzunluk];
            for (var i = 0; i < Uzunluk; i++)
            {
                karakterler[i] = Alfabe[RandomNumberGenerator.GetInt32(Alfabe.Length)];
            }
            return new string(karakterler);
        }

        public static string Normalize(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
            {
                return null;
            }

            var temiz = new string(kod.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            return temiz.Length == 0 ? null : temiz;
        }
    }
}
