namespace Garajim.Entity.Dtos
{
    public class DavetDurumDto
    {
        public string Kod { get; set; }
        public string PaylasimBaglantisi { get; set; }
        public int DavetSayisi { get; set; }
        public int OdulGun { get; set; }
        public string DavetEden { get; set; }
        public List<DavetSatiriDto> Davetliler { get; set; } = new List<DavetSatiriDto>();
    }

    public class DavetSatiriDto
    {
        public string SirketAdi { get; set; }
        public DateTime KatilmaTarihi { get; set; }
    }
}
