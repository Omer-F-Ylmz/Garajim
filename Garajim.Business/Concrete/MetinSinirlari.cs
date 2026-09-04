namespace Garajim.Business.Concrete
{
    public static class MetinSinirlari
    {
        public const int ServisAdi = 150;
        public const int Not = 500;
        public const int ParcaAciklama = 200;
        public const int ParcaMarka = 100;

        public static string Kirp(string metin, int uzunluk)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return metin;
            }

            var temiz = metin.Trim();

            return temiz.Length <= uzunluk ? temiz : temiz.Substring(0, uzunluk);
        }
    }
}
