namespace Garajim.Entity.Dtos
{
    public class HesapSilDto
    {
        public string Kod { get; set; }
    }

    public class ProfilDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool BildirimEvrak { get; set; }
        public bool BildirimHatirlatma { get; set; }
        public bool GeciciSifre { get; set; }
    }

    public class ProfilGuncelleDto
    {
        public string FullName { get; set; }
        public bool BildirimEvrak { get; set; }
        public bool BildirimHatirlatma { get; set; }
    }

    public class EpostaDegistirKodDto
    {
        public string YeniEposta { get; set; }
    }

    public class EpostaDegistirDto
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
