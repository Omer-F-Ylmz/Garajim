namespace Garajim.Entity.Dtos
{
    public class HesapSilDto
    {
        public string Kod { get; set; }
    }

    public class HesapDurumDto
    {
        public bool SilmePlanlandi { get; set; }
        public DateTime? SilinmeTarihi { get; set; }
        public int KalanGun { get; set; }
    }
}
