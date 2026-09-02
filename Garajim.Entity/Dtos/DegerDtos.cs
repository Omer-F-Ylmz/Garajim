using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class DegerGirDto
    {
        public DateTime Tarih { get; set; }
        public decimal Deger { get; set; }
        public DegerKaynagi Kaynak { get; set; }
        public string Not { get; set; }
    }

    public class AracDegerDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public DateTime Tarih { get; set; }
        public decimal Deger { get; set; }
        public string Kaynak { get; set; }
        public string KaynakAdi { get; set; }
        public string Not { get; set; }
    }

    public class DegerSerisiDto
    {
        public int VehicleId { get; set; }
        public string Plaka { get; set; }
        public List<AracDegerDto> Kayitlar { get; set; } = new List<AracDegerDto>();
        public AracDegerDto SonDeger { get; set; }
        public decimal? DegerKaybi { get; set; }
    }

    public class DegerTahminSonucuDto
    {
        public AracDegerDto Kayit { get; set; }
        public string Uyari { get; set; }
        public int KalanHak { get; set; }
    }
}
