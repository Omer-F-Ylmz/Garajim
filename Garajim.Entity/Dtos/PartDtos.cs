using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class MaintenancePartDto
    {
        public int Id { get; set; }
        public ParcaTuru ParcaTuru { get; set; }
        public string Aciklama { get; set; }
        public int Adet { get; set; }
        public decimal? Tutar { get; set; }
        public string Marka { get; set; }
    }

    public class ParcaHafizasiDto
    {
        public ParcaTuru ParcaTuru { get; set; }
        public string ParcaAdi { get; set; }
        public DateTime? SonDegisimTarihi { get; set; }
        public int? SonDegisimKm { get; set; }
        public int DegisimSayisi { get; set; }
        public decimal ToplamTutar { get; set; }
        public int? SonrakiTahminiKm { get; set; }
        public DateTime? SonrakiTahminiTarih { get; set; }
        public string Durum { get; set; }
    }
}
