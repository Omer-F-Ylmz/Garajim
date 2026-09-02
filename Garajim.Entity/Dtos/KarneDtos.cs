namespace Garajim.Entity.Dtos
{
    public class KarneKapsamDto
    {
        public bool BakimGecmisi { get; set; }
        public bool ParcaHafizasi { get; set; }
        public bool YakitOzeti { get; set; }
        public bool Belgeler { get; set; }
        public bool PlakaGoster { get; set; }
        public bool TutarGoster { get; set; }
        public bool AcilKart { get; set; }
        public bool HasarGecmisi { get; set; }
    }

    public class KarneOlusturDto
    {
        public KarneKapsamDto Kapsam { get; set; }
        public int? SonKullanmaGun { get; set; }
    }

    public class KarneLinkDto
    {
        public string Url { get; set; }
        public DateTime? SonKullanma { get; set; }
        public int GoruntulenmeSayisi { get; set; }
    }

    public class AcilKartDto
    {
        public string Plaka { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public int Yil { get; set; }
        public string AcilKisiAd { get; set; }
        public string AcilKisiTelefon { get; set; }
        public string AcilNot { get; set; }
        public string SigortaSaglayici { get; set; }
        public string SigortaPoliceNo { get; set; }
    }

    public class KarneAracDto
    {
        public string Plaka { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public int Yil { get; set; }
        public string YakitTipi { get; set; }
        public int GuncelKm { get; set; }
    }

    public class KarneBakimDto
    {
        public DateTime Tarih { get; set; }
        public string Tur { get; set; }
        public int Km { get; set; }
        public decimal? Tutar { get; set; }
        public string ServisAdi { get; set; }
    }

    public class KarneBelgeDto
    {
        public int Id { get; set; }
        public string Ad { get; set; }
        public DateTime Tarih { get; set; }
    }

    public class KarneYakitOzetiDto
    {
        public int KayitSayisi { get; set; }
        public decimal ToplamLitre { get; set; }
        public decimal? ToplamTutar { get; set; }
        public DateTime? SonDolumTarihi { get; set; }
    }

    public class KarneDto
    {
        public KarneAracDto Arac { get; set; }
        public List<KarneBakimDto> Bakimlar { get; set; } = new List<KarneBakimDto>();
        public List<ParcaHafizasiDto> Parcalar { get; set; } = new List<ParcaHafizasiDto>();
        public KarneYakitOzetiDto YakitOzeti { get; set; }
        public List<KarneBelgeDto> Belgeler { get; set; } = new List<KarneBelgeDto>();
        public List<HasarKarneSatiriDto> Hasarlar { get; set; } = new List<HasarKarneSatiriDto>();
        public decimal? BakimToplami { get; set; }
    }

    public class KarneStatsDto
    {
        public int AracSayisi { get; set; }
        public int KarnesiAktifArac { get; set; }
        public double AktifOran { get; set; }
        public int ToplamGoruntulenme { get; set; }
    }
}
