using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class LastikTakDto
    {
        public int VehicleId { get; set; }
        public string Ad { get; set; }
        public LastikMevsimi Mevsim { get; set; }
        public string Marka { get; set; }
        public string Ebat { get; set; }
        public decimal? DisDerinligiMm { get; set; }
        public DateTime TakilmaTarihi { get; set; }
        public int TakilmaKm { get; set; }
    }

    public class LastikSokDto
    {
        public DateTime SokulmeTarihi { get; set; }
        public int SokulmeKm { get; set; }
        public decimal? DisDerinligiMm { get; set; }
    }

    public class LastikDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Ad { get; set; }
        public string Mevsim { get; set; }
        public string Marka { get; set; }
        public string Ebat { get; set; }
        public decimal? DisDerinligiMm { get; set; }
        public DateTime TakilmaTarihi { get; set; }
        public int TakilmaKm { get; set; }
        public DateTime? SokulmeTarihi { get; set; }
        public int? SokulmeKm { get; set; }
        public int ToplamKm { get; set; }
        public bool Takili { get; set; }
    }

    public class LastikDurumDto
    {
        public bool KisLastigiDonemi { get; set; }
        public string Uyari { get; set; }
        public LastikDto TakiliSet { get; set; }
        public List<LastikDto> Setler { get; set; } = new List<LastikDto>();
    }
}
