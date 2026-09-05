namespace Garajim.RehberUretici
{
    public class Bolum
    {
        public string Yol { get; set; }
        public string Dosya { get; set; }
        public string Etiket { get; set; }
        public string HubBaslik { get; set; }
        public string HubAciklama { get; set; }
    }

    public static class Bolumler
    {
        public const string Belirti = "belirti";
        public const string Obd = "obd";
        public const string Bakim = "bakim";
        public const string Muayene = "muayene";
        public const string Turkiye = "turkiye";

        public static readonly IReadOnlyList<Bolum> Hepsi = new List<Bolum>
        {
            new Bolum
            {
                Yol = Belirti,
                Dosya = "belirtiler.json",
                Etiket = "Belirti",
                HubBaslik = "Araç belirtileri rehberi",
                HubAciklama = "Sesler, titreşimler, kokular ve uyarı lambaları: en sık nedenler, kırmızı çizgiler ve ustaya ne söyleyeceğiniz."
            },
            new Bolum
            {
                Yol = Obd,
                Dosya = "obd-kodlari.json",
                Etiket = "Arıza kodu",
                HubBaslik = "OBD arıza kodları rehberi",
                HubAciklama = "P, C, B ve U ile başlayan arıza kodlarının Türkçe anlamı, sık nedenleri ve aciliyeti."
            },
            new Bolum
            {
                Yol = Bakim,
                Dosya = "bakim-araliklari.json",
                Etiket = "Bakım aralığı",
                HubBaslik = "Bakım aralıkları rehberi",
                HubAciklama = "Motor ve model bazında triger, yağ, filtre ve fren hidroliği aralıkları."
            },
            new Bolum
            {
                Yol = Muayene,
                Dosya = "tuvturk.json",
                Etiket = "Muayene",
                HubBaslik = "TÜVTÜRK muayene rehberi",
                HubAciklama = "Muayene sınıfları, kalma nedenleri, randevu, ücret ve tekrar muayene kuralları."
            },
            new Bolum
            {
                Yol = Turkiye,
                Dosya = "turkiye-ozel.json",
                Etiket = "Türkiye kuralları",
                HubBaslik = "Türkiye'ye özel araç kuralları",
                HubAciklama = "Kış lastiği, egzoz emisyon, sigorta, MTV ve trafikte geçerli süreler."
            }
        };

        public static Bolum Bul(string yol)
        {
            return Hepsi.First(b => b.Yol == yol);
        }
    }
}
