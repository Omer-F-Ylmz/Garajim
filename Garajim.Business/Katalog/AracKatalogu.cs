using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Garajim.Business.Katalog
{
    public class KatalogMarkasi
    {
        [JsonPropertyName("ad")]
        public string Ad { get; set; }

        [JsonPropertyName("seriler")]
        public List<string> Seriler { get; set; } = new List<string>();
    }

    public class KatalogBelgesi
    {
        [JsonPropertyName("surum")]
        public string Surum { get; set; }

        [JsonPropertyName("kaynak")]
        public string Kaynak { get; set; }

        [JsonPropertyName("markalar")]
        public List<KatalogMarkasi> Markalar { get; set; } = new List<KatalogMarkasi>();
    }

    public class GlobalKatalogSerisi
    {
        [JsonPropertyName("ad")]
        public string Ad { get; set; }

        [JsonPropertyName("tr")]
        public bool Tr { get; set; }
    }

    public class GlobalKatalogMarkasi
    {
        [JsonPropertyName("ad")]
        public string Ad { get; set; }

        [JsonPropertyName("tr")]
        public bool Tr { get; set; }

        [JsonPropertyName("seriler")]
        public List<GlobalKatalogSerisi> Seriler { get; set; } = new List<GlobalKatalogSerisi>();
    }

    public class GlobalKatalogBelgesi
    {
        [JsonPropertyName("surum")]
        public string Surum { get; set; }

        [JsonPropertyName("kaynak")]
        public string Kaynak { get; set; }

        [JsonPropertyName("uretimTarihi")]
        public string UretimTarihi { get; set; }

        [JsonPropertyName("markalar")]
        public List<GlobalKatalogMarkasi> Markalar { get; set; } = new List<GlobalKatalogMarkasi>();
    }

    public class KatalogAramaSonucu
    {
        public KatalogAramaSonucu(List<string> kayitlar, int toplam, int atlanan = 0)
        {
            Kayitlar = kayitlar;
            Toplam = toplam;
            Atlanan = atlanan;
        }

        public List<string> Kayitlar { get; }

        public int Toplam { get; }

        public int Atlanan { get; }

        public bool DahaVar => Atlanan + Kayitlar.Count < Toplam;
    }

    public class AracKatalogu
    {
        public const string DosyaAdi = "arac-katalogu.json";
        public const string GlobalDosyaAdi = "arac-katalogu-global.json";
        public const string KlasorAdi = "Katalog";
        public const int VarsayilanSayfa = 50;

        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<string, KatalogMarkasi> _markalar;
        private readonly Dictionary<string, string> _seriMarkasi;
        private readonly HashSet<string> _trMarkalar;
        private readonly Dictionary<string, HashSet<string>> _trSeriler;

        private AracKatalogu(string surum, string globalSurum, List<KatalogMarkasi> markalar,
            HashSet<string> trMarkalar, Dictionary<string, HashSet<string>> trSeriler,
            Dictionary<string, string> seriMarkasi)
        {
            Surum = surum;
            GlobalSurum = globalSurum;
            Markalar = markalar;

            _markalar = markalar.ToDictionary(m => m.Ad, m => m, StringComparer.OrdinalIgnoreCase);
            _trMarkalar = trMarkalar;
            _trSeriler = trSeriler;
            _seriMarkasi = seriMarkasi;
        }

        public string Surum { get; }

        public string GlobalSurum { get; }

        public string EtiketSurumu => GlobalSurum == null ? Surum : Surum + "+" + GlobalSurum;

        public IReadOnlyList<KatalogMarkasi> Markalar { get; }

        public IReadOnlyList<string> MarkaAdlari => Markalar.Select(m => m.Ad).ToList();

        public static AracKatalogu Yukle(string klasor)
        {
            var kok = klasor ?? string.Empty;
            var yol = Path.Combine(kok, DosyaAdi);

            if (!File.Exists(yol))
            {
                throw new InvalidOperationException("Araç kataloğu bulunamadı: " + yol);
            }

            KatalogBelgesi belge;

            try
            {
                belge = JsonSerializer.Deserialize<KatalogBelgesi>(File.ReadAllText(yol), Secenekler);
            }
            catch (JsonException hata)
            {
                throw new InvalidOperationException("Araç kataloğu okunamadı: " + yol, hata);
            }

            Dogrula(belge, yol);

            var markalar = belge.Markalar
                .Select(m => new KatalogMarkasi { Ad = m.Ad, Seriler = m.Seriler.ToList() })
                .ToList();

            var trMarkalar = new HashSet<string>(markalar.Select(m => m.Ad), StringComparer.OrdinalIgnoreCase);
            var trSeriler = markalar.ToDictionary(
                m => m.Ad,
                m => new HashSet<string>(m.Seriler, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            var seriMarkasi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var marka in markalar)
            {
                foreach (var seri in marka.Seriler)
                {
                    seriMarkasi[seri] = marka.Ad;
                }
            }

            var globalSurum = GlobaliBirlestir(kok, markalar);

            return new AracKatalogu(belge.Surum, globalSurum, markalar, trMarkalar, trSeriler, seriMarkasi);
        }

        private static string GlobaliBirlestir(string klasor, List<KatalogMarkasi> markalar)
        {
            var yol = Path.Combine(klasor, GlobalDosyaAdi);

            if (!File.Exists(yol))
            {
                return null;
            }

            GlobalKatalogBelgesi belge;

            try
            {
                belge = JsonSerializer.Deserialize<GlobalKatalogBelgesi>(File.ReadAllText(yol), Secenekler);
            }
            catch (JsonException hata)
            {
                throw new InvalidOperationException("Global araç kataloğu okunamadı: " + yol, hata);
            }

            if (belge == null || string.IsNullOrWhiteSpace(belge.Surum) || belge.Markalar == null)
            {
                throw new InvalidOperationException("Global araç kataloğu şeması geçersiz (sürüm ya da marka listesi eksik): " + yol);
            }

            var sozluk = markalar.ToDictionary(m => m.Ad, m => m, StringComparer.OrdinalIgnoreCase);

            foreach (var globalMarka in belge.Markalar)
            {
                if (string.IsNullOrWhiteSpace(globalMarka.Ad))
                {
                    continue;
                }

                if (!sozluk.TryGetValue(globalMarka.Ad, out var marka))
                {
                    marka = new KatalogMarkasi { Ad = globalMarka.Ad.Trim(), Seriler = new List<string>() };
                    sozluk[marka.Ad] = marka;
                    markalar.Add(marka);
                }

                foreach (var seri in globalMarka.Seriler ?? new List<GlobalKatalogSerisi>())
                {
                    if (string.IsNullOrWhiteSpace(seri.Ad))
                    {
                        continue;
                    }

                    if (!marka.Seriler.Any(s => string.Equals(s, seri.Ad, StringComparison.OrdinalIgnoreCase)))
                    {
                        marka.Seriler.Add(seri.Ad.Trim());
                    }
                }
            }

            return belge.Surum;
        }

        public bool MarkaVar(string marka)
        {
            return !string.IsNullOrWhiteSpace(marka) && _markalar.ContainsKey(marka.Trim());
        }

        public bool SeriVar(string marka, string seri)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(seri))
            {
                return false;
            }

            return _markalar.TryGetValue(marka.Trim(), out var kayit)
                && kayit.Seriler.Any(s => string.Equals(s, seri.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public bool TrMarkaMi(string marka)
        {
            return !string.IsNullOrWhiteSpace(marka) && _trMarkalar.Contains(marka.Trim());
        }

        public bool TrSeriMi(string marka, string seri)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(seri))
            {
                return false;
            }

            return _trSeriler.TryGetValue(marka.Trim(), out var seriler) && seriler.Contains(seri.Trim());
        }

        public string MarkaYazimi(string marka)
        {
            return !string.IsNullOrWhiteSpace(marka) && _markalar.TryGetValue(marka.Trim(), out var kayit)
                ? kayit.Ad
                : null;
        }

        public string SeriYazimi(string marka, string seri)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(seri)
                || !_markalar.TryGetValue(marka.Trim(), out var kayit))
            {
                return null;
            }

            return kayit.Seriler.FirstOrDefault(s => string.Equals(s, seri.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public string SerininMarkasi(string seri)
        {
            return !string.IsNullOrWhiteSpace(seri) && _seriMarkasi.TryGetValue(seri.Trim(), out var marka)
                ? marka
                : null;
        }

        public IReadOnlyList<string> Seriler(string marka)
        {
            return !string.IsNullOrWhiteSpace(marka) && _markalar.TryGetValue(marka.Trim(), out var kayit)
                ? kayit.Seriler
                : Array.Empty<string>();
        }

        public KatalogAramaSonucu MarkaAra(string terim, int sayfaBoyutu, int sayfa = 1)
        {
            return Ara(Markalar.Select(m => m.Ad), terim, sayfaBoyutu, sayfa, ad => TrMarkaMi(ad));
        }

        public KatalogAramaSonucu SeriAra(string marka, string terim, int sayfaBoyutu, int sayfa = 1)
        {
            var kanonik = MarkaYazimi(marka);

            if (kanonik == null)
            {
                return new KatalogAramaSonucu(new List<string>(), 0);
            }

            return Ara(Seriler(kanonik), terim, sayfaBoyutu, sayfa, ad => TrSeriMi(kanonik, ad));
        }

        private static KatalogAramaSonucu Ara(IEnumerable<string> kaynak, string terim, int sayfaBoyutu, int sayfa, Func<string, bool> trMi)
        {
            var sade = Sadelestir(terim);
            var boyut = sayfaBoyutu > 0 ? sayfaBoyutu : VarsayilanSayfa;

            var eslesenler = kaynak
                .Select(ad => new { Ad = ad, Sade = Sadelestir(ad) })
                .Where(k => sade.Length == 0 || k.Sade.Contains(sade, StringComparison.Ordinal))
                .Select(k => new
                {
                    k.Ad,
                    Tr = trMi(k.Ad) ? 0 : 1,
                    Onek = sade.Length > 0 && k.Sade.StartsWith(sade, StringComparison.Ordinal) ? 0 : 1,
                })
                .OrderBy(k => k.Tr)
                .ThenBy(k => k.Onek)
                .ThenBy(k => k.Ad, StringComparer.Ordinal)
                .ToList();

            var atlanacak = (sayfa > 0 ? sayfa - 1 : 0) * boyut;

            return new KatalogAramaSonucu(
                eslesenler.Skip(atlanacak).Take(boyut).Select(k => k.Ad).ToList(),
                eslesenler.Count,
                atlanacak);
        }

        public static string Sadelestir(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return string.Empty;
            }

            var yazi = new StringBuilder(metin.Length);

            foreach (var karakter in metin.Trim())
            {
                if (char.IsWhiteSpace(karakter))
                {
                    continue;
                }

                yazi.Append(Katla(char.ToLowerInvariant(karakter)));
            }

            return yazi.ToString();
        }

        private static char Katla(char karakter)
        {
            switch (karakter)
            {
                case 'ı': return 'i';
                case 'ş': return 's';
                case 'ğ': return 'g';
                case 'ü': return 'u';
                case 'ö': return 'o';
                case 'ç': return 'c';
                case 'â': return 'a';
                case 'î': return 'i';
                case 'û': return 'u';
                case 'ë': return 'e';
                case 'é': return 'e';
                case 'š': return 's';
                case 'ž': return 'z';
                case 'č': return 'c';
                default: return karakter;
            }
        }

        private static void Dogrula(KatalogBelgesi belge, string yol)
        {
            if (belge == null || string.IsNullOrWhiteSpace(belge.Surum) || belge.Markalar == null || belge.Markalar.Count == 0)
            {
                throw new InvalidOperationException("Araç kataloğu şeması geçersiz (sürüm ya da marka listesi eksik): " + yol);
            }

            var markaAdlari = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seriSahipleri = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var marka in belge.Markalar)
            {
                if (string.IsNullOrWhiteSpace(marka?.Ad))
                {
                    throw new InvalidOperationException("Araç kataloğunda adsız marka var: " + yol);
                }

                if (!markaAdlari.Add(marka.Ad))
                {
                    throw new InvalidOperationException("Araç kataloğunda marka iki kez geçiyor: " + marka.Ad);
                }

                if (marka.Seriler == null || marka.Seriler.Count == 0)
                {
                    throw new InvalidOperationException("Araç kataloğunda serisi olmayan marka var: " + marka.Ad);
                }

                foreach (var seri in marka.Seriler)
                {
                    if (string.IsNullOrWhiteSpace(seri))
                    {
                        throw new InvalidOperationException("Araç kataloğunda boş seri adı var: " + marka.Ad);
                    }

                    if (seriSahipleri.TryGetValue(seri, out var digerMarka))
                    {
                        throw new InvalidOperationException(
                            "Araç kataloğunda aynı seri iki markada geçiyor: " + seri + " (" + digerMarka + ", " + marka.Ad + ")");
                    }

                    seriSahipleri[seri] = marka.Ad;
                }
            }
        }
    }
}
