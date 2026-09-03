namespace Garajim.Entity.Dtos
{
    public class MaliyetAyDto
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public decimal Yakit { get; set; }
        public decimal Bakim { get; set; }
        public decimal Masraf { get; set; }
        public decimal Toplam { get; set; }
    }

    public class TuketimAyDto
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public decimal Litre100Km { get; set; }
        public decimal Kwh100Km { get; set; }
    }

    public class AracMaliyetDto
    {
        public int VehicleId { get; set; }
        public string Plaka { get; set; }
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public decimal ToplamYakit { get; set; }
        public decimal ToplamBakim { get; set; }
        public decimal ToplamMasraf { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public int MesafeKm { get; set; }
        public int YakitKaydiSayisi { get; set; }
        public decimal? MaliyetKmBasi { get; set; }
        public decimal? Litre100Km { get; set; }
        public decimal ToplamKwh { get; set; }
        public decimal? Kwh100Km { get; set; }
        public decimal? DonemDegerKaybi { get; set; }
        public decimal? SahiplikMaliyeti { get; set; }
        public List<MaliyetAyDto> AylikSeri { get; set; } = new List<MaliyetAyDto>();
        public List<TuketimAyDto> TuketimSeri { get; set; } = new List<TuketimAyDto>();
    }

    public class FiloMaliyetSatiriDto
    {
        public int VehicleId { get; set; }
        public string Plaka { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public decimal ToplamYakit { get; set; }
        public decimal ToplamBakim { get; set; }
        public decimal ToplamMasraf { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public int MesafeKm { get; set; }
        public int YakitKaydiSayisi { get; set; }
        public decimal? MaliyetKmBasi { get; set; }
        public decimal? Litre100Km { get; set; }
    }

    public class FiloMaliyetDto
    {
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public int ToplamMesafeKm { get; set; }
        public List<FiloMaliyetSatiriDto> Araclar { get; set; } = new List<FiloMaliyetSatiriDto>();
    }

    public class AracToplamDto
    {
        public int VehicleId { get; set; }
        public decimal Toplam { get; set; }
    }

    public class AracYakitOzetDto
    {
        public int VehicleId { get; set; }
        public int Adet { get; set; }
        public decimal Litre { get; set; }
        public decimal Tutar { get; set; }
        public int EnDusukKm { get; set; }
        public int EnYuksekKm { get; set; }
    }

    public class YakitOlcumDto
    {
        public DateTime Tarih { get; set; }
        public int Km { get; set; }
        public decimal Litre { get; set; }
        public decimal Kwh { get; set; }
        public bool TamDolum { get; set; }
    }
}
