using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class HasarOlusturDto
    {
        public int VehicleId { get; set; }
        public DateTime OlayTarihi { get; set; }
        public HasarTuru Tur { get; set; }
        public string Konum { get; set; }
        public string Aciklama { get; set; }
        public int? OlayKm { get; set; }
        public TutanakTuru TutanakTuru { get; set; }
        public string KarsiTarafPlaka { get; set; }
        public string KarsiTarafSigorta { get; set; }
        public string KarsiTarafPoliceNo { get; set; }
        public string SigortaDosyaNo { get; set; }
        public decimal? HasarBedeli { get; set; }
    }

    public class HasarGuncelleDto
    {
        public DateTime OlayTarihi { get; set; }
        public HasarTuru Tur { get; set; }
        public string Konum { get; set; }
        public string Aciklama { get; set; }
        public int? OlayKm { get; set; }
        public TutanakTuru TutanakTuru { get; set; }
        public string KarsiTarafPlaka { get; set; }
        public string KarsiTarafSigorta { get; set; }
        public string KarsiTarafPoliceNo { get; set; }
        public string SigortaDosyaNo { get; set; }
        public decimal? HasarBedeli { get; set; }
        public HasarDurumu Durum { get; set; }
    }

    public class HasarFotoDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Etiket { get; set; }
        public string EtiketAdi { get; set; }
        public int Sira { get; set; }
        public string DosyaAdi { get; set; }
    }

    public class HasarDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Plaka { get; set; }
        public DateTime OlayTarihi { get; set; }
        public string Tur { get; set; }
        public string TurAdi { get; set; }
        public string Konum { get; set; }
        public string Aciklama { get; set; }
        public int? OlayKm { get; set; }
        public string TutanakTuru { get; set; }
        public string TutanakTuruAdi { get; set; }
        public string KarsiTarafPlaka { get; set; }
        public string KarsiTarafSigorta { get; set; }
        public string KarsiTarafPoliceNo { get; set; }
        public string SigortaDosyaNo { get; set; }
        public decimal? HasarBedeli { get; set; }
        public string Durum { get; set; }
        public string DurumAdi { get; set; }
        public int FotoSayisi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public List<HasarFotoDto> Fotograflar { get; set; } = new List<HasarFotoDto>();
    }

    public class HasarKarneSatiriDto
    {
        public DateTime OlayTarihi { get; set; }
        public string Tur { get; set; }
        public string Durum { get; set; }
    }

    public class KazaRehberiAdimiDto
    {
        public string Baslik { get; set; }
        public List<string> Maddeler { get; set; } = new List<string>();
    }

    public class KazaRehberiDto
    {
        public string Ozet { get; set; }
        public List<string> AnlasmaliTutanakKosullari { get; set; } = new List<string>();
        public List<string> PolisGerekliHaller { get; set; } = new List<string>();
        public List<string> FotografListesi { get; set; } = new List<string>();
        public List<string> AlinacakBilgiler { get; set; } = new List<string>();
        public string BildirimSuresi { get; set; }
        public List<KazaRehberiAdimiDto> Adimlar { get; set; } = new List<KazaRehberiAdimiDto>();
        public string Kaynak { get; set; }
    }
}
