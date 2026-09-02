using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class UstaOnayDurumDto
    {
        public bool OnayGerekli { get; set; }
        public string GuncelSurum { get; set; }
        public string KabulEdilenSurum { get; set; }
        public DateTime? KabulTarihi { get; set; }
        public string MetinBagi { get; set; }
    }

    public class UstaOnayVerDto
    {
        public string MetinSurumu { get; set; }
    }

    public class UstaSohbetOlusturDto
    {
        public int VehicleId { get; set; }
    }

    public class UstaMesajGonderDto
    {
        public string Metin { get; set; }
    }

    public class UstaGeriBildirimDto
    {
        public UstaGeriBildirim GeriBildirim { get; set; }
        public int? CozumBakimId { get; set; }
    }

    public class UstaKademeDto
    {
        public string Kademe { get; set; }
        public string Neden { get; set; }
        public string BelirtiUyumu { get; set; }
        public string EvdeKontrol { get; set; }
        public List<decimal> MaliyetTl { get; set; } = new List<decimal>();
        public string Aciliyet { get; set; }
    }

    public class UstaYanitDto
    {
        public string Ozet { get; set; }
        public bool KirmiziCizgi { get; set; }
        public List<UstaKademeDto> Kademeler { get; set; } = new List<UstaKademeDto>();
        public List<string> AracVerisindenNotlar { get; set; } = new List<string>();
        public string UstayaBoyleAnlat { get; set; }
        public List<string> TakipSorulari { get; set; } = new List<string>();
        public string Uyari { get; set; }
    }

    public class UstaMesajDto
    {
        public int Id { get; set; }
        public string Rol { get; set; }
        public string Metin { get; set; }
        public UstaYanitDto Yanit { get; set; }
        public bool KirmiziCizgi { get; set; }
        public string GeriBildirim { get; set; }
        public int? CozumBakimId { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }

    public class UstaSohbetDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Plaka { get; set; }
        public string Baslik { get; set; }
        public int MesajSayisi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public List<UstaMesajDto> Mesajlar { get; set; } = new List<UstaMesajDto>();
    }

    public class UstaMesajSonucDto
    {
        public int SohbetId { get; set; }
        public UstaMesajDto Mesaj { get; set; }
        public int KalanGunlukHak { get; set; }
        public int KalanSohbetMesaji { get; set; }
    }

    public class UstaBakimSecenegiDto
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }
        public string Tur { get; set; }
        public string Servis { get; set; }
        public decimal Tutar { get; set; }
    }

    public class UstaIstatistikDto
    {
        public int Toplam { get; set; }
        public int Puanlanan { get; set; }
        public int Olumlu { get; set; }
        public int KirmiziCizgi { get; set; }
        public int CozumBagli { get; set; }
        public long TokenGiris { get; set; }
        public long TokenCikis { get; set; }
        public long SureMs { get; set; }
    }

    public class UstaStatsDto
    {
        public int SoruSayisi { get; set; }
        public decimal PuanlananOrani { get; set; }
        public decimal OlumluOrani { get; set; }
        public decimal KirmiziCizgiOrani { get; set; }
        public decimal CozumBagiOrani { get; set; }
        public int OrtTokenGiris { get; set; }
        public int OrtTokenCikis { get; set; }
        public int OrtSureMs { get; set; }
        public decimal TahminiMaliyetTl { get; set; }
    }
}
