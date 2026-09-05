namespace Garajim.RehberUretici
{
    public static class Sabitler
    {
        public const string TabanAdres = "https://garajim.runasp.net";

        public const string Uyari = "Bilgilendirme amaçlıdır; teşhis/onarım kararı yetkili servise aittir.";

        public const string BakimKuralId = "bkm-000";

        public const string BakimKuralBasligi = "Önce şunu bilin";

        public const string CtaBaslik = "Aracının belgeli hafızasını tut";

        public const string CtaMetin = "Fişi fotoğrafla, gerisini Garajım halletsin.";

        public const string CtaDugme = "Ücretsiz başla";

        public const string Kok = "/rehber/";

        public static readonly string[] DurgunSayfalar =
        {
            "/",
            "/yardim.html",
            "/yenilikler.html",
            "/sartlar.html",
            "/rehber/"
        };

        public static string Cta(string slug)
        {
            return "/?utm_source=rehber&amp;utm_medium=icerik&amp;utm_content=" + slug;
        }
    }
}
