using System.Text.RegularExpressions;

namespace Garajim.Business.Usta
{
    public class KirmiziCizgiBulgusu
    {
        public string Kod { get; set; }
        public string Baslik { get; set; }
    }

    public static class KirmiziCizgiler
    {
        public const string Cevap =
            "Anlattığın belirti yol güvenliğini doğrudan ilgilendiriyor. Bu araçla yola çıkma; " +
            "aracı güvenli bir yerde durdur, dörtlüleri yak ve üçgeni koy. " +
            "Aracı kendin sürerek servise götürmeye çalışma, çekici çağır. " +
            "Ben tahmin yürütebilirim ama bu tabloda tahmin yeterli değil; parçayı yerinde gören bir usta baksın. " +
            "Yolda kaldıysan yol yardım hattını veya sigortanın asistans hizmetini ara.";

        private static readonly (string Kod, string Baslik, Regex Desen)[] Tablo =
        {
            ("FrenBosaldi", "Fren pedalı boşaldı ya da fren tutmuyor", new Regex(
                @"(fren\w*\s+(pedal\w*\s+)?(yere\s+(kadar\s+)?(gid|dayan|in)|bosal|kacir|tutmuyo|tutmiyo|tutmoyo|gitmiyo)|pedal\w*\s+yere\s+(kadar\s+)?(gid|dayan|in)|fren\w*\s+yok)",
                RegexOptions.Compiled)),

            ("DireksiyonKilit", "Direksiyon kilitlendi ya da ağırlaştı", new Regex(
                @"(direksiyon\w*(\s+\w+){0,2}\s+(kilitlen|kilitli|donmus|donuyor)|direksiyon\w*(\s+\w+){0,2}\s+(agir|sert)\w*.{0,40}(ses|tak|gicirt|catir)|(ses|tak|gicirt|catir)\w*.{0,40}direksiyon\w*(\s+\w+){0,2}\s+(agir|sert))",
                RegexOptions.Compiled)),

            ("KirmiziLamba", "Kırmızı yağ, sıcaklık ya da şarj lambası yandı", new Regex(
                @"kirmizi.{0,30}(yag|sicaklik|hararet|sarj|aku)\w*\s*(lamba|isik|ikaz|uyari)|((yag|sicaklik|hararet|sarj)\w*\s*(lamba|isik|ikaz|uyari)\w*).{0,20}kirmizi",
                RegexOptions.Compiled)),

            ("Hararet", "Motor hararet yaptı ya da buhar çıkıyor", new Regex(
                @"(hararet\s+(yap|etti|yapiyor|var)|motor\w*\s+kayn|kaputtan\s+(buhar|duman)|radyator\w*\s+kayn|(buhar)\w*\s+(cikiyor|fiskir)|sicaklik\s+ibresi\s+(kirmizi|tavan))",
                RegexOptions.Compiled)),

            ("YakitKokusu", "Yakıt kokusu geliyor", new Regex(
                @"((benzin|mazot|motorin|yakit|lpg|gaz)\s+koku\w*|koku\w*\s+(benzin|mazot|motorin|yakit)|(benzin|mazot|motorin|yakit)\s+(sizint|damliyor|akiyor|siziyor))",
                RegexOptions.Compiled)),

            ("KabinDumani", "Kabine duman ya da yanık kokusu doluyor", new Regex(
                @"((ic|ici|icine|kabin|kabine|araba\w*n\s+ici)\w*.{0,25}duman|duman.{0,25}(kabin|icer|ic\s+kisim)|yanik\s+(koku|plastik|kablo)|kablo\s+yanik)",
                RegexOptions.Compiled)),

            ("MetalSesiTitreme", "Metal sesi ile birlikte titreme var", new Regex(
                @"((metal|demir|tik\s*tak|takirti|catirti)\w*\s*ses\w*.{0,50}(titr|sars|zipl)|(titr|sars)\w*.{0,50}(metal|demir|takirti|catirti)\w*\s*ses)",
                RegexOptions.Compiled)),

            ("SeyirdeStop", "Araç seyir hâlinde stop ediyor", new Regex(
                @"((seyir|giderken|yolda|otoyolda|hareket)\w*.{0,30}stop\s*(etti|ediyor|yapiyor)|stop\s*(etti|ediyor).{0,30}(seyir|giderken|yolda|otoyolda)|(giderken|seyirde)\w*\s+motor\w*\s+(sustu|kesildi|durdu))",
                RegexOptions.Compiled))
        };

        public static KirmiziCizgiBulgusu Bul(string metin)
        {
            var normal = BilgiSecici.Normalize(metin);
            if (normal.Length == 0)
            {
                return null;
            }

            foreach (var satir in Tablo)
            {
                if (satir.Desen.IsMatch(normal))
                {
                    return new KirmiziCizgiBulgusu { Kod = satir.Kod, Baslik = satir.Baslik };
                }
            }

            return null;
        }

        public static bool VarMi(string metin)
        {
            return Bul(metin) != null;
        }
    }
}
